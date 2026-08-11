namespace ITHunterview.Domain.Enums;

/// <summary>
/// Structural usability of a JD analysis payload. This is intentionally
/// independent from the analysis/job lifecycle state.
/// </summary>
public enum JdAnalysisQuality
{
    COMPLETE,
    PARTIAL,
    INVALID
}
