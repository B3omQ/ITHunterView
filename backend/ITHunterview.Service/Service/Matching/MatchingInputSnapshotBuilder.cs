using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Service.Matching;

public sealed class MatchingInputSnapshotBuilder
{
    public const string LegacySchemaVersion = "matching-context/v1";
    public const string Version2SchemaVersion = "matching-context/v2";
    public const string SchemaVersion = "matching-context/v3";

    private readonly IMatchingSourceRepository _sourceRepository;
    private readonly IJobAnalysisInputBuilder _jobAnalysisInputBuilder;

    public MatchingInputSnapshotBuilder(
        IMatchingSourceRepository sourceRepository,
        IJobAnalysisInputBuilder? jobAnalysisInputBuilder = null)
    {
        _sourceRepository = sourceRepository;
        _jobAnalysisInputBuilder = jobAnalysisInputBuilder ?? new JobAnalysisInputBuilder();
    }

    public async Task<MatchingSnapshotResult> BuildAsync(
        Guid userId,
        PreparedMatchingRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cv = await BuildCvSnapshotAsync(userId, request.Cv, ct);
        var jd = await BuildJdSnapshotAsync(userId, request.Jd, ct);
        var snapshot = new MatchingInputSnapshotV1(
            SchemaVersion,
            request.Mode,
            cv,
            jd,
            DateTime.UtcNow);

        var json = MatchingInputSnapshotIntegrity.Serialize(snapshot);
        var hash = MatchingInputSnapshotIntegrity.ComputeHash(snapshot);

        return new MatchingSnapshotResult(snapshot, json, hash);
    }

    private async Task<MatchingCvSnapshot> BuildCvSnapshotAsync(
        Guid userId,
        PreparedCvSource source,
        CancellationToken ct)
    {
        if (source is PreparedRawCvSource raw)
        {
            return new MatchingCvSnapshot(
                "raw_cv",
                null,
                raw.FileName,
                raw.RawText,
                null,
                null);
        }

        if (source is not PreparedSavedCvSource saved)
        {
            throw new InvalidOperationException("INVALID_PREPARED_CV_SOURCE");
        }

        var cv = await _sourceRepository.GetOwnedCvAsync(saved.CvId, userId, ct);
        if (cv is null)
        {
            throw new KeyNotFoundException("CV not found");
        }

        return new MatchingCvSnapshot(
            "saved_cv",
            cv.Id,
            cv.FileName,
            cv.RawText ?? string.Empty,
            cv.ParsedData,
            ReadSchemaVersion(cv.ParsedData),
            string.IsNullOrWhiteSpace(cv.FileUrl) ? null : cv.FileUrl.Trim(),
            MatchingSourceFingerprint.ForCv(cv.FileUrl, cv.RawText),
            cv.ParseStatus);
    }

    private async Task<MatchingJdSnapshot> BuildJdSnapshotAsync(
        Guid userId,
        PreparedJdSource source,
        CancellationToken ct)
    {
        if (source is PreparedRawJdSource raw)
        {
            var rawInput = _jobAnalysisInputBuilder.BuildFromPastedText(raw.Title, raw.RawText);
            return new MatchingJdSnapshot(
                "raw_jd",
                null,
                raw.Title,
                raw.RawText,
                null,
                null,
                AnalysisInputJson: _jobAnalysisInputBuilder.SerializeCanonical(rawInput));
        }

        if (source is not PreparedSavedJdSource saved)
        {
            throw new InvalidOperationException("INVALID_PREPARED_JD_SOURCE");
        }

        var job = await _sourceRepository.GetAccessibleJobAsync(
            saved.JobId,
            userId,
            DateTime.UtcNow,
            ct);
        if (job is null)
        {
            throw new KeyNotFoundException("Job not found");
        }

        var savedInput = _jobAnalysisInputBuilder.Build(job);
        return new MatchingJdSnapshot(
            "saved_jd",
            job.Id,
            job.Title,
            JdTextHelper.BuildRawText(job),
            job.ParsedData,
            ReadSchemaVersion(job.ParsedData),
            MatchingSourceFingerprint.ForJd(savedInput, _jobAnalysisInputBuilder),
            MatchingSourceFingerprint.ForAnalysis(job.ParsedData),
            job.AnalysisRevision,
            job.EffectiveAnalysisRevision,
            job.ParseStatus,
            _jobAnalysisInputBuilder.SerializeCanonical(savedInput));
    }

    private static string? ReadSchemaVersion(string? analysisJson)
    {
        if (string.IsNullOrWhiteSpace(analysisJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(analysisJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("schema_version", out var schemaVersion) &&
                schemaVersion.ValueKind == JsonValueKind.String)
            {
                return schemaVersion.GetString();
            }
        }
        catch (JsonException)
        {
            // The existing parser/validator remains responsible for invalid
            // analysis JSON during processing. The snapshot records no guess.
        }

        return null;
    }
}
