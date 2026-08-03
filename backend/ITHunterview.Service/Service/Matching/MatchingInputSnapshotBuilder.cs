using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Service.Matching;

public sealed class MatchingInputSnapshotBuilder
{
    public const string SchemaVersion = "matching-context/v1";

    private readonly IMatchingSourceRepository _sourceRepository;

    public MatchingInputSnapshotBuilder(IMatchingSourceRepository sourceRepository)
    {
        _sourceRepository = sourceRepository;
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
            ReadSchemaVersion(cv.ParsedData));
    }

    private async Task<MatchingJdSnapshot> BuildJdSnapshotAsync(
        Guid userId,
        PreparedJdSource source,
        CancellationToken ct)
    {
        if (source is PreparedRawJdSource raw)
        {
            return new MatchingJdSnapshot(
                "raw_jd",
                null,
                raw.Title,
                raw.RawText,
                null,
                null);
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

        return new MatchingJdSnapshot(
            "saved_jd",
            job.Id,
            job.Title,
            JdTextHelper.BuildRawText(job),
            job.ParsedData,
            ReadSchemaVersion(job.ParsedData));
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
