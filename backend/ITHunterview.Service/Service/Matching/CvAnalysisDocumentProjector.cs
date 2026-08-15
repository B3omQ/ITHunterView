using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Inspects the fixed CV v2 structure without producing a rewritten document.
/// Diagnostics describe readability only; the provider JSON remains untouched.
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
        var state = new InspectionState();

        var verbatim = RequireObject(root, "verbatim_sections", "$.verbatim_sections", "VERBATIM_SECTIONS_INVALID", state);
        InspectVerbatim(verbatim, state);

        var metrics = RequireObject(root, "matching_metrics", "$.matching_metrics", "MATCHING_METRICS_INVALID", state);
        InspectMetrics(metrics, state);

        var evidence = RequireObject(root, "matching_evidence", "$.matching_evidence", "MATCHING_EVIDENCE_INVALID", state);
        InspectEvidence(evidence, state);

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
            coverage,
            state.Diagnostics,
            state.HasUsableMatchingContent,
            state.HasStructuralDegradation);
    }

    private static void InspectVerbatim(JsonElement? section, InspectionState state)
    {
        var personal = RequireObject(section, "personal_info", "$.verbatim_sections.personal_info", "PERSONAL_INFO_INVALID", state);
        RequireString(personal, "name", "$.verbatim_sections.personal_info.name", state, useful: false);
        RequireString(personal, "title", "$.verbatim_sections.personal_info.title", state);
        RequireString(personal, "summary", "$.verbatim_sections.personal_info.summary", state);

        InspectObjectArray(section, "education", "$.verbatim_sections.education", "EDUCATION_ENTRY_INVALID", state,
            static (item, path, itemState) =>
                RequireString(item, "institution", $"{path}.institution", itemState) |
                RequireString(item, "degree", $"{path}.degree", itemState) |
                RequireString(item, "major", $"{path}.major", itemState) |
                RequireString(item, "timeline", $"{path}.timeline", itemState));

        InspectObjectArray(section, "languages", "$.verbatim_sections.languages", "LANGUAGE_ENTRY_INVALID", state,
            static (item, path, itemState) =>
                RequireString(item, "language", $"{path}.language", itemState) |
                RequireString(item, "certifications_or_level", $"{path}.certifications_or_level", itemState));

        InspectStringArray(section, "skills_section", "$.verbatim_sections.skills_section", state);

        var experiences = RequireArray(section, "professional_experience_and_projects",
            "$.verbatim_sections.professional_experience_and_projects", state);
        state.InputExperienceEntries = experiences?.GetArrayLength() ?? 0;
        if (experiences is { } experienceArray)
        {
            var index = 0;
            foreach (var item in experienceArray.EnumerateArray())
            {
                var path = $"$.verbatim_sections.professional_experience_and_projects[{index++}]";
                if (item.ValueKind != JsonValueKind.Object)
                {
                    state.AddStructural("EXPERIENCE_ENTRY_INVALID", path);
                    continue;
                }

                var hasValue = false;
                hasValue |= RequireString(item, "company_or_project_name", $"{path}.company_or_project_name", state);
                hasValue |= RequireString(item, "role", $"{path}.role", state);
                hasValue |= RequireString(item, "timeline", $"{path}.timeline", state);
                hasValue |= RequireString(item, "entry_type", $"{path}.entry_type", state, allowed: EntryTypes,
                    unknownCode: "ENTRY_TYPE_UNKNOWN");
                hasValue |= InspectStringArray(item, "details_and_responsibilities", $"{path}.details_and_responsibilities", state);
                hasValue |= InspectStringArray(item, "technologies_used", $"{path}.technologies_used", state);
                if (hasValue)
                {
                    state.AcceptedExperienceEntries++;
                }
                else
                {
                    state.AddStructural("EXPERIENCE_ENTRY_EMPTY", path);
                }
            }
        }

        InspectStringArray(section, "certifications_and_awards", "$.verbatim_sections.certifications_and_awards", state);
        RequireString(section, "other_information", "$.verbatim_sections.other_information", state);
    }

    private static void InspectMetrics(JsonElement? metrics, InspectionState state)
    {
        state.TitleMetricsAvailable = InspectStringArray(metrics, "job_titles_normalized",
            "$.matching_metrics.job_titles_normalized", state);
        state.SkillMetricsAvailable = InspectStringArray(metrics, "skills_normalized",
            "$.matching_metrics.skills_normalized", state);
        state.DomainMetricsAvailable = InspectStringArray(metrics, "domains",
            "$.matching_metrics.domains", state);

        if (metrics is { ValueKind: JsonValueKind.Object } value &&
            value.TryGetProperty("total_years_exp", out var years) &&
            years.ValueKind == JsonValueKind.Number && years.TryGetInt32(out var totalYears) && totalYears >= 0)
        {
            state.ExperienceMetricAvailable = true;
            state.HasUsableMatchingContent = true;
        }
        else
        {
            state.AddStructural("INTEGER_INVALID", "$.matching_metrics.total_years_exp");
        }
    }

    private static void InspectEvidence(JsonElement? evidence, InspectionState state)
    {
        var signals = RequireArray(evidence, "requirement_signals", "$.matching_evidence.requirement_signals", state);
        state.InputRequirementSignals = signals?.GetArrayLength() ?? 0;
        if (signals is { } signalArray)
        {
            var index = 0;
            foreach (var item in signalArray.EnumerateArray())
            {
                var path = $"$.matching_evidence.requirement_signals[{index++}]";
                if (item.ValueKind != JsonValueKind.Object)
                {
                    state.AddStructural("REQUIREMENT_SIGNAL_INVALID", path);
                    continue;
                }

                var hasValue = false;
                hasValue |= RequireString(item, "name", $"{path}.name", state);
                hasValue |= RequireString(item, "category", $"{path}.category", state, allowed: SignalCategories,
                    unknownCode: "SIGNAL_CATEGORY_UNKNOWN");
                hasValue |= RequireString(item, "evidence_strength", $"{path}.evidence_strength", state,
                    allowed: EvidenceStrengths, unknownCode: "EVIDENCE_STRENGTH_UNKNOWN");
                hasValue |= RequireString(item, "source_type", $"{path}.source_type", state, allowed: SourceTypes,
                    unknownCode: "SOURCE_TYPE_UNKNOWN");
                RequireInteger(item, "source_index", $"{path}.source_index", state, nullable: false);
                hasValue |= InspectStringArray(item, "evidence", $"{path}.evidence", state);
                if (hasValue)
                {
                    state.AcceptedRequirementSignals++;
                }
                else
                {
                    state.AddStructural("REQUIREMENT_SIGNAL_EMPTY", path);
                }
            }
        }

        var summary = RequireObject(evidence, "experience_summary", "$.matching_evidence.experience_summary",
            "EXPERIENCE_SUMMARY_INVALID", state);
        RequireNonNegativeInteger(summary, "total_professional_months",
            "$.matching_evidence.experience_summary.total_professional_months", state);
        RequireString(summary, "calculation_basis", "$.matching_evidence.experience_summary.calculation_basis", state,
            allowed: CalculationBases, unknownCode: "CALCULATION_BASIS_UNKNOWN");

        var periods = RequireArray(summary, "periods", "$.matching_evidence.experience_summary.periods", state);
        state.InputExperiencePeriods = periods?.GetArrayLength() ?? 0;
        if (periods is { } periodArray)
        {
            var index = 0;
            foreach (var item in periodArray.EnumerateArray())
            {
                var path = $"$.matching_evidence.experience_summary.periods[{index++}]";
                if (item.ValueKind != JsonValueKind.Object)
                {
                    state.AddStructural("EXPERIENCE_PERIOD_INVALID", path);
                    continue;
                }

                var hasValue = false;
                RequireInteger(item, "source_index", $"{path}.source_index", state, nullable: false);
                hasValue |= RequireString(item, "entry_type", $"{path}.entry_type", state, allowed: EntryTypes,
                    unknownCode: "ENTRY_TYPE_UNKNOWN");
                hasValue |= RequireString(item, "organization", $"{path}.organization", state);
                hasValue |= RequireString(item, "role", $"{path}.role", state);
                hasValue |= RequireString(item, "timeline_raw", $"{path}.timeline_raw", state);
                RequireInteger(item, "start_year", $"{path}.start_year", state, nullable: true);
                RequireInteger(item, "start_month", $"{path}.start_month", state, nullable: true);
                RequireInteger(item, "end_year", $"{path}.end_year", state, nullable: true);
                RequireInteger(item, "end_month", $"{path}.end_month", state, nullable: true);
                RequireBoolean(item, "is_current", $"{path}.is_current", state);
                hasValue |= RequireString(item, "evidence", $"{path}.evidence", state);
                if (hasValue)
                {
                    state.AcceptedExperiencePeriods++;
                }
                else
                {
                    state.AddStructural("EXPERIENCE_PERIOD_EMPTY", path);
                }
            }
        }

        InspectObjectArray(evidence, "seniority_signals", "$.matching_evidence.seniority_signals",
            "SENIORITY_SIGNAL_INVALID", state,
            static (item, path, itemState) =>
            {
                var hasValue = RequireString(item, "name", $"{path}.name", itemState);
                hasValue |= RequireString(item, "source_type", $"{path}.source_type", itemState,
                    allowed: SourceTypes, unknownCode: "SOURCE_TYPE_UNKNOWN");
                RequireInteger(item, "source_index", $"{path}.source_index", itemState, nullable: false);
                hasValue |= RequireString(item, "evidence", $"{path}.evidence", itemState);
                return hasValue;
            });
    }

    private static JsonElement? RequireObject(JsonElement owner, string property, string path, string code, InspectionState state) =>
        RequireObject((JsonElement?)owner, property, path, code, state);

    private static JsonElement? RequireObject(JsonElement? owner, string property, string path, string code, InspectionState state)
    {
        if (owner is { ValueKind: JsonValueKind.Object } value &&
            value.TryGetProperty(property, out var child) && child.ValueKind == JsonValueKind.Object)
        {
            return child;
        }

        state.AddStructural(code, path);
        return null;
    }

    private static JsonElement? RequireArray(JsonElement? owner, string property, string path, InspectionState state)
    {
        if (owner is { ValueKind: JsonValueKind.Object } value &&
            value.TryGetProperty(property, out var child) && child.ValueKind == JsonValueKind.Array)
        {
            return child;
        }

        state.AddStructural("ARRAY_INVALID", path);
        return null;
    }

    private static bool RequireString(
        JsonElement? owner,
        string property,
        string path,
        InspectionState state,
        bool useful = true,
        HashSet<string>? allowed = null,
        string? unknownCode = null)
    {
        if (owner is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty(property, out var child) || child.ValueKind != JsonValueKind.String)
        {
            state.AddStructural("STRING_INVALID", path);
            return false;
        }

        var text = child.GetString();
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (useful)
            {
                state.HasUsableMatchingContent = true;
            }
            if (allowed is not null && !allowed.Contains(text) && unknownCode is not null)
            {
                state.AddWarning(unknownCode, path);
            }
            return useful;
        }

        return false;
    }

    private static bool InspectStringArray(JsonElement? owner, string property, string path, InspectionState state)
    {
        var array = RequireArray(owner, property, path, state);
        if (array is null)
        {
            return false;
        }

        var hasValue = false;
        var index = 0;
        foreach (var child in array.Value.EnumerateArray())
        {
            if (child.ValueKind != JsonValueKind.String)
            {
                state.AddStructural("STRING_ARRAY_ENTRY_INVALID", $"{path}[{index++}]");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(child.GetString()))
            {
                hasValue = true;
                state.HasUsableMatchingContent = true;
            }
            index++;
        }
        return hasValue;
    }

    private static void InspectObjectArray(
        JsonElement? owner,
        string property,
        string path,
        string invalidCode,
        InspectionState state,
        Func<JsonElement, string, InspectionState, bool> inspect)
    {
        var array = RequireArray(owner, property, path, state);
        if (array is null)
        {
            return;
        }

        var index = 0;
        foreach (var item in array.Value.EnumerateArray())
        {
            var itemPath = $"{path}[{index++}]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                state.AddStructural(invalidCode, itemPath);
                continue;
            }

            if (!inspect(item, itemPath, state))
            {
                state.AddStructural($"{invalidCode}_EMPTY", itemPath);
            }
        }
    }

    private static void RequireNonNegativeInteger(JsonElement? owner, string property, string path, InspectionState state)
    {
        if (owner is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty(property, out var child) &&
            child.ValueKind == JsonValueKind.Number && child.TryGetInt32(out var number) && number >= 0)
        {
            state.HasUsableMatchingContent = true;
            return;
        }

        state.AddStructural("INTEGER_INVALID", path);
    }

    private static void RequireInteger(JsonElement owner, string property, string path, InspectionState state, bool nullable)
    {
        if (!owner.TryGetProperty(property, out var child))
        {
            state.AddStructural(nullable ? "NULLABLE_INTEGER_INVALID" : "INTEGER_INVALID", path);
            return;
        }
        if ((nullable && child.ValueKind == JsonValueKind.Null) ||
            (child.ValueKind == JsonValueKind.Number && child.TryGetInt32(out _)))
        {
            return;
        }

        state.AddStructural(nullable ? "NULLABLE_INTEGER_INVALID" : "INTEGER_INVALID", path);
    }

    private static void RequireBoolean(JsonElement owner, string property, string path, InspectionState state)
    {
        if (owner.TryGetProperty(property, out var child) && child.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return;
        }
        state.AddStructural("BOOLEAN_INVALID", path);
    }

    private sealed class InspectionState
    {
        public List<CvAnalysisDiagnostic> Diagnostics { get; } = new();
        public bool HasUsableMatchingContent { get; set; }
        public bool HasStructuralDegradation { get; private set; }
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

        public void AddStructural(string code, string path)
        {
            HasStructuralDegradation = true;
            Add(code, path);
        }

        public void AddWarning(string code, string path) => Add(code, path);

        private void Add(string code, string path)
        {
            if (Diagnostics.Count >= MaxDiagnostics || Diagnostics.Any(x => x.Code == code && x.JsonPath == path))
            {
                return;
            }
            Diagnostics.Add(new CvAnalysisDiagnostic(code, path));
        }
    }
}
