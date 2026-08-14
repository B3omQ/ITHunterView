namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record CvAnalysisCoverage(
    int InputExperienceEntryCount,
    int AcceptedExperienceEntryCount,
    int DiscardedExperienceEntryCount,
    int InputRequirementSignalCount,
    int AcceptedRequirementSignalCount,
    int DiscardedRequirementSignalCount,
    int InputExperiencePeriodCount,
    int AcceptedExperiencePeriodCount,
    int DiscardedExperiencePeriodCount,
    bool TitleMetricsAvailable,
    bool SkillMetricsAvailable,
    bool ExperienceMetricAvailable,
    bool DomainMetricsAvailable);

public sealed record CvAnalysisDiagnostic(string Code, string JsonPath);

public sealed record CvAnalysisProjection(
    CvAnalysisCoverage Coverage,
    IReadOnlyList<CvAnalysisDiagnostic> Diagnostics,
    bool HasUsableMatchingContent,
    bool HasStructuralDegradation);
