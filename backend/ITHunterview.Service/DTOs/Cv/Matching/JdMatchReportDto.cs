namespace ITHunterview.Service.DTOs.Cv.Matching;

public static class MatchReportKinds
{
    public const string Structured = "structured";
    public const string RawTextFallback = "raw_text_fallback";
    public const string LegacySummary = "legacy_summary";
}

public static class MatchMethodCodes
{
    public const string OneToOneAi = "one_to_one_ai";
    public const string Hardcode = "hardcode";
    public const string Vector = "vector";
    public const string RawTextAi = "raw_text_ai";
    public const string LegacyUnknown = "legacy_unknown";
}

public static class MatchReportContracts
{
    public const string Version2 = "match-report/v2";
    public const string Version3 = "match-report/v3";
    public const string Current = Version3;
}

public sealed class MatchReportDto
{
    public string ReportContract { get; set; } = MatchReportContracts.Current;
    public string ReportKind { get; set; } = MatchReportKinds.LegacySummary;
    public string? SchemaVersion { get; set; }
    public string MatchMethod { get; set; } = MatchMethodCodes.LegacyUnknown;
    public decimal? ScorePercent { get; set; }
    public bool ScoreAvailable { get; set; }
    public string? ResultCode { get; set; }
    public string? ResultLabel { get; set; }
    public string Narrative { get; set; } = string.Empty;
    public List<MatchRequirementGroupReportDto> RequirementGroups { get; set; } = new();
    public List<MatchCriticalGapReportDto> CriticalGaps { get; set; } = new();
    public List<string> WarningFlags { get; set; } = new();
}

public sealed class MatchRequirementGroupReportDto
{
    public string? GroupId { get; set; }
    public string? SourceRequirementId { get; set; }
    public string? Intent { get; set; }
    public string? Operator { get; set; }
    public int? MinSatisfied { get; set; }
    public string? Importance { get; set; }
    public string? SourceSection { get; set; }
    public string? RequirementVerbatim { get; set; }
    public decimal? GroupScore { get; set; }
    public List<string> SelectedItemIds { get; set; } = new();
    public List<string> SatisfiedItemIds { get; set; } = new();
    public bool IsCriticalGap { get; set; }
    public int? SourceOrder { get; set; }
    public List<MatchRequirementItemReportDto> Items { get; set; } = new();
}

public sealed class MatchRequirementItemReportDto
{
    public string? ItemId { get; set; }
    public string? NormalizedText { get; set; }
    public string? DetailVerbatim { get; set; }
    public string? RawMention { get; set; }
    public string? Category { get; set; }
    public decimal? Score { get; set; }
    public string AssessmentStatus { get; set; } = "assessed";
    public string? HandlerCode { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<MatchEvidenceReportDto> Evidence { get; set; } = new();
    public bool IsCriticalGap { get; set; }
    public int? SourceOrder { get; set; }
}

public sealed class MatchEvidenceReportDto
{
    public string Quotation { get; set; } = string.Empty;
    public string? Section { get; set; }
}

public sealed class MatchCriticalGapReportDto
{
    public string GapId { get; set; } = string.Empty;
    public string Code { get; set; } = "CRITICAL_GAP";
    public string? Scope { get; set; }
    public string? GroupId { get; set; }
    public string? ItemId { get; set; }
    public string? SourceRequirementId { get; set; }
    public string? SourceSection { get; set; }
    public string? Category { get; set; }
    public string? Importance { get; set; }
    public string? Operator { get; set; }
    public int? RequiredCount { get; set; }
    public int? SatisfiedCount { get; set; }
    public List<string> AffectedItemIds { get; set; } = new();
    public string Requirement { get; set; } = string.Empty;
    public string RequirementVerbatim { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public List<MatchEvidenceReportDto> Evidence { get; set; } = new();
}
