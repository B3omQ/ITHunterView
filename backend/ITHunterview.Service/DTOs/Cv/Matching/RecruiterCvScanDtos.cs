using System.Text.Json.Serialization;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed class RecruiterCvScanRunDto
{
    public Guid RunId { get; init; }
    public Guid JobId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public sealed class RecruiterCvScanResultDto
{
    public Guid ScanResultId { get; init; }
    public string AnonymousLabel { get; init; } = string.Empty;
    public int Rank { get; init; }
    public decimal? MatchScore { get; init; }
    public string MatchDetails { get; init; } = string.Empty;
    public bool IsUnlocked { get; init; }
    public int UnlockCost { get; init; }
    public DateTime? MatchedAt { get; init; }

    // Populated only when IsUnlocked == true (R-10: candidate identity must not leak in locked state)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CandidateName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CandidateUserId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CvId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CvFileName { get; init; }

    /// <summary>
    /// Original CV URL when the CV still exists; otherwise snapshot storage key reference.
    /// Email and Phone are intentionally excluded — exposed only via the dedicated Unlock endpoint.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileUrl { get; init; }
}
