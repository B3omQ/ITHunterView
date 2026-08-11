using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record CvStageTwoContext(
    string Json,
    CvAnalysisQuality Quality,
    CvAnalysisCoverage? Coverage,
    IReadOnlyList<CvAnalysisDiagnostic> Diagnostics);

/// <summary>
/// Selects a deterministic, bounded subset of the typed CV analysis and then
/// serializes it. It never slices a serialized JSON document mid-token.
/// </summary>
public sealed class CvStageTwoContextBuilder
{
    public const string InvalidCvMatchingContext = "INVALID_CV_MATCHING_CONTEXT";
    private readonly CvAnalysisResponseValidator _validator;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    public CvStageTwoContextBuilder()
        : this(new CvAnalysisResponseValidator())
    {
    }

    public CvStageTwoContextBuilder(CvAnalysisResponseValidator validator)
    {
        _validator = validator;
    }

    public CvStageTwoContext Build(string canonicalCvJson)
    {
        if (string.IsNullOrWhiteSpace(canonicalCvJson))
        {
            throw Invalid();
        }

        try
        {
            var validation = _validator.ValidateAndCanonicalize(canonicalCvJson);
            if (!validation.IsUsable)
            {
                throw Invalid();
            }
            var cv = JsonSerializer.Deserialize<CvAnalysisDocument>(validation.CanonicalJson, SerializerOptions)
                ?? throw Invalid();
            if (!string.Equals(cv.SchemaVersion, "cv-analysis/v2", StringComparison.Ordinal) ||
                cv.VerbatimSections is null || cv.MatchingMetrics is null || cv.MatchingEvidence is null)
            {
                throw Invalid();
            }

            var sections = cv.VerbatimSections;
            var metrics = cv.MatchingMetrics;
            var evidence = cv.MatchingEvidence;
            var experienceSummary = evidence.ExperienceSummary ?? new CvExperienceSummary();
            var coverage = validation.Coverage;
            var warningCodes = validation.Diagnostics
                .Select(diagnostic => diagnostic.Code)
                .Distinct(StringComparer.Ordinal)
                .Take(20)
                .ToArray();

            var context = new
            {
                schema_version = "matching-context/v1",
                source_cv_schema_version = cv.SchemaVersion,
                cv_analysis = new
                {
                    quality = validation.Quality.ToString(),
                    title_metrics_available = coverage?.TitleMetricsAvailable ?? false,
                    skill_metrics_available = coverage?.SkillMetricsAvailable ?? false,
                    experience_metric_available = coverage?.ExperienceMetricAvailable ?? false,
                    domain_metrics_available = coverage?.DomainMetricsAvailable ?? false,
                    warning_codes = warningCodes
                },
                candidate = new
                {
                    title = Clip(sections.PersonalInfo?.Title, 300),
                    summary = Clip(sections.PersonalInfo?.Summary, 1_500),
                    education = (sections.Education ?? new List<CvEducation>())
                        .Take(10)
                        .Select(entry => new
                        {
                            institution = Clip(entry.Institution, 250),
                            degree = Clip(entry.Degree, 250),
                            major = Clip(entry.Major, 250),
                            timeline = Clip(entry.Timeline, 100)
                        }),
                    languages = (sections.Languages ?? new List<CvLanguage>())
                        .Take(12)
                        .Select(language => new
                        {
                            language = Clip(language.Language, 100),
                            certifications_or_level = Clip(language.CertificationsOrLevel, 200)
                        }),
                    skills_section = (sections.SkillsSection ?? new List<string>())
                        .Select(skill => Clip(skill, 120))
                        .Where(skill => skill.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(80),
                    professional_experience_and_projects = (sections.ProfessionalExperienceAndProjects ?? new List<CvExperienceOrProject>())
                        .Take(15)
                        .Select(entry => new
                        {
                            company_or_project_name = Clip(entry.CompanyOrProjectName, 200),
                            role = Clip(entry.Role, 200),
                            timeline = Clip(entry.Timeline, 100),
                            entry_type = Clip(entry.EntryType, 50),
                            details_and_responsibilities = (entry.DetailsAndResponsibilities ?? new List<string>())
                                .Select(detail => Clip(detail, 400))
                                .Where(detail => detail.Length > 0)
                                .Take(10),
                            technologies_used = (entry.TechnologiesUsed ?? new List<string>())
                                .Select(technology => Clip(technology, 120))
                                .Where(technology => technology.Length > 0)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .Take(30)
                        })
                },
                matching_metrics = new
                {
                    job_titles_normalized = (metrics.JobTitlesNormalized ?? new List<string>()).Take(20),
                    skills_normalized = (metrics.SkillsNormalized ?? new List<string>()).Take(120),
                    total_years_exp = metrics.TotalYearsExperience,
                    domains = (metrics.Domains ?? new List<string>()).Take(30)
                },
                matching_evidence = new
                {
                    requirement_signals = (evidence.RequirementSignals ?? new List<CvRequirementSignal>())
                        .Take(100)
                        .Select(signal => new
                        {
                            name = Clip(signal.Name, 150),
                            category = Clip(signal.Category, 50),
                            evidence_strength = Clip(signal.EvidenceStrength, 50),
                            source_type = Clip(signal.SourceType, 50),
                            source_index = signal.SourceIndex,
                            evidence = (signal.Evidence ?? new List<string>()).Select(value => Clip(value, 350)).Where(value => value.Length > 0).Take(5)
                        }),
                    experience_summary = new
                    {
                        total_professional_months = experienceSummary.TotalProfessionalMonths,
                        calculation_basis = Clip(experienceSummary.CalculationBasis, 250),
                        periods = (experienceSummary.Periods ?? new List<CvExperiencePeriod>()).Take(20).Select(period => new
                        {
                            source_index = period.SourceIndex,
                            entry_type = Clip(period.EntryType, 50),
                            organization = Clip(period.Organization, 200),
                            role = Clip(period.Role, 200),
                            timeline_raw = Clip(period.TimelineRaw, 100),
                            start_year = period.StartYear,
                            start_month = period.StartMonth,
                            end_year = period.EndYear,
                            end_month = period.EndMonth,
                            is_current = period.IsCurrent,
                            evidence = Clip(period.Evidence, 350)
                        })
                    },
                    seniority_signals = (evidence.SenioritySignals ?? new List<CvSenioritySignal>()).Take(40).Select(signal => new
                    {
                        name = Clip(signal.Name, 150),
                        source_type = Clip(signal.SourceType, 50),
                        source_index = signal.SourceIndex,
                        evidence = Clip(signal.Evidence, 350)
                    })
                }
            };

            return new CvStageTwoContext(
                JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true }),
                validation.Quality,
                coverage,
                validation.Diagnostics);
        }
        catch (InvalidOperationException exception) when (exception.Message == InvalidCvMatchingContext)
        {
            throw;
        }
        catch (Exception)
        {
            throw Invalid();
        }
    }

    private static string Clip(string? value, int maximumLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static InvalidOperationException Invalid() => new(InvalidCvMatchingContext);
}
