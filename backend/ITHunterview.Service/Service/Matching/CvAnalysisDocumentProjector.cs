using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Converts a readable provider payload to the fixed CV v2 shape. It performs
/// structural conversion only: no grounding, inference, aliasing, sorting, or
/// experience recalculation belongs at this boundary.
/// </summary>
public sealed class CvAnalysisDocumentProjector
{
    private const int MaxDiagnostics = 100;
    private static readonly HashSet<string> EntryTypes = new(StringComparer.Ordinal)
    {
        "professional_experience", "internship", "freelance", "academic_project",
        "personal_project", "volunteer_experience", "unknown"
    };
    private static readonly HashSet<string> SignalCategories = new(StringComparer.Ordinal)
    {
        "tech_skill", "experience", "domain_knowledge", "language", "education", "soft_skill"
    };
    private static readonly HashSet<string> EvidenceStrengths = new(StringComparer.Ordinal)
    {
        "listed", "applied", "outcome"
    };
    private static readonly HashSet<string> SourceTypes = new(StringComparer.Ordinal)
    {
        "headline", "summary", "skills_section", "professional_experience", "internship",
        "freelance", "academic_project", "personal_project", "volunteer_experience",
        "education", "language_section", "certification", "other"
    };
    private static readonly HashSet<string> CalculationBases = new(StringComparer.Ordinal)
    {
        "explicit_timeline", "partial_timeline", "insufficient_timeline"
    };

    public CvAnalysisProjection Project(JsonElement root)
    {
        var state = new ProjectionState();
        var document = new CvAnalysisDocument
        {
            SchemaVersion = root.GetProperty("schema_version").GetString()!.Trim()
        };

        var verbatim = ReadObject(root, "verbatim_sections", "$.verbatim_sections", "VERBATIM_SECTIONS_INVALID", state);
        document.VerbatimSections = ProjectVerbatim(verbatim, state);

        var metrics = ReadObject(root, "matching_metrics", "$.matching_metrics", "MATCHING_METRICS_INVALID", state);
        document.MatchingMetrics = ProjectMetrics(metrics, state);

        var evidence = ReadObject(root, "matching_evidence", "$.matching_evidence", "MATCHING_EVIDENCE_INVALID", state);
        document.MatchingEvidence = ProjectEvidence(
            evidence,
            document.VerbatimSections.ProfessionalExperienceAndProjects.Count,
            state);

        var coverage = new CvAnalysisCoverage(
            state.InputExperienceEntries,
            state.AcceptedExperienceEntries,
            Math.Max(0, state.InputExperienceEntries - state.AcceptedExperienceEntries),
            state.InputRequirementSignals,
            state.AcceptedRequirementSignals,
            Math.Max(0, state.InputRequirementSignals - state.AcceptedRequirementSignals),
            state.InputExperiencePeriods,
            state.AcceptedExperiencePeriods,
            Math.Max(0, state.InputExperiencePeriods - state.AcceptedExperiencePeriods),
            state.TitleMetricsAvailable,
            state.SkillMetricsAvailable,
            state.ExperienceMetricAvailable,
            state.DomainMetricsAvailable);

        return new CvAnalysisProjection(
            document,
            coverage,
            state.Diagnostics,
            HasUsableMatchingContent(document, coverage));
    }

    private static CvVerbatimSections ProjectVerbatim(JsonElement? section, ProjectionState state)
    {
        var result = new CvVerbatimSections();
        var personal = ReadObject(section, "personal_info", "$.verbatim_sections.personal_info", "PERSONAL_INFO_INVALID", state);
        result.PersonalInfo = new CvPersonalInfo
        {
            Name = ReadString(personal, "name", "$.verbatim_sections.personal_info.name", state),
            Title = ReadString(personal, "title", "$.verbatim_sections.personal_info.title", state),
            Summary = ReadString(personal, "summary", "$.verbatim_sections.personal_info.summary", state)
        };

        result.Education = ReadObjectArray(
            section,
            "education",
            "$.verbatim_sections.education",
            20,
            "EDUCATION_ENTRY_INVALID",
            state,
            (item, path) => new CvEducation
            {
                Institution = ReadString(item, "institution", $"{path}.institution", state),
                Degree = ReadString(item, "degree", $"{path}.degree", state),
                Major = ReadString(item, "major", $"{path}.major", state),
                Timeline = ReadString(item, "timeline", $"{path}.timeline", state)
            });

        result.Languages = ReadObjectArray(
            section,
            "languages",
            "$.verbatim_sections.languages",
            20,
            "LANGUAGE_ENTRY_INVALID",
            state,
            (item, path) => new CvLanguage
            {
                Language = ReadString(item, "language", $"{path}.language", state),
                CertificationsOrLevel = ReadString(item, "certifications_or_level", $"{path}.certifications_or_level", state)
            });

        result.SkillsSection = ReadStringArray(
            section,
            "skills_section",
            "$.verbatim_sections.skills_section",
            80,
            state);

        var experienceArray = ReadArray(
            section,
            "professional_experience_and_projects",
            "$.verbatim_sections.professional_experience_and_projects",
            state);
        state.InputExperienceEntries = experienceArray?.GetArrayLength() ?? 0;
        if (experienceArray is { } experiences)
        {
            var index = 0;
            foreach (var item in experiences.EnumerateArray())
            {
                var path = $"$.verbatim_sections.professional_experience_and_projects[{index}]";
                index++;
                if (result.ProfessionalExperienceAndProjects.Count >= 30)
                {
                    state.Add("EXPERIENCE_ENTRIES_TRUNCATED", "$.verbatim_sections.professional_experience_and_projects");
                    continue;
                }
                if (item.ValueKind != JsonValueKind.Object)
                {
                    state.Add("EXPERIENCE_ENTRY_INVALID", path);
                    continue;
                }

                var entryType = ReadString(item, "entry_type", $"{path}.entry_type", state);
                AddUnknownEnumDiagnostic(entryType, EntryTypes, "ENTRY_TYPE_UNKNOWN", $"{path}.entry_type", state);
                result.ProfessionalExperienceAndProjects.Add(new CvExperienceOrProject
                {
                    CompanyOrProjectName = ReadString(item, "company_or_project_name", $"{path}.company_or_project_name", state),
                    Role = ReadString(item, "role", $"{path}.role", state),
                    Timeline = ReadString(item, "timeline", $"{path}.timeline", state),
                    EntryType = entryType,
                    DetailsAndResponsibilities = ReadStringArray(item, "details_and_responsibilities", $"{path}.details_and_responsibilities", 100, state),
                    TechnologiesUsed = ReadStringArray(item, "technologies_used", $"{path}.technologies_used", 80, state)
                });
                state.AcceptedExperienceEntries++;
            }
        }

        result.CertificationsAndAwards = ReadStringArray(
            section,
            "certifications_and_awards",
            "$.verbatim_sections.certifications_and_awards",
            40,
            state);
        result.OtherInformation = ReadString(section, "other_information", "$.verbatim_sections.other_information", state);
        return result;
    }

    private static CvMatchingMetrics ProjectMetrics(JsonElement? metrics, ProjectionState state)
    {
        var result = new CvMatchingMetrics();
        result.JobTitlesNormalized = ReadStringArray(
            metrics,
            "job_titles_normalized",
            "$.matching_metrics.job_titles_normalized",
            40,
            state,
            available => state.TitleMetricsAvailable = available);
        result.SkillsNormalized = ReadStringArray(
            metrics,
            "skills_normalized",
            "$.matching_metrics.skills_normalized",
            100,
            state,
            available => state.SkillMetricsAvailable = available);
        result.Domains = ReadStringArray(
            metrics,
            "domains",
            "$.matching_metrics.domains",
            40,
            state,
            available => state.DomainMetricsAvailable = available);
        result.TotalYearsExperience = ReadInt(
            metrics,
            "total_years_exp",
            "$.matching_metrics.total_years_exp",
            state,
            value => value >= 0,
            available => state.ExperienceMetricAvailable = available);
        return result;
    }

    private static CvMatchingEvidence ProjectEvidence(JsonElement? evidence, int experienceCount, ProjectionState state)
    {
        var result = new CvMatchingEvidence();
        var signals = ReadArray(evidence, "requirement_signals", "$.matching_evidence.requirement_signals", state);
        state.InputRequirementSignals = signals?.GetArrayLength() ?? 0;
        if (signals is { } signalArray)
        {
            var index = 0;
            foreach (var item in signalArray.EnumerateArray())
            {
                var path = $"$.matching_evidence.requirement_signals[{index}]";
                index++;
                if (result.RequirementSignals.Count >= 100)
                {
                    state.Add("REQUIREMENT_SIGNALS_TRUNCATED", "$.matching_evidence.requirement_signals");
                    continue;
                }
                if (item.ValueKind != JsonValueKind.Object)
                {
                    state.Add("REQUIREMENT_SIGNAL_INVALID", path);
                    continue;
                }

                var category = ReadString(item, "category", $"{path}.category", state);
                var strength = ReadString(item, "evidence_strength", $"{path}.evidence_strength", state);
                var sourceType = ReadString(item, "source_type", $"{path}.source_type", state);
                AddUnknownEnumDiagnostic(category, SignalCategories, "SIGNAL_CATEGORY_UNKNOWN", $"{path}.category", state);
                AddUnknownEnumDiagnostic(strength, EvidenceStrengths, "EVIDENCE_STRENGTH_UNKNOWN", $"{path}.evidence_strength", state);
                AddUnknownEnumDiagnostic(sourceType, SourceTypes, "SOURCE_TYPE_UNKNOWN", $"{path}.source_type", state);
                var sourceIndex = ReadInt(item, "source_index", $"{path}.source_index", state);
                if (sourceIndex < 0 || (IsExperienceSource(sourceType) && sourceIndex >= experienceCount))
                {
                    state.Add("SOURCE_INDEX_OUT_OF_RANGE", $"{path}.source_index");
                }

                result.RequirementSignals.Add(new CvRequirementSignal
                {
                    Name = ReadString(item, "name", $"{path}.name", state),
                    Category = category,
                    EvidenceStrength = strength,
                    SourceType = sourceType,
                    SourceIndex = sourceIndex,
                    Evidence = ReadStringArray(item, "evidence", $"{path}.evidence", 5, state)
                });
                state.AcceptedRequirementSignals++;
            }
        }

        var summary = ReadObject(evidence, "experience_summary", "$.matching_evidence.experience_summary", "EXPERIENCE_SUMMARY_INVALID", state);
        var basis = ReadString(summary, "calculation_basis", "$.matching_evidence.experience_summary.calculation_basis", state);
        AddUnknownEnumDiagnostic(basis, CalculationBases, "CALCULATION_BASIS_UNKNOWN", "$.matching_evidence.experience_summary.calculation_basis", state);
        result.ExperienceSummary = new CvExperienceSummary
        {
            TotalProfessionalMonths = ReadInt(summary, "total_professional_months", "$.matching_evidence.experience_summary.total_professional_months", state, value => value >= 0),
            CalculationBasis = basis
        };

        var periods = ReadArray(summary, "periods", "$.matching_evidence.experience_summary.periods", state);
        state.InputExperiencePeriods = periods?.GetArrayLength() ?? 0;
        if (periods is { } periodArray)
        {
            var index = 0;
            foreach (var item in periodArray.EnumerateArray())
            {
                var path = $"$.matching_evidence.experience_summary.periods[{index}]";
                index++;
                if (result.ExperienceSummary.Periods.Count >= 30)
                {
                    state.Add("EXPERIENCE_PERIODS_TRUNCATED", "$.matching_evidence.experience_summary.periods");
                    continue;
                }
                if (item.ValueKind != JsonValueKind.Object)
                {
                    state.Add("EXPERIENCE_PERIOD_INVALID", path);
                    continue;
                }

                var entryType = ReadString(item, "entry_type", $"{path}.entry_type", state);
                AddUnknownEnumDiagnostic(entryType, EntryTypes, "ENTRY_TYPE_UNKNOWN", $"{path}.entry_type", state);
                var startYear = ReadNullableInt(item, "start_year", $"{path}.start_year", state);
                var startMonth = ReadNullableInt(item, "start_month", $"{path}.start_month", state);
                var endYear = ReadNullableInt(item, "end_year", $"{path}.end_year", state);
                var endMonth = ReadNullableInt(item, "end_month", $"{path}.end_month", state);
                AddRangeDiagnostic(startYear, 1900, 2200, "YEAR_OUT_OF_RANGE", $"{path}.start_year", state);
                AddRangeDiagnostic(endYear, 1900, 2200, "YEAR_OUT_OF_RANGE", $"{path}.end_year", state);
                AddRangeDiagnostic(startMonth, 1, 12, "MONTH_OUT_OF_RANGE", $"{path}.start_month", state);
                AddRangeDiagnostic(endMonth, 1, 12, "MONTH_OUT_OF_RANGE", $"{path}.end_month", state);

                var sourceIndex = ReadInt(item, "source_index", $"{path}.source_index", state);
                if (sourceIndex < 0 || sourceIndex >= experienceCount)
                {
                    state.Add("SOURCE_INDEX_OUT_OF_RANGE", $"{path}.source_index");
                }

                result.ExperienceSummary.Periods.Add(new CvExperiencePeriod
                {
                    SourceIndex = sourceIndex,
                    EntryType = entryType,
                    Organization = ReadString(item, "organization", $"{path}.organization", state),
                    Role = ReadString(item, "role", $"{path}.role", state),
                    TimelineRaw = ReadString(item, "timeline_raw", $"{path}.timeline_raw", state),
                    StartYear = startYear,
                    StartMonth = startMonth,
                    EndYear = endYear,
                    EndMonth = endMonth,
                    IsCurrent = ReadBool(item, "is_current", $"{path}.is_current", state),
                    Evidence = ReadString(item, "evidence", $"{path}.evidence", state)
                });
                state.AcceptedExperiencePeriods++;
            }
        }

        result.SenioritySignals = ReadObjectArray(
            evidence,
            "seniority_signals",
            "$.matching_evidence.seniority_signals",
            40,
            "SENIORITY_SIGNAL_INVALID",
            state,
            (item, path) =>
            {
                var sourceType = ReadString(item, "source_type", $"{path}.source_type", state);
                AddUnknownEnumDiagnostic(sourceType, SourceTypes, "SOURCE_TYPE_UNKNOWN", $"{path}.source_type", state);
                var sourceIndex = ReadInt(item, "source_index", $"{path}.source_index", state);
                if (sourceIndex < 0 || (IsExperienceSource(sourceType) && sourceIndex >= experienceCount))
                {
                    state.Add("SOURCE_INDEX_OUT_OF_RANGE", $"{path}.source_index");
                }
                return new CvSenioritySignal
                {
                    Name = ReadString(item, "name", $"{path}.name", state),
                    SourceType = sourceType,
                    SourceIndex = sourceIndex,
                    Evidence = ReadString(item, "evidence", $"{path}.evidence", state)
                };
            });
        return result;
    }

    private static bool HasUsableMatchingContent(CvAnalysisDocument document, CvAnalysisCoverage coverage) =>
        !string.IsNullOrWhiteSpace(document.VerbatimSections.PersonalInfo.Title) ||
        !string.IsNullOrWhiteSpace(document.VerbatimSections.PersonalInfo.Summary) ||
        document.VerbatimSections.Education.Count > 0 ||
        document.VerbatimSections.Languages.Count > 0 ||
        document.VerbatimSections.SkillsSection.Count > 0 ||
        document.VerbatimSections.ProfessionalExperienceAndProjects.Count > 0 ||
        document.VerbatimSections.CertificationsAndAwards.Count > 0 ||
        !string.IsNullOrWhiteSpace(document.VerbatimSections.OtherInformation) ||
        document.MatchingMetrics.JobTitlesNormalized.Count > 0 ||
        document.MatchingMetrics.SkillsNormalized.Count > 0 ||
        coverage.ExperienceMetricAvailable ||
        document.MatchingMetrics.Domains.Count > 0 ||
        document.MatchingEvidence.RequirementSignals.Count > 0 ||
        document.MatchingEvidence.ExperienceSummary.Periods.Count > 0 ||
        document.MatchingEvidence.SenioritySignals.Count > 0;

    private static JsonElement? ReadObject(JsonElement owner, string property, string path, string code, ProjectionState state) =>
        ReadObject((JsonElement?)owner, property, path, code, state);

    private static JsonElement? ReadObject(JsonElement? owner, string property, string path, string code, ProjectionState state)
    {
        if (owner is { ValueKind: JsonValueKind.Object } value &&
            value.TryGetProperty(property, out var child) && child.ValueKind == JsonValueKind.Object)
        {
            return child;
        }
        state.Add(code, path);
        return null;
    }

    private static JsonElement? ReadArray(JsonElement? owner, string property, string path, ProjectionState state)
    {
        if (owner is { ValueKind: JsonValueKind.Object } value &&
            value.TryGetProperty(property, out var child) && child.ValueKind == JsonValueKind.Array)
        {
            return child;
        }
        state.Add("ARRAY_INVALID", path);
        return null;
    }

    private static string ReadString(JsonElement? owner, string property, string path, ProjectionState state)
    {
        if (owner is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty(property, out var child) && child.ValueKind == JsonValueKind.String)
        {
            return child.GetString()?.Trim() ?? string.Empty;
        }
        state.Add("STRING_INVALID", path);
        return string.Empty;
    }

    private static int ReadInt(
        JsonElement? owner,
        string property,
        string path,
        ProjectionState state,
        Func<int, bool>? range = null,
        Action<bool>? availability = null)
    {
        if (owner is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty(property, out var child) &&
            child.ValueKind == JsonValueKind.Number && child.TryGetInt32(out var result) && (range?.Invoke(result) ?? true))
        {
            availability?.Invoke(true);
            return result;
        }
        availability?.Invoke(false);
        state.Add("INTEGER_INVALID", path);
        return 0;
    }

    private static int? ReadNullableInt(JsonElement owner, string property, string path, ProjectionState state)
    {
        if (!owner.TryGetProperty(property, out var child) || child.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (child.ValueKind == JsonValueKind.Number && child.TryGetInt32(out var result))
        {
            return result;
        }
        state.Add("NULLABLE_INTEGER_INVALID", path);
        return null;
    }

    private static bool ReadBool(JsonElement owner, string property, string path, ProjectionState state)
    {
        if (owner.TryGetProperty(property, out var child) && child.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return child.GetBoolean();
        }
        state.Add("BOOLEAN_INVALID", path);
        return false;
    }

    private static List<string> ReadStringArray(
        JsonElement? owner,
        string property,
        string path,
        int cap,
        ProjectionState state,
        Action<bool>? availability = null)
    {
        var array = ReadArray(owner, property, path, state);
        availability?.Invoke(array is not null);
        if (array is null)
        {
            return new List<string>();
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var child in array.Value.EnumerateArray())
        {
            if (result.Count >= cap)
            {
                state.Add("ARRAY_TRUNCATED", path);
                break;
            }
            if (child.ValueKind != JsonValueKind.String)
            {
                state.Add("STRING_ARRAY_ENTRY_INVALID", $"{path}[{index}]");
                index++;
                continue;
            }
            var text = child.GetString()?.Trim() ?? string.Empty;
            if (seen.Add(text))
            {
                result.Add(text);
            }
            index++;
        }
        return result;
    }

    private static List<T> ReadObjectArray<T>(
        JsonElement? owner,
        string property,
        string path,
        int cap,
        string invalidCode,
        ProjectionState state,
        Func<JsonElement, string, T> projector)
    {
        var array = ReadArray(owner, property, path, state);
        var result = new List<T>();
        if (array is null)
        {
            return result;
        }
        var index = 0;
        foreach (var item in array.Value.EnumerateArray())
        {
            var itemPath = $"{path}[{index}]";
            index++;
            if (result.Count >= cap)
            {
                state.Add("ARRAY_TRUNCATED", path);
                break;
            }
            if (item.ValueKind != JsonValueKind.Object)
            {
                state.Add(invalidCode, itemPath);
                continue;
            }
            result.Add(projector(item, itemPath));
        }
        return result;
    }

    private static void AddUnknownEnumDiagnostic(string value, HashSet<string> allowed, string code, string path, ProjectionState state)
    {
        if (!string.IsNullOrWhiteSpace(value) && !allowed.Contains(value))
        {
            state.Add(code, path);
        }
    }

    private static void AddRangeDiagnostic(int? value, int min, int max, string code, string path, ProjectionState state)
    {
        if (value.HasValue && (value.Value < min || value.Value > max))
        {
            state.Add(code, path);
        }
    }

    private static bool IsExperienceSource(string sourceType) =>
        sourceType is "professional_experience" or "internship" or "freelance" or
            "academic_project" or "personal_project" or "volunteer_experience";

    private sealed class ProjectionState
    {
        public List<CvAnalysisDiagnostic> Diagnostics { get; } = new();
        public int InputExperienceEntries { get; set; }
        public int AcceptedExperienceEntries { get; set; }
        public int InputRequirementSignals { get; set; }
        public int AcceptedRequirementSignals { get; set; }
        public int InputExperiencePeriods { get; set; }
        public int AcceptedExperiencePeriods { get; set; }
        public bool TitleMetricsAvailable { get; set; }
        public bool SkillMetricsAvailable { get; set; }
        public bool ExperienceMetricAvailable { get; set; }
        public bool DomainMetricsAvailable { get; set; }

        public void Add(string code, string path)
        {
            if (Diagnostics.Count >= MaxDiagnostics || Diagnostics.Any(x => x.Code == code && x.JsonPath == path))
            {
                return;
            }
            Diagnostics.Add(new CvAnalysisDiagnostic(code, path));
        }
    }
}
