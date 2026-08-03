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
        return JsonSerializer.Serialize(snapshot, JsonOptions);
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
                !string.Equals(snapshot.SchemaVersion, MatchingInputSnapshotBuilder.SchemaVersion, StringComparison.Ordinal) ||
                snapshot.Cv is null ||
                snapshot.Jd is null)
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
        var canonicalPayload = JsonSerializer.Serialize(new
        {
            snapshot.SchemaVersion,
            snapshot.Mode,
            snapshot.Cv,
            snapshot.Jd
        }, JsonOptions);

        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload)));
    }

    public static bool IsValid(MatchingInputSnapshotV1 snapshot, string? expectedHash)
        => !string.IsNullOrWhiteSpace(expectedHash)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(ComputeHash(snapshot)),
                Encoding.ASCII.GetBytes(expectedHash.Trim().ToLowerInvariant()));
}
