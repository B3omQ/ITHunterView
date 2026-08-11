using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Keeps the persisted snapshot hash canonical and independent from submission
/// metadata such as the timestamp. The same function is used at submit and
/// immediately before worker execution.
/// </summary>
public static class MatchingInputSnapshotIntegrity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(MatchingInputSnapshotV1 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.SchemaVersion switch
        {
            MatchingInputSnapshotBuilder.LegacySchemaVersion =>
                JsonSerializer.Serialize(CreateV1StoragePayload(snapshot), JsonOptions),
            MatchingInputSnapshotBuilder.Version2SchemaVersion =>
                JsonSerializer.Serialize(CreateV2StoragePayload(snapshot), JsonOptions),
            MatchingInputSnapshotBuilder.SchemaVersion =>
                JsonSerializer.Serialize(CreateV3StoragePayload(snapshot), JsonOptions),
            _ => throw new InvalidOperationException("SNAPSHOT_INVALID")
        };
    }

    public static MatchingInputSnapshotV1 Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("SNAPSHOT_INVALID");
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<MatchingInputSnapshotV1>(json, JsonOptions);
            if (snapshot is null ||
                snapshot.Cv is null ||
                snapshot.Jd is null ||
                (snapshot.SchemaVersion != MatchingInputSnapshotBuilder.LegacySchemaVersion &&
                 snapshot.SchemaVersion != MatchingInputSnapshotBuilder.Version2SchemaVersion &&
                 snapshot.SchemaVersion != MatchingInputSnapshotBuilder.SchemaVersion) ||
                (snapshot.SchemaVersion == MatchingInputSnapshotBuilder.LegacySchemaVersion && HasPostV1Fields(snapshot)) ||
                (snapshot.SchemaVersion == MatchingInputSnapshotBuilder.Version2SchemaVersion && HasV3OnlyFields(snapshot)))
            {
                throw new InvalidOperationException("SNAPSHOT_INVALID");
            }

            return snapshot;
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("SNAPSHOT_INVALID");
        }
    }

    public static string ComputeHash(MatchingInputSnapshotV1 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var canonicalPayload = snapshot.SchemaVersion switch
        {
            MatchingInputSnapshotBuilder.LegacySchemaVersion =>
                JsonSerializer.Serialize(CreateV1HashPayload(snapshot), JsonOptions),
            MatchingInputSnapshotBuilder.Version2SchemaVersion =>
                JsonSerializer.Serialize(CreateV2HashPayload(snapshot), JsonOptions),
            MatchingInputSnapshotBuilder.SchemaVersion =>
                JsonSerializer.Serialize(CreateV3HashPayload(snapshot), JsonOptions),
            _ => throw new InvalidOperationException("SNAPSHOT_INVALID")
        };

        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload)));
    }

    public static bool IsValid(MatchingInputSnapshotV1 snapshot, string? expectedHash)
        => !string.IsNullOrWhiteSpace(expectedHash)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(ComputeHash(snapshot)),
                Encoding.ASCII.GetBytes(expectedHash.Trim().ToLowerInvariant()));

    private static bool HasPostV1Fields(MatchingInputSnapshotV1 snapshot) =>
        snapshot.Cv.FileUrl is not null ||
        snapshot.Cv.SourceContentHash is not null ||
        snapshot.Cv.SourceParseStatus is not null ||
        snapshot.Jd.SourceContentHash is not null ||
        snapshot.Jd.SourceAnalysisHash is not null ||
        snapshot.Jd.SourceAnalysisRevision is not null ||
        snapshot.Jd.SourceEffectiveAnalysisRevision is not null ||
        snapshot.Jd.SourceParseStatus is not null ||
        HasV3OnlyFields(snapshot);

    private static bool HasV3OnlyFields(MatchingInputSnapshotV1 snapshot) =>
        snapshot.Jd.AnalysisInputJson is not null;

    private static object CreateV1StoragePayload(MatchingInputSnapshotV1 snapshot) => new
    {
        snapshot.SchemaVersion,
        snapshot.Mode,
        Cv = CreateV1CvPayload(snapshot.Cv),
        Jd = CreateV1JdPayload(snapshot.Jd),
        snapshot.SubmittedAtUtc
    };

    private static object CreateV2StoragePayload(MatchingInputSnapshotV1 snapshot) => new
    {
        snapshot.SchemaVersion,
        snapshot.Mode,
        Cv = CreateV2CvPayload(snapshot.Cv),
        Jd = CreateV2JdPayload(snapshot.Jd),
        snapshot.SubmittedAtUtc
    };

    private static object CreateV3StoragePayload(MatchingInputSnapshotV1 snapshot) => new
    {
        snapshot.SchemaVersion,
        snapshot.Mode,
        Cv = CreateV2CvPayload(snapshot.Cv),
        Jd = CreateV3JdPayload(snapshot.Jd),
        snapshot.SubmittedAtUtc
    };

    private static object CreateV1HashPayload(MatchingInputSnapshotV1 snapshot) => new
    {
        snapshot.SchemaVersion,
        snapshot.Mode,
        Cv = CreateV1CvPayload(snapshot.Cv),
        Jd = CreateV1JdPayload(snapshot.Jd)
    };

    private static object CreateV2HashPayload(MatchingInputSnapshotV1 snapshot) => new
    {
        snapshot.SchemaVersion,
        snapshot.Mode,
        Cv = CreateV2CvPayload(snapshot.Cv),
        Jd = CreateV2JdPayload(snapshot.Jd)
    };

    private static object CreateV3HashPayload(MatchingInputSnapshotV1 snapshot) => new
    {
        snapshot.SchemaVersion,
        snapshot.Mode,
        Cv = CreateV2CvPayload(snapshot.Cv),
        Jd = CreateV3JdPayload(snapshot.Jd)
    };

    private static object CreateV1CvPayload(MatchingCvSnapshot cv) => new
    {
        cv.SourceKind,
        cv.SourceId,
        cv.FileName,
        cv.OriginalText,
        cv.AnalysisJson,
        cv.AnalysisSchemaVersion
    };

    private static object CreateV1JdPayload(MatchingJdSnapshot jd) => new
    {
        jd.SourceKind,
        jd.SourceId,
        jd.Title,
        jd.OriginalText,
        jd.AnalysisJson,
        jd.AnalysisSchemaVersion
    };

    private static object CreateV2CvPayload(MatchingCvSnapshot cv) => new
    {
        cv.SourceKind,
        cv.SourceId,
        cv.FileName,
        cv.OriginalText,
        cv.AnalysisJson,
        cv.AnalysisSchemaVersion,
        cv.FileUrl,
        cv.SourceContentHash,
        cv.SourceParseStatus
    };

    private static object CreateV2JdPayload(MatchingJdSnapshot jd) => new
    {
        jd.SourceKind,
        jd.SourceId,
        jd.Title,
        jd.OriginalText,
        jd.AnalysisJson,
        jd.AnalysisSchemaVersion,
        jd.SourceContentHash,
        jd.SourceAnalysisHash,
        jd.SourceAnalysisRevision,
        jd.SourceEffectiveAnalysisRevision,
        jd.SourceParseStatus
    };

    private static object CreateV3JdPayload(MatchingJdSnapshot jd) => new
    {
        jd.SourceKind,
        jd.SourceId,
        jd.Title,
        jd.OriginalText,
        jd.AnalysisJson,
        jd.AnalysisSchemaVersion,
        jd.SourceContentHash,
        jd.SourceAnalysisHash,
        jd.SourceAnalysisRevision,
        jd.SourceEffectiveAnalysisRevision,
        jd.SourceParseStatus,
        jd.AnalysisInputJson
    };
}
