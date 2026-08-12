using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Service.Matching;

public sealed class MatchingJdPreparationService : IMatchingJdPreparationService
{
    private const int MaxDiagnosticCount = 100;
    private const string SavedJdSourceKind = "saved_jd";
    private const string SuccessStatus = "SUCCESS";
    private const string RawFallbackStatus = "RAW_FALLBACK";

    private readonly IJobAnalysisExtractionService _extractionService;
    private readonly IJobAnalysisInputBuilder _inputBuilder;
    private readonly IJdRequirementProjector _projector;

    public MatchingJdPreparationService(
        IJobAnalysisExtractionService extractionService,
        IJobAnalysisInputBuilder inputBuilder,
        IJdRequirementProjector projector)
    {
        _extractionService = extractionService;
        _inputBuilder = inputBuilder;
        _projector = projector;
    }

    public async Task<PreparedJdMatchingInput> PrepareAsync(
        MatchingInputSnapshotV1 snapshot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var jd = snapshot.Jd;
        if (CanReuseRawFallback(snapshot))
        {
            return new PreparedRawJdMatchingInput(
                RequireRawText(jd.OriginalText),
                jd.Title,
                Array.Empty<JdAnalysisDiagnostic>(),
                null);
        }

        if (CanReuseStructured(snapshot) && TryProjectUsable(jd.AnalysisJson, out var reusableProjection))
        {
            return new PreparedStructuredJdMatchingInput(
                jd.AnalysisJson!,
                reusableProjection,
                reusableProjection.Quality,
                reusableProjection.Coverage,
                reusableProjection.Diagnostics ?? Array.Empty<JdAnalysisDiagnostic>(),
                null);
        }

        var input = BuildAnalysisInput(snapshot, out var inputDiagnostics);
        var extraction = await _extractionService.ExtractWithActivePromptsAsync(input, ct);
        var diagnostics = MergeDiagnostics(inputDiagnostics, extraction.Diagnostics);
        if (extraction.Quality is JdAnalysisQuality.COMPLETE or JdAnalysisQuality.PARTIAL &&
            extraction.Validation.Data is not null)
        {
            var effectiveJson = _extractionService.SerializeEffectiveAnalysis(extraction.Validation.Data);
            if (TryProjectUsable(effectiveJson, out var projection))
            {
                var projectedDiagnostics = MergeDiagnostics(
                    diagnostics,
                    projection.Diagnostics ?? Array.Empty<JdAnalysisDiagnostic>());
                return new PreparedStructuredJdMatchingInput(
                    effectiveJson,
                    projection,
                    projection.Quality,
                    projection.Coverage,
                    projectedDiagnostics,
                    CreatePersistenceIntent(
                        snapshot,
                        effectiveJson,
                        projection.Quality,
                        projection.Coverage,
                        projectedDiagnostics,
                        null));
            }
        }

        var rawText = !string.IsNullOrWhiteSpace(jd.OriginalText)
            ? jd.OriginalText
            : RequireRawText(extraction.RawTextFallback);
        var failureCode = extraction.Validation.FailureCode
            ?? extraction.Diagnostics.FirstOrDefault()?.Code
            ?? inputDiagnostics.FirstOrDefault()?.Code
            ?? "INVALID_JD_ANALYSIS";
        return new PreparedRawJdMatchingInput(
            rawText,
            jd.Title,
            diagnostics,
            CreatePersistenceIntent(snapshot, null, JdAnalysisQuality.INVALID, null, diagnostics, failureCode));
    }

    private JobAnalysisInputSnapshot BuildAnalysisInput(
        MatchingInputSnapshotV1 snapshot,
        out IReadOnlyList<JdAnalysisDiagnostic> diagnostics)
    {
        var jd = snapshot.Jd;
        if (string.Equals(snapshot.SchemaVersion, MatchingInputSnapshotBuilder.SchemaVersion, StringComparison.Ordinal))
        {
            try
            {
                var canonicalInput = _inputBuilder.BuildFromCanonicalJson(jd.AnalysisInputJson ?? string.Empty);
                diagnostics = Array.Empty<JdAnalysisDiagnostic>();
                return canonicalInput;
            }
            catch (InvalidOperationException exception)
                when (string.Equals(exception.Message, "INVALID_CANONICAL_JD_INPUT", StringComparison.Ordinal))
            {
                diagnostics = new[]
                {
                    new JdAnalysisDiagnostic(
                        "SNAPSHOT_CANONICAL_INPUT_INVALID",
                        "$.jd.analysisInputJson")
                };
                return BuildCompatibilityInput(jd);
            }
        }

        diagnostics = Array.Empty<JdAnalysisDiagnostic>();
        if (string.Equals(snapshot.SchemaVersion, MatchingInputSnapshotBuilder.Version2SchemaVersion, StringComparison.Ordinal) &&
            string.Equals(jd.SourceKind, SavedJdSourceKind, StringComparison.Ordinal))
        {
            return _inputBuilder.BuildFromSavedSnapshotText(jd.Title, jd.OriginalText);
        }

        return _inputBuilder.BuildFromPastedText(jd.Title, jd.OriginalText);
    }

    private JobAnalysisInputSnapshot BuildCompatibilityInput(MatchingJdSnapshot jd) =>
        string.Equals(jd.SourceKind, SavedJdSourceKind, StringComparison.Ordinal)
            ? _inputBuilder.BuildFromSavedSnapshotText(jd.Title, jd.OriginalText)
            : _inputBuilder.BuildFromPastedText(jd.Title, jd.OriginalText);

    private static IReadOnlyList<JdAnalysisDiagnostic> MergeDiagnostics(
        IReadOnlyList<JdAnalysisDiagnostic> inputDiagnostics,
        IReadOnlyList<JdAnalysisDiagnostic> extractionDiagnostics) =>
        inputDiagnostics
            .Concat(extractionDiagnostics)
            .Distinct()
            .Take(MaxDiagnosticCount)
            .ToArray();

    private bool TryProjectUsable(string? effectiveJson, out JdRequirementProjection projection)
    {
        projection = null!;
        if (string.IsNullOrWhiteSpace(effectiveJson))
        {
            return false;
        }

        var candidate = _projector.Project(effectiveJson);
        if (candidate.Quality == JdAnalysisQuality.INVALID || candidate.Groups.Count == 0)
        {
            return false;
        }

        projection = candidate;
        return true;
    }

    private static bool CanReuseRawFallback(MatchingInputSnapshotV1 snapshot)
    {
        var jd = snapshot.Jd;
        return IsCurrentSavedSnapshot(snapshot) &&
               string.Equals(jd.SourceParseStatus, RawFallbackStatus, StringComparison.Ordinal) &&
               !string.IsNullOrWhiteSpace(jd.OriginalText);
    }

    private static bool CanReuseStructured(MatchingInputSnapshotV1 snapshot)
    {
        var jd = snapshot.Jd;
        if (string.IsNullOrWhiteSpace(jd.AnalysisJson))
        {
            return false;
        }

        // v1 jobs predate persisted analysis lifecycle metadata. Retain their
        // structurally usable data for backwards-compatible worker recovery.
        if (string.Equals(snapshot.SchemaVersion, MatchingInputSnapshotBuilder.LegacySchemaVersion, StringComparison.Ordinal))
        {
            return true;
        }

        return IsCurrentSavedSnapshot(snapshot) &&
               string.Equals(jd.SourceParseStatus, SuccessStatus, StringComparison.Ordinal);
    }

    private static bool IsCurrentSavedSnapshot(MatchingInputSnapshotV1 snapshot)
    {
        var jd = snapshot.Jd;
        var isSupportedGuardedVersion =
            string.Equals(snapshot.SchemaVersion, MatchingInputSnapshotBuilder.Version2SchemaVersion, StringComparison.Ordinal) ||
            string.Equals(snapshot.SchemaVersion, MatchingInputSnapshotBuilder.SchemaVersion, StringComparison.Ordinal);
        return isSupportedGuardedVersion &&
               string.Equals(jd.SourceKind, SavedJdSourceKind, StringComparison.Ordinal) &&
               jd.SourceId.HasValue &&
               jd.SourceAnalysisRevision.HasValue &&
               jd.SourceEffectiveAnalysisRevision.HasValue &&
               jd.SourceAnalysisRevision == jd.SourceEffectiveAnalysisRevision;
    }

    private static string RequireRawText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new InvalidOperationException("INVALID_JD_ANALYSIS");
        }

        return rawText;
    }

    private static JdAnalysisPersistenceIntent? CreatePersistenceIntent(
        MatchingInputSnapshotV1 snapshot,
        string? canonicalJson,
        JdAnalysisQuality quality,
        JdAnalysisCoverage? coverage,
        IReadOnlyList<JdAnalysisDiagnostic> diagnostics,
        string? failureCode)
    {
        var jd = snapshot.Jd;
        if (!IsCurrentSavedSnapshot(snapshot) ||
            string.IsNullOrWhiteSpace(jd.SourceContentHash) ||
            string.IsNullOrWhiteSpace(jd.SourceAnalysisHash))
        {
            return null;
        }

        return new JdAnalysisPersistenceIntent(
            jd.SourceId!.Value,
            jd.SourceContentHash,
            jd.SourceAnalysisHash,
            jd.SourceAnalysisRevision,
            canonicalJson,
            quality,
            JdAnalysisMetadataReader.SerializeCoverage(coverage),
            JdAnalysisMetadataReader.SerializeDiagnostics(diagnostics),
            failureCode);
    }
}
