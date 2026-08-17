using System.Text.Json;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching;

public sealed class HardcodeCvJobPairMatcher : IHardcodeCvJobPairMatcher
{
    private readonly ITHunterviewContext _context;
    private readonly ICvTextExtractorService _cvTextExtractorService;
    private readonly ILogger<HardcodeCvJobPairMatcher> _logger;
    private readonly HardcodeJdRequirementScoringService _hardcodeJdRequirementScoringService;
    private readonly ICvAnalysisResponseValidator _cvAnalysisResponseValidator;
    private readonly Dictionary<Cvs, Task<ParsedMetrics>> _preparedCvMetrics = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<JobPostings, Task<ParsedMetrics>> _preparedJobMetrics = new(ReferenceEqualityComparer.Instance);
    private readonly object _preparationLock = new();

    public HardcodeCvJobPairMatcher(
        ITHunterviewContext context,
        ICvTextExtractorService cvTextExtractorService,
        ILogger<HardcodeCvJobPairMatcher> logger,
        HardcodeJdRequirementScoringService hardcodeJdRequirementScoringService,
        ICvAnalysisResponseValidator cvAnalysisResponseValidator)
    {
        _context = context;
        _cvTextExtractorService = cvTextExtractorService;
        _logger = logger;
        _hardcodeJdRequirementScoringService = hardcodeJdRequirementScoringService;
        _cvAnalysisResponseValidator = cvAnalysisResponseValidator;
    }

    public async Task<HardcodePairMatchResult> MatchAsync(
        Cvs cv,
        JobPostings job,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cvMetrics = await GetPreparedCvMetricsAsync(cv, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var jobMetrics = await GetPreparedJobMetricsAsync(job, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return ProcessMatching(cv, cvMetrics, job, jobMetrics);
    }

    internal async Task PrepareCvAsync(Cvs cv, CancellationToken cancellationToken = default)
    {
        await GetPreparedCvMetricsAsync(cv, cancellationToken);
    }

    internal async Task PrepareJobAsync(JobPostings job, CancellationToken cancellationToken = default)
    {
        await GetPreparedJobMetricsAsync(job, cancellationToken);
    }

    private async Task<ParsedMetrics> GetPreparedCvMetricsAsync(Cvs cv, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task<ParsedMetrics> preparation;
        lock (_preparationLock)
        {
            if (!_preparedCvMetrics.TryGetValue(cv, out preparation!))
            {
                preparation = PrepareCvMetricsAsync(cv, cancellationToken);
                _preparedCvMetrics.Add(cv, preparation);
            }
        }

        try
        {
            return await preparation;
        }
        catch
        {
            lock (_preparationLock)
            {
                if (_preparedCvMetrics.TryGetValue(cv, out var current) &&
                    ReferenceEquals(current, preparation))
                {
                    _preparedCvMetrics.Remove(cv);
                }
            }
            throw;
        }
    }

    private async Task<ParsedMetrics> GetPreparedJobMetricsAsync(JobPostings job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task<ParsedMetrics> preparation;
        lock (_preparationLock)
        {
            if (!_preparedJobMetrics.TryGetValue(job, out preparation!))
            {
                preparation = ExtractJobMetricsAsync(job, cancellationToken);
                _preparedJobMetrics.Add(job, preparation);
            }
        }

        try
        {
            return await preparation;
        }
        catch
        {
            lock (_preparationLock)
            {
                if (_preparedJobMetrics.TryGetValue(job, out var current) &&
                    ReferenceEquals(current, preparation))
                {
                    _preparedJobMetrics.Remove(job);
                }
            }
            throw;
        }
    }

    private async Task<ParsedMetrics> PrepareCvMetricsAsync(Cvs cv, CancellationToken cancellationToken)
    {
        await EnsureCvIsParsedAsync(cv, cancellationToken);
        return ExtractMetrics(cv.ParsedData);
    }

    private static JsonElement? GetJsonElement(string? jsonString, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(jsonString)) return null;
        try
        {
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            if (root.TryGetProperty(fieldName, out var element))
            {
                return element.Clone();
            }

            if (fieldName.Contains("."))
            {
                var parts = fieldName.Split('.');
                var current = root;
                foreach (var part in parts)
                {
                    if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var nextElement))
                    {
                        current = nextElement;
                    }
                    else
                    {
                        return null;
                    }
                }
                return current.Clone();
            }
        }
        catch { }
        return null;
    }

    private static List<string> ExtractJsonArray(string? jsonString, string fieldName)
    {
        var element = GetJsonElement(jsonString, fieldName);
        var result = new List<string>();
        if (element.HasValue && element.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.Value.EnumerateArray())
            {
                var val = item.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    result.Add(val.Trim());
                }
            }
        }
        return result;
    }

    private static int ExtractJsonInt(string? jsonString, string fieldName)
    {
        var element = GetJsonElement(jsonString, fieldName);
        if (element.HasValue && element.Value.ValueKind == JsonValueKind.Number)
        {
            if (element.Value.TryGetInt32(out int val))
                return val;
        }
        return 0;
    }

    private static decimal CalculateSkillsScore(List<string> cvSkills, List<string> jobSkills)
    {
        if (jobSkills == null || jobSkills.Count == 0) return 0.5m;
        if (cvSkills == null || cvSkills.Count == 0) return 0m;

        var cvSet = cvSkills.Select(s => s.ToLower()).ToHashSet();
        var jobSet = jobSkills.Select(s => s.ToLower()).ToHashSet();

        int matchCount = jobSet.Count(j => cvSet.Contains(j));
        return (decimal)matchCount / jobSet.Count;
    }

    private static decimal CalculateTitleScore(List<string> cvTitles, List<string> jobTitles)
    {
        if (jobTitles == null || jobTitles.Count == 0) return 0.5m;
        if (cvTitles == null || cvTitles.Count == 0) return 0m;

        var cvSet = cvTitles.Select(s => s.ToLower()).ToHashSet();
        var jobSet = jobTitles.Select(s => s.ToLower()).ToHashSet();

        if (jobSet.Any(j => cvSet.Contains(j))) return 1.0m;
        return 0m;
    }

    private static decimal CalculateExperienceScore(int cvYears, int jobYears)
    {
        if (jobYears <= 0) return 0.5m;
        if (cvYears >= jobYears) return 1.0m;
        return (decimal)cvYears / jobYears;
    }

    private static decimal CalculateDomainScore(List<string> cvDomains, List<string> jobDomains)
    {
        if (jobDomains == null || jobDomains.Count == 0) return 0.5m;
        if (cvDomains == null || cvDomains.Count == 0) return 0m;

        var cvSet = cvDomains.Select(s => s.ToLower()).ToHashSet();
        var jobSet = jobDomains.Select(s => s.ToLower()).ToHashSet();

        int matchCount = jobSet.Count(j => cvSet.Contains(j));
        if (matchCount > 0) return 1.0m;
        return 0.3m;
    }

    private sealed class ParsedMetrics
    {
        public List<string> Titles { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public int Exp { get; set; }
        public List<string> Domains { get; set; } = new();
        public bool TitleAvailable { get; set; }
        public bool SkillsAvailable { get; set; }
        public bool ExperienceAvailable { get; set; }
        public bool DomainsAvailable { get; set; }
    }

    private static ParsedMetrics ExtractMetrics(string? parsedData)
    {
        var metrics = JobAnalysisMetricsReader.Read(parsedData);
        return new ParsedMetrics
        {
            Titles = metrics.Titles,
            Skills = metrics.Skills,
            Exp = metrics.TotalYearsExperience,
            Domains = metrics.Domains,
            TitleAvailable = metrics.TitleAvailable,
            SkillsAvailable = metrics.SkillsAvailable,
            ExperienceAvailable = metrics.ExperienceAvailable,
            DomainsAvailable = metrics.DomainsAvailable
        };
    }

    private async Task<ParsedMetrics> ExtractJobMetricsAsync(
        JobPostings job,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metrics = ExtractMetrics(job.ParsedData);
        if (metrics.Skills.Count > 0)
        {
            return metrics;
        }

        // Older parsed documents may have no usable skill array. The normalized
        // recruiter-approved tags are the safe compatibility fallback.
        metrics.Skills = await (
            from requirement in _context.JobSkillRequirements.AsNoTracking()
            join skill in _context.Skills.AsNoTracking() on requirement.SkillId equals skill.Id
            where requirement.JobId == job.Id
            select skill.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
        metrics.SkillsAvailable = metrics.Skills.Count > 0;

        return metrics;
    }

    private HardcodePairMatchResult ProcessMatching(
        Cvs cv,
        ParsedMetrics cvMetrics,
        JobPostings job,
        ParsedMetrics jobMetrics)
    {
        var titleScore = CalculateTitleScore(cvMetrics.Titles, jobMetrics.Titles);
        var scoringDecision = _hardcodeJdRequirementScoringService.Evaluate(job.ParsedData, cvMetrics.Skills);
        if (scoringDecision.FailureCode != null)
        {
            if (!scoringDecision.CanUseLegacyCompatibilityFallback)
            {
                _logger.LogWarning(
                    "Hardcode matching completed without a score for job {JobId}: structured JD analysis has no usable groups.",
                    job.Id);
                return WriteUnscoredHardcodeResult(
                    cv,
                    scoringDecision.Projection,
                    scoringDecision.Evaluation,
                    "STRUCTURED_JD_UNAVAILABLE");
            }

            _logger.LogWarning(
                "Hardcode matching ignored invalid effective JD analysis for job {JobId}; using compatibility metrics.",
                job.Id);
        }
        var projection = scoringDecision.Projection;
        var groupEvaluation = scoringDecision.Evaluation;
        if (projection is { Quality: JdAnalysisQuality.PARTIAL } && groupEvaluation is not null)
        {
            return WriteUnscoredHardcodeResult(
                cv,
                projection,
                groupEvaluation,
                "PARTIAL_REQUIREMENT_SET");
        }
        var hasStructuredTechnicalTargets = groupEvaluation?.Outcomes.Any(
            outcome => outcome.EvaluatedBySkillComponent) == true;
        var skillsScore = hasStructuredTechnicalTargets
            ? groupEvaluation!.SkillScore
            : CalculateSkillsScore(cvMetrics.Skills, jobMetrics.Skills);
        var expScore = CalculateExperienceScore(cvMetrics.Exp, jobMetrics.Exp);
        var domainScore = CalculateDomainScore(cvMetrics.Domains, jobMetrics.Domains);
        var availableWeight = 0m;
        var weightedScore = 0m;
        var availableDimensions = new List<string>(4);
        AddAvailableDimension(cvMetrics.TitleAvailable && jobMetrics.TitleAvailable, "title", 0.15m, titleScore, availableDimensions, ref availableWeight, ref weightedScore);
        AddAvailableDimension(
            cvMetrics.SkillsAvailable && (hasStructuredTechnicalTargets || jobMetrics.SkillsAvailable),
            "skills",
            0.45m,
            skillsScore,
            availableDimensions,
            ref availableWeight,
            ref weightedScore);
        AddAvailableDimension(cvMetrics.ExperienceAvailable && jobMetrics.ExperienceAvailable && jobMetrics.Exp > 0, "experience", 0.30m, expScore, availableDimensions, ref availableWeight, ref weightedScore);
        AddAvailableDimension(cvMetrics.DomainsAvailable && jobMetrics.DomainsAvailable, "domain", 0.10m, domainScore, availableDimensions, ref availableWeight, ref weightedScore);

        if (availableWeight == 0m)
        {
            return WriteUnscoredHardcodeResult(
                cv,
                projection,
                groupEvaluation,
                "NO_SAFE_DIMENSIONS");
        }

        var finalScore = (weightedScore / availableWeight) * 100m;
        var scoreBasis = availableWeight == 1m ? "complete_cv_metrics" : "available_cv_metrics";

        var details = JsonSerializer.Serialize(new
        {
            Method = groupEvaluation == null ? "Hardcode" : "HardcodeV3",
            JdSchemaVersion = projection?.SourceSchemaVersion,
            TitleScore = Math.Round(titleScore * 100m, 2),
            SkillsScore = Math.Round(skillsScore * 100m, 2),
            ExperienceScore = Math.Round(expScore * 100m, 2),
            DomainScore = Math.Round(domainScore * 100m, 2),
            FinalScore = Math.Round(finalScore, 2),
            ScoreBasis = scoreBasis,
            CvAnalysisQuality = cv.AnalysisQuality?.ToString(),
            AvailableDimensions = availableDimensions,
            Weights = new { TitleWeight = 0.15m, SkillsWeight = 0.45m, ExperienceWeight = 0.30m, DomainWeight = 0.10m },
            GroupOutcomes = groupEvaluation?.Outcomes
        });

        return CreateResult(cv, finalScore, details);
    }

    private static void AddAvailableDimension(
        bool available,
        string name,
        decimal weight,
        decimal score,
        ICollection<string> dimensions,
        ref decimal totalWeight,
        ref decimal weightedScore)
    {
        if (!available) return;
        dimensions.Add(name);
        totalWeight += weight;
        weightedScore += weight * score;
    }

    private static HardcodePairMatchResult WriteUnscoredHardcodeResult(
        Cvs cv,
        JdRequirementProjection? projection,
        JdHardcodeRequirementEvaluation? groupEvaluation,
        string reasonCode)
    {
        var details = JsonSerializer.Serialize(new
        {
            Method = "Hardcode",
            ScoreBasis = "no_safe_dimensions",
            ResultCode = "SCORE_UNAVAILABLE",
            InternalReasonCode = reasonCode,
            CvAnalysisQuality = cv.AnalysisQuality?.ToString(),
            JdAnalysisQuality = projection?.AnalysisQuality,
            JdSchemaVersion = projection?.SourceSchemaVersion,
            GroupOutcomes = groupEvaluation?.Outcomes
        });
        return CreateResult(cv, null, details);
    }

    private static HardcodePairMatchResult CreateResult(Cvs cv, decimal? matchScore, string matchDetails) => new(
        matchScore,
        matchDetails,
        cv.AnalysisQuality,
        cv.AnalysisCoverageJson,
        cv.AnalysisDiagnosticsJson);

    private async Task EnsureCvIsParsedAsync(Cvs cv, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(cv.ParsedData) && cv.ParseStatus == "SUCCESS")
        {
            var stored = ValidateStoredCvJson(cv.ParsedData);
            if (stored.IsUsable)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ApplyValidatedCv(cv, stored);
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }
        }

        _logger.LogInformation("On-demand parsing CV {CvId} in hardcode matching.", cv.Id);
        try
        {
            var parsedData = cancellationToken.CanBeCanceled
                ? await _cvTextExtractorService.ExtractParsedDataFromUrlAsync(
                    cv.FileUrl,
                    cv.RawText,
                    cancellationToken)
                : await _cvTextExtractorService.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText);
            cancellationToken.ThrowIfCancellationRequested();
            var validation = ValidateStoredCvJson(parsedData);
            if (!validation.IsUsable)
            {
                throw new CvAnalysisValidationException(validation);
            }

            cancellationToken.ThrowIfCancellationRequested();
            ApplyValidatedCv(cv, validation);
            cv.ParseStatus = "SUCCESS";
            cv.ParseError = null;
            _context.Cvs.Update(cv);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            cv.ParseStatus = "FAILED";
            cv.ParseError = "CV_ANALYSIS_INVALID_FOR_MATCHING";
            cv.AnalysisQuality = CvAnalysisQuality.INVALID;
            cv.AnalysisCoverageJson = null;
            cv.AnalysisDiagnosticsJson = null;
            _context.Cvs.Update(cv);
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static void ApplyValidatedCv(Cvs cv, CvAnalysisValidationResult validation)
    {
        cv.ParsedData = validation.CanonicalJson;
        cv.AnalysisQuality = validation.Quality;
        cv.AnalysisCoverageJson = CvAnalysisMetadataReader.SerializeCoverage(validation.Coverage);
        cv.AnalysisDiagnosticsJson = CvAnalysisMetadataReader.SerializeDiagnostics(validation.Diagnostics);
    }

    private CvAnalysisValidationResult ValidateStoredCvJson(string canonicalJson) =>
        _cvAnalysisResponseValidator is ICvAnalysisRecoveryAwareValidator recoveryAware
            ? recoveryAware.ValidateStoredCanonical(canonicalJson)
            : _cvAnalysisResponseValidator.ValidateAndCanonicalize(canonicalJson);
}
