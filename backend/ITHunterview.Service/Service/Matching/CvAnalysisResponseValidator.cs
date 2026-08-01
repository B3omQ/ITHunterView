using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Enforces the persisted <c>cv-analysis/v2</c> contract. It is deliberately
/// independent from the prompt so every AI entry point (file, URL, pasted text,
/// on-demand parse) receives the same safe, canonical output.
/// </summary>
public sealed class CvAnalysisResponseValidator : ICvAnalysisResponseValidator
{
    private const string ContractVersion = "cv-analysis/v2";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false
    };

    private static readonly HashSet<string> EntryTypes = new(StringComparer.Ordinal)
    {
        "professional_experience", "internship", "freelance", "academic_project",
        "personal_project", "volunteer_experience", "unknown"
    };

    private static readonly HashSet<string> ProfessionalEntryTypes = new(StringComparer.Ordinal)
    {
        "professional_experience", "internship", "freelance"
    };

    private static readonly HashSet<string> SignalCategories = new(StringComparer.Ordinal)
    {
        "tech_skill", "domain_knowledge", "language", "education", "soft_skill"
    };

    private static readonly HashSet<string> EvidenceStrengths = new(StringComparer.Ordinal)
    {
        "listed", "applied", "outcome"
    };

    private static readonly HashSet<string> SignalSourceTypes = new(StringComparer.Ordinal)
    {
        "headline", "summary", "skills_section", "professional_experience", "internship",
        "freelance", "academic_project", "personal_project", "volunteer_experience",
        "education", "language_section", "certification", "other"
    };

    private static readonly HashSet<string> CalculationBases = new(StringComparer.Ordinal)
    {
        "explicit_timeline", "partial_timeline", "insufficient_timeline"
    };

    private static readonly HashSet<string> SenioritySignalNames = new(StringComparer.Ordinal)
    {
        "team leadership", "mentoring", "technical ownership", "architecture ownership",
        "project ownership", "stakeholder communication", "code review",
        "production responsibility", "system design", "cross-team collaboration"
    };

    private static readonly IReadOnlyDictionary<string, string> CanonicalNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["reactjs"] = "react",
            ["react.js"] = "react",
            ["node"] = "node.js",
            ["nodejs"] = "node.js",
            ["node.js"] = "node.js",
            ["postgres"] = "postgresql",
            ["postgresql"] = "postgresql",
            ["microsoft sql server"] = "sql server",
            ["ms sql server"] = "sql server",
            ["mssql"] = "sql server",
            ["c sharp"] = "c#",
            ["c-sharp"] = "c#",
            ["dotnet"] = ".net",
            [".net"] = ".net",
            ["asp.net core"] = "asp.net core",
            ["rest"] = "rest api",
            ["restful api"] = "rest api",
            ["rest api"] = "rest api",
            ["ci-cd"] = "ci/cd",
            ["continuous integration and continuous delivery"] = "ci/cd",
            ["oop"] = "object-oriented programming",
            ["object oriented programming"] = "object-oriented programming",
            ["object-oriented programming"] = "object-oriented programming",
            ["js"] = "javascript",
            ["ts"] = "typescript"
        };

    public CvAnalysisValidationResult ValidateAndCanonicalize(string responseJson, CvAnalysisInputSnapshot input)
    {
        if (string.IsNullOrWhiteSpace(input.RawText))
        {
            return CvAnalysisValidationResult.Failure("CV_ANALYSIS_RAW_TEXT_REQUIRED");
        }

        if (!IsSupportedSourceType(input.SourceType))
        {
            return CvAnalysisValidationResult.Failure("CV_ANALYSIS_INPUT_INVALID");
        }

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return CvAnalysisValidationResult.Failure("CV_ANALYSIS_EMPTY_OUTPUT");
        }

        try
        {
            using var json = JsonDocument.Parse(responseJson);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                return CvAnalysisValidationResult.Failure("CV_ANALYSIS_SCHEMA_INVALID");
            }
        }
        catch (JsonException)
        {
            return CvAnalysisValidationResult.Failure("CV_ANALYSIS_INVALID_JSON");
        }

        CvAnalysisDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<CvAnalysisDocument>(responseJson, SerializerOptions);
        }
        catch (JsonException)
        {
            return CvAnalysisValidationResult.Failure("CV_ANALYSIS_SCHEMA_INVALID");
        }

        if (document is null)
        {
            return CvAnalysisValidationResult.Failure("CV_ANALYSIS_SCHEMA_INVALID");
        }

        try
        {
            ValidateDocument(document, input.RawText);
            Canonicalize(document, input.AnalysisDate);
            return CvAnalysisValidationResult.Success(JsonSerializer.Serialize(document, SerializerOptions));
        }
        catch (CvAnalysisContractException exception)
        {
            return CvAnalysisValidationResult.Failure(exception.Code);
        }
    }

    private static void ValidateDocument(CvAnalysisDocument document, string rawText)
    {
        Require(string.Equals(document.SchemaVersion, ContractVersion, StringComparison.Ordinal), "CV_ANALYSIS_SCHEMA_INVALID");
        Require(document.VerbatimSections is not null && document.MatchingMetrics is not null && document.MatchingEvidence is not null, "CV_ANALYSIS_SCHEMA_INVALID");

        ValidateVerbatimSections(document.VerbatimSections);
        ValidateMetrics(document.MatchingMetrics);
        ValidateEvidence(document.MatchingEvidence, document.VerbatimSections.ProfessionalExperienceAndProjects, rawText);
        ValidateMetricEvidenceConsistency(document.MatchingMetrics, document.MatchingEvidence.RequirementSignals);
    }

    private static void ValidateVerbatimSections(CvVerbatimSections sections)
    {
        Require(sections.PersonalInfo is not null, "CV_ANALYSIS_SCHEMA_INVALID");
        RequireString(sections.PersonalInfo.Name);
        RequireString(sections.PersonalInfo.Title);
        RequireString(sections.PersonalInfo.Summary);
        RequireString(sections.OtherInformation);

        RequireAtMost(sections.Education, 20);
        RequireAtMost(sections.Languages, 20);
        RequireStringList(sections.SkillsSection, 40);
        RequireAtMost(sections.ProfessionalExperienceAndProjects, 30);
        RequireStringList(sections.CertificationsAndAwards, 20);

        foreach (var education in sections.Education)
        {
            Require(education is not null, "CV_ANALYSIS_SCHEMA_INVALID");
            RequireString(education.Institution);
            RequireString(education.Degree);
            RequireString(education.Major);
            RequireString(education.Timeline);
        }

        foreach (var language in sections.Languages)
        {
            Require(language is not null, "CV_ANALYSIS_SCHEMA_INVALID");
            RequireString(language.Language);
            RequireString(language.CertificationsOrLevel);
        }

        foreach (var entry in sections.ProfessionalExperienceAndProjects)
        {
            Require(entry is not null, "CV_ANALYSIS_SCHEMA_INVALID");
            RequireString(entry.CompanyOrProjectName);
            RequireString(entry.Role);
            RequireString(entry.Timeline);
            Require(EntryTypes.Contains(entry.EntryType), "CV_ANALYSIS_SCHEMA_INVALID");
            RequireStringList(entry.DetailsAndResponsibilities);
            RequireStringList(entry.TechnologiesUsed);
        }
    }

    private static void ValidateMetrics(CvMatchingMetrics metrics)
    {
        RequireStringList(metrics.JobTitlesNormalized);
        RequireStringList(metrics.SkillsNormalized);
        RequireStringList(metrics.Domains);
        Require(metrics.TotalYearsExperience >= 0, "CV_ANALYSIS_SCHEMA_INVALID");
    }

    private static void ValidateEvidence(
        CvMatchingEvidence evidence,
        IReadOnlyList<CvExperienceOrProject> entries,
        string rawText)
    {
        if (evidence.ExperienceSummary is null || evidence.RequirementSignals is null || evidence.SenioritySignals is null)
        {
            throw new CvAnalysisContractException("CV_ANALYSIS_SCHEMA_INVALID");
        }

        var summary = evidence.ExperienceSummary;
        RequireAtMost(evidence.RequirementSignals, 50);
        RequireAtMost(summary.Periods, 30);
        RequireAtMost(evidence.SenioritySignals, 20);
        Require(summary.TotalProfessionalMonths >= 0, "CV_ANALYSIS_SCHEMA_INVALID");
        Require(CalculationBases.Contains(summary.CalculationBasis), "CV_ANALYSIS_SCHEMA_INVALID");

        foreach (var signal in evidence.RequirementSignals)
        {
            if (signal is null) throw new CvAnalysisContractException("CV_ANALYSIS_SCHEMA_INVALID");
            RequireNonEmpty(signal.Name);
            Require(SignalCategories.Contains(signal.Category), "CV_ANALYSIS_SCHEMA_INVALID");
            Require(EvidenceStrengths.Contains(signal.EvidenceStrength), "CV_ANALYSIS_SCHEMA_INVALID");
            ValidateSignalSource(signal.SourceType, signal.SourceIndex, entries);
            Require(signal.Evidence is { Count: >= 1 and <= 3 }, "CV_ANALYSIS_SCHEMA_INVALID");
            foreach (var item in signal.Evidence)
            {
                RequireNonEmpty(item);
                Require(IsEvidenceGrounded(item, rawText), "CV_ANALYSIS_EVIDENCE_NOT_GROUNDED");
            }
        }

        foreach (var period in summary.Periods)
        {
            if (period is null) throw new CvAnalysisContractException("CV_ANALYSIS_SCHEMA_INVALID");
            Require(ProfessionalEntryTypes.Contains(period.EntryType), "CV_ANALYSIS_SCHEMA_INVALID");
            Require(period.SourceIndex >= 0 && period.SourceIndex < entries.Count, "CV_ANALYSIS_SCHEMA_INVALID");
            Require(string.Equals(entries[period.SourceIndex].EntryType, period.EntryType, StringComparison.Ordinal), "CV_ANALYSIS_SCHEMA_INVALID");
            RequireString(period.Organization);
            RequireString(period.Role);
            RequireString(period.TimelineRaw);
            RequireNonEmpty(period.Evidence);
            Require(IsEvidenceGrounded(period.Evidence, rawText), "CV_ANALYSIS_EVIDENCE_NOT_GROUNDED");
            ValidatePeriodDates(period);
        }

        foreach (var signal in evidence.SenioritySignals)
        {
            if (signal is null) throw new CvAnalysisContractException("CV_ANALYSIS_SCHEMA_INVALID");
            Require(SenioritySignalNames.Contains(Normalize(signal.Name)), "CV_ANALYSIS_SCHEMA_INVALID");
            ValidateSignalSource(signal.SourceType, signal.SourceIndex, entries);
            RequireNonEmpty(signal.Evidence);
            Require(IsEvidenceGrounded(signal.Evidence, rawText), "CV_ANALYSIS_EVIDENCE_NOT_GROUNDED");
        }
    }

    private static void ValidateSignalSource(string sourceType, int sourceIndex, IReadOnlyList<CvExperienceOrProject> entries)
    {
        Require(SignalSourceTypes.Contains(sourceType), "CV_ANALYSIS_SCHEMA_INVALID");
        Require(sourceIndex >= 0, "CV_ANALYSIS_SCHEMA_INVALID");

        if (EntryTypes.Contains(sourceType) && sourceType != "unknown")
        {
            Require(sourceIndex < entries.Count, "CV_ANALYSIS_SCHEMA_INVALID");
            Require(string.Equals(entries[sourceIndex].EntryType, sourceType, StringComparison.Ordinal), "CV_ANALYSIS_SCHEMA_INVALID");
            return;
        }

        Require(sourceIndex == 0, "CV_ANALYSIS_SCHEMA_INVALID");
    }

    private static void ValidatePeriodDates(CvExperiencePeriod period)
    {
        RequireDatePair(period.StartYear, period.StartMonth);
        RequireDatePair(period.EndYear, period.EndMonth);

        if (period.IsCurrent)
        {
            Require(period.EndYear is null && period.EndMonth is null, "CV_ANALYSIS_SCHEMA_INVALID");
        }

        if (period.StartYear is not null && period.EndYear is not null)
        {
            var start = ToMonthIndex(period.StartYear.Value, period.StartMonth!.Value);
            var end = ToMonthIndex(period.EndYear.Value, period.EndMonth!.Value);
            Require(end >= start, "CV_ANALYSIS_SCHEMA_INVALID");
        }
    }

    private static void ValidateMetricEvidenceConsistency(CvMatchingMetrics metrics, IReadOnlyCollection<CvRequirementSignal> signals)
    {
        var signalByCategory = signals
            .Where(signal => !string.IsNullOrWhiteSpace(signal.Name))
            .GroupBy(signal => Normalize(signal.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(signal => signal.Category).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);

        foreach (var skill in metrics.SkillsNormalized)
        {
            var normalized = Normalize(skill);
            Require(signalByCategory.TryGetValue(normalized, out var categories) &&
                    categories.Overlaps(new[] { "tech_skill", "domain_knowledge", "language" }),
                "CV_ANALYSIS_SCHEMA_INVALID");
        }

        foreach (var domain in metrics.Domains)
        {
            var normalized = Normalize(domain);
            Require(signalByCategory.TryGetValue(normalized, out var categories) && categories.Contains("domain_knowledge"),
                "CV_ANALYSIS_SCHEMA_INVALID");
        }
    }

    private static void Canonicalize(CvAnalysisDocument document, DateOnly analysisDate)
    {
        document.SchemaVersion = ContractVersion;
        document.MatchingMetrics.JobTitlesNormalized = CanonicalList(document.MatchingMetrics.JobTitlesNormalized);
        document.MatchingMetrics.SkillsNormalized = CanonicalList(document.MatchingMetrics.SkillsNormalized);
        document.MatchingMetrics.Domains = CanonicalList(document.MatchingMetrics.Domains);

        document.MatchingEvidence.RequirementSignals = document.MatchingEvidence.RequirementSignals
            .Select(signal =>
            {
                signal.Name = Normalize(signal.Name);
                return signal;
            })
            .OrderBy(signal => signal.Category, StringComparer.Ordinal)
            .ThenBy(signal => signal.Name, StringComparer.Ordinal)
            .ToList();

        document.MatchingEvidence.SenioritySignals = document.MatchingEvidence.SenioritySignals
            .Select(signal =>
            {
                signal.Name = Normalize(signal.Name);
                return signal;
            })
            .OrderBy(signal => signal.Name, StringComparer.Ordinal)
            .ToList();

        var summary = document.MatchingEvidence.ExperienceSummary;
        var calculation = CalculateExperience(summary.Periods, analysisDate);
        summary.TotalProfessionalMonths = calculation.TotalMonths;
        summary.CalculationBasis = calculation.Basis;
        document.MatchingMetrics.TotalYearsExperience = calculation.TotalMonths / 12;
    }

    private static (int TotalMonths, string Basis) CalculateExperience(IEnumerable<CvExperiencePeriod> periods, DateOnly analysisDate)
    {
        var periodList = periods.ToList();
        var dated = new List<(int Start, int End)>();
        var hasIncompleteTimeline = false;

        foreach (var period in periodList)
        {
            if (period.StartYear is null || period.StartMonth is null)
            {
                hasIncompleteTimeline = true;
                continue;
            }

            int? endYear = period.IsCurrent ? analysisDate.Year : period.EndYear;
            int? endMonth = period.IsCurrent ? analysisDate.Month : period.EndMonth;
            if (endYear is null || endMonth is null)
            {
                hasIncompleteTimeline = true;
                continue;
            }

            dated.Add((ToMonthIndex(period.StartYear.Value, period.StartMonth.Value), ToMonthIndex(endYear.Value, endMonth.Value)));
        }

        if (dated.Count == 0)
        {
            return (0, "insufficient_timeline");
        }

        var ordered = dated.OrderBy(period => period.Start).ThenBy(period => period.End).ToList();
        var total = 0;
        var currentStart = ordered[0].Start;
        var currentEnd = ordered[0].End;

        foreach (var period in ordered.Skip(1))
        {
            if (period.Start <= currentEnd)
            {
                currentEnd = Math.Max(currentEnd, period.End);
                continue;
            }

            total += Math.Max(0, currentEnd - currentStart);
            currentStart = period.Start;
            currentEnd = period.End;
        }

        total += Math.Max(0, currentEnd - currentStart);
        return (total, hasIncompleteTimeline ? "partial_timeline" : "explicit_timeline");
    }

    private static List<string> CanonicalList(IEnumerable<string> values) => values
        .Select(Normalize)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToList();

    private static string Normalize(string value)
    {
        var compact = string.Join(" ", value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return CanonicalNames.TryGetValue(compact, out var canonical) ? canonical : compact;
    }

    private static bool IsEvidenceGrounded(string evidence, string rawText)
    {
        var normalizedEvidence = NormalizeWhitespace(evidence);
        var normalizedRawText = NormalizeWhitespace(rawText);
        return normalizedEvidence.Length > 0 &&
               normalizedRawText.Contains(normalizedEvidence, StringComparison.Ordinal);
    }

    private static string NormalizeWhitespace(string value) => string.Join(
        " ",
        value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool IsSupportedSourceType(string sourceType) =>
        sourceType is "pdf_text" or "docx_text" or "ocr" or "pasted_text";

    private static int ToMonthIndex(int year, int month) => checked((year * 12) + month);

    private static void RequireDatePair(int? year, int? month)
    {
        Require((year is null) == (month is null), "CV_ANALYSIS_SCHEMA_INVALID");
        if (year is not null)
        {
            Require(year is >= 1900 and <= 2200, "CV_ANALYSIS_SCHEMA_INVALID");
            Require(month is >= 1 and <= 12, "CV_ANALYSIS_SCHEMA_INVALID");
        }
    }

    private static void RequireAtMost<T>(IReadOnlyCollection<T>? values, int max) =>
        Require(values is not null && values.Count <= max, "CV_ANALYSIS_SCHEMA_INVALID");

    private static void RequireStringList(IReadOnlyCollection<string>? values, int? max = null)
    {
        if (values is null || (max.HasValue && values.Count > max.Value))
        {
            throw new CvAnalysisContractException("CV_ANALYSIS_SCHEMA_INVALID");
        }

        foreach (var value in values)
        {
            RequireString(value);
        }
    }

    private static void RequireString(string? value) => Require(value is not null, "CV_ANALYSIS_SCHEMA_INVALID");

    private static void RequireNonEmpty(string? value) => Require(!string.IsNullOrWhiteSpace(value), "CV_ANALYSIS_SCHEMA_INVALID");

    private static void Require(bool condition, string failureCode)
    {
        if (!condition)
        {
            throw new CvAnalysisContractException(failureCode);
        }
    }

    private sealed class CvAnalysisContractException : Exception
    {
        public CvAnalysisContractException(string code) => Code = code;

        public string Code { get; }
    }
}
