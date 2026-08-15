namespace ITHunterview.Domain.Enums;

/// <summary>
/// Stable, persisted progress codes for one-to-one CV/JD matching jobs.
/// These codes describe execution progress only; they do not select prompts,
/// contracts, algorithms, billing behavior, or authorization rules.
/// </summary>
public static class MatchingProcessingStages
{
    public const string Queued = "queued";
    public const string PreparingCv = "preparing_cv";
    public const string PreparingJd = "preparing_jd";
    public const string MatchingRequirements = "matching_requirements";
    public const string Finalizing = "finalizing";
    public const string WaitingForRetry = "waiting_for_retry";
    public const string Completed = "completed";
    public const string Failed = "failed";

    public static bool IsKnown(string? value)
        => value is Queued
            or PreparingCv
            or PreparingJd
            or MatchingRequirements
            or Finalizing
            or WaitingForRetry
            or Completed
            or Failed;
}
