using System.Text.Json;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record StagedMatchingResult(
    decimal? Score,
    string MatchDetails,
    MatchingCompletionDisposition CompletionDisposition)
{
    public bool RequiresRefund => CompletionDisposition == MatchingCompletionDisposition.UnscoredRefundable;
}

/// <summary>
/// Reads only application-owned terminal envelopes that were staged by this
/// service. It never calls AI and never infers a missing numeric score.
/// </summary>
public sealed class StagedMatchingResultReader
{
    private const int MaximumDetailsLength = 1_000_000;

    public StagedMatchingResult ReadOrCreateSafeFallback(CvJobMatchScores job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return ReadOrCreateSafeFallback(job.MatchScore, job.MatchDetails);
    }

    public StagedMatchingResult ReadOrCreateSafeFallback(decimal? score, string? matchDetails)
    {
        if (TryRead(score, matchDetails, out var staged))
        {
            return staged!;
        }

        return new StagedMatchingResult(
            null,
            CreateSafeUnscoredEnvelope(),
            MatchingCompletionDisposition.UnscoredRefundable);
    }

    public bool TryRead(decimal? persistedScore, string? matchDetails, out StagedMatchingResult? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(matchDetails) || matchDetails.Length > MaximumDetailsLength)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(matchDetails, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64
            });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("contract", out var contractElement) ||
                contractElement.ValueKind != JsonValueKind.String ||
                contractElement.GetString() is not (JdFitResultContract.Version5 or JdFitResultContract.RawTextFallbackVersion2) ||
                !root.TryGetProperty("scoreAvailable", out var availabilityElement) ||
                availabilityElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("completionDisposition", out var dispositionElement) ||
                dispositionElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var scoreAvailable = availabilityElement.GetBoolean();
            var dispositionText = dispositionElement.GetString();
            if (scoreAvailable &&
                string.Equals(dispositionText, "scored_billable", StringComparison.Ordinal) &&
                persistedScore is >= 0m and <= 100m)
            {
                result = new StagedMatchingResult(
                    persistedScore,
                    matchDetails,
                    MatchingCompletionDisposition.ScoredBillable);
                return true;
            }

            if (!scoreAvailable &&
                string.Equals(dispositionText, "unscored_refundable", StringComparison.Ordinal) &&
                !persistedScore.HasValue)
            {
                result = new StagedMatchingResult(
                    null,
                    matchDetails,
                    MatchingCompletionDisposition.UnscoredRefundable);
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string CreateSafeUnscoredEnvelope() => JsonSerializer.Serialize(new
    {
        mode = "jd_fit",
        contract = JdFitResultContract.RawTextFallbackVersion2,
        scoreAvailable = false,
        completionDisposition = "unscored_refundable",
        resultCode = "SCORE_UNAVAILABLE",
        jdFit = new
        {
            score = (decimal?)null,
            result = (string?)null,
            requirementGroups = Array.Empty<object>(),
            criticalGaps = Array.Empty<object>(),
            narrative = "Kết quả hiện chưa thể chấm điểm."
        }
    });
}
