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
}
