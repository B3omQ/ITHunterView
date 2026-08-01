using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record CvAnalysisInputSnapshot(
    string RawText,
    string SourceType,
    string? FileName,
    DateOnly AnalysisDate);

public sealed record CvAnalysisValidationResult(
    bool IsValid,
    string CanonicalJson,
    string FailureCode)
{
    public static CvAnalysisValidationResult Success(string canonicalJson) => new(true, canonicalJson, string.Empty);

    public static CvAnalysisValidationResult Failure(string failureCode) => new(false, string.Empty, failureCode);
}

public sealed class CvAnalysisDocument
{
    [JsonRequired, JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("verbatim_sections")]
    public CvVerbatimSections VerbatimSections { get; set; } = new();

    [JsonRequired, JsonPropertyName("matching_metrics")]
    public CvMatchingMetrics MatchingMetrics { get; set; } = new();

    [JsonRequired, JsonPropertyName("matching_evidence")]
    public CvMatchingEvidence MatchingEvidence { get; set; } = new();
}

public sealed class CvVerbatimSections
{
    [JsonRequired, JsonPropertyName("personal_info")]
    public CvPersonalInfo PersonalInfo { get; set; } = new();

    [JsonRequired, JsonPropertyName("education")]
    public List<CvEducation> Education { get; set; } = new();

    [JsonRequired, JsonPropertyName("languages")]
    public List<CvLanguage> Languages { get; set; } = new();

    [JsonRequired, JsonPropertyName("skills_section")]
    public List<string> SkillsSection { get; set; } = new();

    [JsonRequired, JsonPropertyName("professional_experience_and_projects")]
    public List<CvExperienceOrProject> ProfessionalExperienceAndProjects { get; set; } = new();

    [JsonRequired, JsonPropertyName("certifications_and_awards")]
    public List<string> CertificationsAndAwards { get; set; } = new();

    [JsonRequired, JsonPropertyName("other_information")]
    public string OtherInformation { get; set; } = string.Empty;
}

public sealed class CvPersonalInfo
{
    [JsonRequired, JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

public sealed class CvEducation
{
    [JsonRequired, JsonPropertyName("institution")]
    public string Institution { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("degree")]
    public string Degree { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("major")]
    public string Major { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("timeline")]
    public string Timeline { get; set; } = string.Empty;
}

public sealed class CvLanguage
{
    [JsonRequired, JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("certifications_or_level")]
    public string CertificationsOrLevel { get; set; } = string.Empty;
}

public sealed class CvExperienceOrProject
{
    [JsonRequired, JsonPropertyName("company_or_project_name")]
    public string CompanyOrProjectName { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("timeline")]
    public string Timeline { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("entry_type")]
    public string EntryType { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("details_and_responsibilities")]
    public List<string> DetailsAndResponsibilities { get; set; } = new();

    [JsonRequired, JsonPropertyName("technologies_used")]
    public List<string> TechnologiesUsed { get; set; } = new();
}

public sealed class CvMatchingMetrics
{
    [JsonRequired, JsonPropertyName("job_titles_normalized")]
    public List<string> JobTitlesNormalized { get; set; } = new();

    [JsonRequired, JsonPropertyName("skills_normalized")]
    public List<string> SkillsNormalized { get; set; } = new();

    [JsonRequired, JsonPropertyName("total_years_exp")]
    public int TotalYearsExperience { get; set; }

    [JsonRequired, JsonPropertyName("domains")]
    public List<string> Domains { get; set; } = new();
}

public sealed class CvMatchingEvidence
{
    [JsonRequired, JsonPropertyName("requirement_signals")]
    public List<CvRequirementSignal> RequirementSignals { get; set; } = new();

    [JsonRequired, JsonPropertyName("experience_summary")]
    public CvExperienceSummary ExperienceSummary { get; set; } = new();

    [JsonRequired, JsonPropertyName("seniority_signals")]
    public List<CvSenioritySignal> SenioritySignals { get; set; } = new();
}

public sealed class CvRequirementSignal
{
    [JsonRequired, JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("evidence_strength")]
    public string EvidenceStrength { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("source_type")]
    public string SourceType { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("source_index")]
    public int SourceIndex { get; set; }

    [JsonRequired, JsonPropertyName("evidence")]
    public List<string> Evidence { get; set; } = new();
}

public sealed class CvExperienceSummary
{
    [JsonRequired, JsonPropertyName("total_professional_months")]
    public int TotalProfessionalMonths { get; set; }

    [JsonRequired, JsonPropertyName("calculation_basis")]
    public string CalculationBasis { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("periods")]
    public List<CvExperiencePeriod> Periods { get; set; } = new();
}

public sealed class CvExperiencePeriod
{
    [JsonRequired, JsonPropertyName("source_index")]
    public int SourceIndex { get; set; }

    [JsonRequired, JsonPropertyName("entry_type")]
    public string EntryType { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("organization")]
    public string Organization { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("timeline_raw")]
    public string TimelineRaw { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("start_year")]
    public int? StartYear { get; set; }

    [JsonRequired, JsonPropertyName("start_month")]
    public int? StartMonth { get; set; }

    [JsonRequired, JsonPropertyName("end_year")]
    public int? EndYear { get; set; }

    [JsonRequired, JsonPropertyName("end_month")]
    public int? EndMonth { get; set; }

    [JsonRequired, JsonPropertyName("is_current")]
    public bool IsCurrent { get; set; }

    [JsonRequired, JsonPropertyName("evidence")]
    public string Evidence { get; set; } = string.Empty;
}

public sealed class CvSenioritySignal
{
    [JsonRequired, JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("source_type")]
    public string SourceType { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("source_index")]
    public int SourceIndex { get; set; }

    [JsonRequired, JsonPropertyName("evidence")]
    public string Evidence { get; set; } = string.Empty;
}
