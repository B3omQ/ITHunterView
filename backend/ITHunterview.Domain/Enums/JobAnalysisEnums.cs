namespace ITHunterview.Domain.Enums
{
    public enum JobAnalysisStatus
    {
        PENDING,
        PROCESSING,
        READY,
        FAILED,
        SUPERSEDED
    }

    public enum SkillResolutionStatus
    {
        EXACT_CANONICAL,
        EXACT_ALIAS,
        AMBIGUOUS,
        UNMATCHED,
        MANUAL
    }

    public enum SkillDecisionStatus
    {
        PENDING,
        ACCEPTED,
        REJECTED
    }
}
