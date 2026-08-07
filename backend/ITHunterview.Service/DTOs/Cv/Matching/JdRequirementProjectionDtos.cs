using System;
using System.Collections.Generic;
using System.Linq;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record JdRequirementProjection(
    string SourceSchemaVersion,
    IReadOnlyList<ProjectedJdRequirementGroup> Groups,
    bool UsesLegacySemantics,
    JdAnalysisQuality Quality = JdAnalysisQuality.COMPLETE,
    JdAnalysisCoverage? Coverage = null,
    IReadOnlyList<JdAnalysisDiagnostic>? Diagnostics = null)
{
    public string AnalysisQuality => Quality.ToString();
    public bool RequirementSetComplete => Coverage?.RequirementSetComplete ?? true;
    public IReadOnlyList<string>? WarningCodes => Diagnostics?
        .Select(diagnostic => diagnostic.Code)
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

public sealed record ProjectedJdRequirementGroup(
    string GroupId,
    string Operator,
    int MinSatisfied,
    string Importance,
    IReadOnlyList<ProjectedJdRequirementItem> Items,
    string SourceSection = "",
    string RequirementVerbatim = "");

public sealed record ProjectedJdRequirementItem(
    string ItemId,
    string Category,
    string SkillName,
    string DetailVerbatim,
    string RawMention,
    string SourceSection,
    IReadOnlyList<string> Evidences,
    int? MinYears,
    int? MaxYears,
    decimal CategoryWeight);

public static class JdRequirementCategoryWeights
{
    public static decimal Get(string category) => category switch
    {
        "tech_skill" => 1.0m,
        "experience" => 0.9m,
        "seniority_fit" => 0.9m,
        "domain_knowledge" => 0.7m,
        "language" => 0.6m,
        "education" => 0.5m,
        "soft_skill" => 0.4m,
        _ => throw new InvalidOperationException("INVALID_EFFECTIVE_JD_ANALYSIS")
    };
}
