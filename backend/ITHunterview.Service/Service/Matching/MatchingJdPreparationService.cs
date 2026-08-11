using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Service.Matching;

public sealed class MatchingJdPreparationService : IMatchingJdPreparationService
{
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

        var input = _inputBuilder.BuildFromPastedText(jd.Title, jd.OriginalText);
        var extraction = await _extractionService.ExtractWithActivePromptsAsync(input, ct);
        if (extraction.Quality is JdAnalysisQuality.COMPLETE or JdAnalysisQuality.PARTIAL &&
            extraction.Validation.Data is not null)
        {
            var effectiveJson = _extractionService.SerializeEffectiveAnalysis(extraction.Validation.Data);
            if (TryProjectUsable(effectiveJson, out var projection))
            {
                return new PreparedStructuredJdMatchingInput(
                    effectiveJson,
                    projection,
                    extraction.Quality,
                    extraction.Coverage,
                    extraction.Diagnostics,
                    CreatePersistenceIntent(snapshot, effectiveJson, extraction.Quality, extraction.Coverage, extraction.Diagnostics, null));
            }
        }

        var rawText = !string.IsNullOrWhiteSpace(extraction.RawTextFallback)
            ? extraction.RawTextFallback
            : RequireRawText(jd.OriginalText);
        var failureCode = extraction.Validation.FailureCode
            ?? extraction.Diagnostics.FirstOrDefault()?.Code
            ?? "INVALID_JD_ANALYSIS";
        return new PreparedRawJdMatchingInput(
            rawText,
            jd.Title,
            extraction.Diagnostics,
            CreatePersistenceIntent(snapshot, null, JdAnalysisQuality.INVALID, null, extraction.Diagnostics, failureCode));
    }

    private bool TryProjectUsable(string? effectiveJson, out JdRequirementProjection projection)
    {
        projection = null!;
        if (string.IsNullOrWhiteSpace(effectiveJson))
        {
            return false;
        }

        try
        {
            var candidate = _projector.Project(effectiveJson);
            if (candidate.Quality == JdAnalysisQuality.INVALID || candidate.Groups.Count == 0)
            {
                return false;
            }

            projection = candidate;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool CanReuseRawFallback(MatchingInputSnapshotV1 snapshot)
    {
        var jd = snapshot.Jd;
        return IsCurrentSavedV2(snapshot) &&
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

        return IsCurrentSavedV2(snapshot) &&
               string.Equals(jd.SourceParseStatus, SuccessStatus, StringComparison.Ordinal);
    }

    private static bool IsCurrentSavedV2(MatchingInputSnapshotV1 snapshot)
    {
        var jd = snapshot.Jd;
        return string.Equals(snapshot.SchemaVersion, MatchingInputSnapshotBuilder.SchemaVersion, StringComparison.Ordinal) &&
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
        if (!IsCurrentSavedV2(snapshot) ||
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
