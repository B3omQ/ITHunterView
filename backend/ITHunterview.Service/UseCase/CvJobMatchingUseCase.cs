using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Pgvector;
using System.Collections.Generic;
using ITHunterview.Service.DTOs.Cv.Matching;
using Microsoft.Extensions.Logging;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.UseCase
{

    public class CvJobMatchingUseCase : ICvJobMatchingUseCase, ICvJdOneToOneMatchingEngine
    {
        private readonly ITHunterviewContext _context;
        private readonly IAiEmbeddingService _aiService;
        private readonly ICvTextExtractorService _cvTextExtractorService;
        private readonly ILogger<CvJobMatchingUseCase> _logger;
        private readonly IPromptManagementService _promptManagementService;
        private readonly IJdStageTwoMatchingService _jdStageTwoMatchingService;
        private readonly IJobAnalysisExtractionService? _jobAnalysisExtractionService;
        private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;
        private readonly IMatchingInputPreflightUseCase _matchingInputPreflightUseCase;
        private readonly IMatchingSourceRepository _matchingSourceRepository;
        private readonly ICvAnalysisResponseValidator _cvAnalysisResponseValidator;
        private readonly IJdRequirementProjector _jdRequirementProjector;
        private readonly CvStageTwoContextBuilder _cvStageTwoContextBuilder;
        private readonly IJobAnalysisInputBuilder _jobAnalysisInputBuilder;
        private readonly IMatchingCvPreparationService? _matchingCvPreparationService;
        private readonly IMatchingJdPreparationService? _matchingJdPreparationService;
        private readonly IRawJdFallbackMatchingService? _rawJdFallbackMatchingService;
        private readonly IJdMatchReportReader _jdMatchReportReader;

        public CvJobMatchingUseCase(
            ITHunterviewContext context, 
            IAiEmbeddingService aiService,
            ICvTextExtractorService cvTextExtractorService,
            ILogger<CvJobMatchingUseCase> logger,
            IPromptManagementService promptManagementService,
            IAiService textAiService,
            ICandidateFeatureUsageUseCase featureUsageUseCase,
            IMatchingInputPreflightUseCase matchingInputPreflightUseCase,
            IMatchingSourceRepository matchingSourceRepository,
            ICvAnalysisResponseValidator cvAnalysisResponseValidator,
            IJobAnalysisExtractionService? jobAnalysisExtractionService = null,
            IJdRequirementProjector? jdRequirementProjector = null,
            IJdStageTwoMatchingService? jdStageTwoMatchingService = null,
            CvStageTwoContextBuilder? cvStageTwoContextBuilder = null,
            IJobAnalysisInputBuilder? jobAnalysisInputBuilder = null,
            IMatchingCvPreparationService? matchingCvPreparationService = null,
            IMatchingJdPreparationService? matchingJdPreparationService = null,
            IRawJdFallbackMatchingService? rawJdFallbackMatchingService = null,
            IJdMatchReportReader? jdMatchReportReader = null)
        {
            _context = context;
            _aiService = aiService;
            _cvTextExtractorService = cvTextExtractorService;
            _logger = logger;
            _promptManagementService = promptManagementService;
            _featureUsageUseCase = featureUsageUseCase;
            _matchingInputPreflightUseCase = matchingInputPreflightUseCase;
            _matchingSourceRepository = matchingSourceRepository;
            _cvAnalysisResponseValidator = cvAnalysisResponseValidator;
            _jobAnalysisExtractionService = jobAnalysisExtractionService;
            _jdRequirementProjector = jdRequirementProjector ?? new JdRequirementProjector();
            _jdStageTwoMatchingService = jdStageTwoMatchingService ??
                new JdStageTwoMatchingService(textAiService, new Microsoft.Extensions.Logging.Abstractions.NullLogger<JdStageTwoMatchingService>());
            _cvStageTwoContextBuilder = cvStageTwoContextBuilder ?? new CvStageTwoContextBuilder();
            _jobAnalysisInputBuilder = jobAnalysisInputBuilder ?? new JobAnalysisInputBuilder();
            _matchingCvPreparationService = matchingCvPreparationService;
            _matchingJdPreparationService = matchingJdPreparationService;
            _rawJdFallbackMatchingService = rawJdFallbackMatchingService;
            _jdMatchReportReader = jdMatchReportReader ?? new JdMatchReportReader();
        }

        public string ExtractJsonField(string? jsonString, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) return string.Empty;
            try
            {
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;
                
                JsonElement current = root;
                
                // For deep nested properties like "matching_metrics.skills"
                if (fieldName.Contains("."))
                {
                    var parts = fieldName.Split('.');
                    foreach (var part in parts)
                    {
                        if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var nextElement))
                        {
                            current = nextElement;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                }
                else
                {
                    if (!root.TryGetProperty(fieldName, out current))
                    {
                        return string.Empty;
                    }
                }

                if (current.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<string>();
                    foreach (var item in current.EnumerateArray())
                    {
                        var str = item.ValueKind == JsonValueKind.String
                            ? item.GetString()
                            : item.ToString();
                        if (!string.IsNullOrWhiteSpace(str)) items.Add(str);
                    }
                    return string.Join(", ", items);
                }

                return current.ToString() ?? string.Empty;
            }
            catch
            {
                // Ignore parse errors, just return empty
            }
            return string.Empty;
        }

        private async Task EnsureCvIsParsedAsync(Cvs cv)
        {
            if (!string.IsNullOrWhiteSpace(cv.ParsedData) && cv.ParseStatus == "SUCCESS")
            {
                var stored = ValidateAndCanonicalizeStoredCv(cv);
                if (stored.IsUsable)
                {
                    await _context.SaveChangesAsync();
                    return;
                }
            }

            _logger.LogInformation("On-demand parsing CV {CvId} in AI matching.", cv.Id);
            var parsedData = await _cvTextExtractorService.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText);
            var validation = _cvAnalysisResponseValidator.ValidateAndCanonicalize(parsedData);
            if (!validation.IsUsable) throw new CvAnalysisValidationException(validation);

            cv.ParsedData = validation.CanonicalJson;
            ApplyCvAnalysisMetadata(cv, validation);
            _context.Cvs.Update(cv);
            await _context.SaveChangesAsync();
        }

        private CvAnalysisValidationResult ValidateAndCanonicalizeStoredCv(Cvs cv)
        {
            if (string.IsNullOrWhiteSpace(cv.ParsedData))
            {
                return CvAnalysisValidationResult.Invalid(
                    "CV_ANALYSIS_EMPTY_OUTPUT",
                    "EMPTY_STORED_ANALYSIS",
                    "$");
            }

            var result = _cvAnalysisResponseValidator.ValidateAndCanonicalize(cv.ParsedData);
            if (!result.IsUsable)
            {
                _logger.LogWarning("Stored CV analysis requires reparsing. CvId={CvId}; FailureCode={FailureCode}", cv.Id, result.FailureCode);
                return result;
            }

            cv.ParsedData = result.CanonicalJson;
            ApplyCvAnalysisMetadata(cv, result);
            return result;
        }

        private static void ApplyCvAnalysisMetadata(Cvs cv, CvAnalysisValidationResult validation)
        {
            cv.AnalysisQuality = validation.Quality;
            cv.AnalysisCoverageJson = CvAnalysisMetadataReader.SerializeCoverage(validation.Coverage);
            cv.AnalysisDiagnosticsJson = CvAnalysisMetadataReader.SerializeDiagnostics(validation.Diagnostics);
            cv.ParseStatus = "SUCCESS";
            cv.ParseError = null;
            cv.UpdatedAt = DateTime.UtcNow;
        }

        private async Task GenerateEmbeddingsForCvAsync(Cvs cv)
        {
            await EnsureCvIsParsedAsync(cv);

            bool updated = false;
            
            if (cv.TitleEmbedding == null)
            {
                var titleText = ExtractJsonField(cv.ParsedData, "matching_metrics.job_titles_normalized");
                if (string.IsNullOrEmpty(titleText)) titleText = "Unknown Title";
                cv.TitleEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(titleText));
                updated = true;
            }
            if (cv.SkillsEmbedding == null)
            {
                var skillsText = ExtractJsonField(cv.ParsedData, "matching_metrics.skills_normalized");
                if (string.IsNullOrEmpty(skillsText)) skillsText = "No skills provided";
                cv.SkillsEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(skillsText));
                updated = true;
            }
            if (cv.ExperienceEmbedding == null)
            {
                var expText = ExtractJsonField(cv.ParsedData, "matching_metrics.total_years_exp");
                if (string.IsNullOrEmpty(expText)) expText = "No experience provided";
                else expText += " years";
                cv.ExperienceEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(expText));
                updated = true;
            }
            if (cv.DomainEmbedding == null)
            {
                // Fallback to experience if domain is missing
                var domainText = ExtractJsonField(cv.ParsedData, "matching_metrics.domains");
                if (string.IsNullOrEmpty(domainText)) domainText = ExtractJsonField(cv.ParsedData, "matching_metrics.total_years_exp");
                if (string.IsNullOrEmpty(domainText)) domainText = "Unknown domain";
                cv.DomainEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(domainText));
                updated = true;
            }

            if (updated)
            {
                _context.Cvs.Update(cv);
                await _context.SaveChangesAsync();
            }
        }

        private async Task GenerateEmbeddingsForJobAsync(JobPostings job)
        {
            bool updated = false;
            var metrics = JobAnalysisMetricsReader.Read(job.ParsedData);
            var skillNames = metrics.Skills;
            if (skillNames.Count == 0)
            {
                skillNames = await (
                    from requirement in _context.JobSkillRequirements.AsNoTracking()
                    join skill in _context.Skills.AsNoTracking() on requirement.SkillId equals skill.Id
                    where requirement.JobId == job.Id
                    select skill.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToListAsync();
            }
            
            if (job.TitleEmbedding == null)
            {
                var titleText = string.Join(", ", metrics.Titles);
                if (string.IsNullOrEmpty(titleText)) titleText = job.Title ?? "Unknown Title";
                job.TitleEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(titleText));
                updated = true;
            }
            if (job.SkillsEmbedding == null)
            {
                var skillsText = string.Join(", ", skillNames);
                if (string.IsNullOrEmpty(skillsText)) skillsText = JobPostingRichText.ToPlainText(job.Requirements);
                if (string.IsNullOrEmpty(skillsText)) skillsText = "No requirements provided";
                job.SkillsEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(skillsText));
                updated = true;
            }
            if (job.ExperienceEmbedding == null)
            {
                var expText = metrics.TotalYearsExperience > 0
                    ? $"{metrics.TotalYearsExperience} years"
                    : JobPostingRichText.ToPlainText(job.Requirements);
                if (string.IsNullOrEmpty(expText)) expText = "No requirements provided";
                job.ExperienceEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(expText));
                updated = true;
            }
            if (job.DomainEmbedding == null)
            {
                var domainText = string.Join(", ", metrics.Domains);
                if (string.IsNullOrEmpty(domainText)) domainText = JobPostingRichText.ToPlainText(job.Description);
                if (string.IsNullOrEmpty(domainText)) domainText = "Unknown domain";
                job.DomainEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(domainText));
                updated = true;
            }

            if (updated)
            {
                _context.JobPostings.Update(job);
                await _context.SaveChangesAsync();
            }
        }

        public decimal CalculateComponentScore(Vector? v1, Vector? v2)
        {
            if (v1 == null || v2 == null) return 0m;
            
            var arr1 = v1.ToArray();
            var arr2 = v2.ToArray();
            
            if (arr1.Length != arr2.Length || arr1.Length == 0) return 0m;

            double dot = 0, mag1 = 0, mag2 = 0;
            for(int i = 0; i < arr1.Length; i++)
            {
                dot += arr1[i] * arr2[i];
                mag1 += arr1[i] * arr1[i];
                mag2 += arr2[i] * arr2[i];
            }

            if (mag1 == 0 || mag2 == 0) return 0m;

            var similarity = (decimal)(dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2)));
            return similarity < 0 ? 0 : similarity;
        }

        public async Task MatchCvWithAllJobsAsync(Guid cvId, Guid userId)
        {
            var cv = await _context.Cvs.FindAsync(cvId);
            if (cv == null) throw new Exception("CV not found");

            await GenerateEmbeddingsForCvAsync(cv);

            var existingScores = await _context.CvJobMatchScores
                .Where(s => s.CvId == cvId && s.UserId == userId)
                .ToDictionaryAsync(s => s.JobId);

            // Fetch all jobs that have embeddings and are successfully parsed
            var jobs = await _context.JobPostings.AsNoTracking()
                .Where(j => j.Status == ITHunterview.Domain.Enums.JobStatus.PUBLISHED && j.ParseStatus == "SUCCESS" && j.TitleEmbedding != null && j.SkillsEmbedding != null && j.ExperienceEmbedding != null && j.DomainEmbedding != null)
                .ToListAsync();

            var matchScores = new List<CvJobMatchScores>();

            foreach (var job in jobs)
            {
                existingScores.TryGetValue(job.Id, out var existingScore);

                if (existingScore != null && existingScore.Status != "Pending")
                {
                    continue;
                }

                var titleScore = CalculateComponentScore(cv.TitleEmbedding, job.TitleEmbedding);
                var skillsScore = CalculateComponentScore(cv.SkillsEmbedding, job.SkillsEmbedding);
                var expScore = CalculateComponentScore(cv.ExperienceEmbedding, job.ExperienceEmbedding);
                var domainScore = CalculateComponentScore(cv.DomainEmbedding, job.DomainEmbedding);

                var finalScore = (titleScore * 0.15m) +
                                 (skillsScore * 0.45m) +
                                 (expScore * 0.30m) +
                                 (domainScore * 0.10m);

                var details = JsonSerializer.Serialize(new 
                {
                    TitleScore = Math.Round(titleScore, 4),
                    SkillsScore = Math.Round(skillsScore, 4),
                    ExperienceScore = Math.Round(expScore, 4),
                    DomainScore = Math.Round(domainScore, 4),
                    FinalScore = Math.Round(finalScore, 4),
                    Weights = new { TitleWeight = 0.15m, SkillsWeight = 0.45m, ExperienceWeight = 0.30m, DomainWeight = 0.10m }
                });

                if (existingScore != null)
                {
                    existingScore.MatchScore = finalScore;
                    existingScore.UpdatedAt = DateTime.UtcNow;
                    existingScore.MatchDetails = details;
                    existingScore.Status = "Completed";
                }
                else
                {
                    _context.CvJobMatchScores.Add(new CvJobMatchScores
                    {
                        UserId = userId,
                        CvId = cvId,
                        JobId = job.Id,
                        RawJdText = job.Title,
                        MatchScore = finalScore,
                        MatchDetails = details,
                        Status = "Completed",
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task MatchJobWithAllCvsAsync(Guid jobId, Guid userId)
        {
            var job = await _context.JobPostings.FindAsync(jobId);
            if (job == null) throw new Exception("Job not found");
            if (job.ParseStatus != "SUCCESS") throw new Exception($"Job posting is currently in status '{job.ParseStatus ?? "PENDING"}'. AI analysis must complete before matching.");

            await GenerateEmbeddingsForJobAsync(job);

            var existingScores = await _context.CvJobMatchScores
                .Where(s => s.JobId == jobId) // Fix Duplicate Bug: Bỏ lọc theo Recruiter UserId
                .ToDictionaryAsync(s => s.CvId);

            var cvs = await _context.Cvs
                .Include(c => c.User)
                .ThenInclude(u => u.CandidateProfile)
                .AsNoTracking()
                .Where(c => c.IsPrimary 
                         && c.User.CandidateProfile != null 
                         && c.User.CandidateProfile.IsVisibleToRecruiters == true // Fix Privacy Bug
                         && c.ParseStatus == "SUCCESS" 
                         && c.TitleEmbedding != null 
                         && c.SkillsEmbedding != null 
                         && c.ExperienceEmbedding != null 
                         && c.DomainEmbedding != null)
                .ToListAsync();

            foreach (var cv in cvs)
            {
                existingScores.TryGetValue(cv.Id, out var existingScore);

                if (existingScore != null && existingScore.Status != "Pending")
                {
                    continue;
                }

                var titleScore = CalculateComponentScore(cv.TitleEmbedding, job.TitleEmbedding);
                var skillsScore = CalculateComponentScore(cv.SkillsEmbedding, job.SkillsEmbedding);
                var expScore = CalculateComponentScore(cv.ExperienceEmbedding, job.ExperienceEmbedding);
                var domainScore = CalculateComponentScore(cv.DomainEmbedding, job.DomainEmbedding);

                var finalScore = (titleScore * 0.15m) +
                                 (skillsScore * 0.45m) +
                                 (expScore * 0.30m) +
                                 (domainScore * 0.10m);

                var details = JsonSerializer.Serialize(new 
                {
                    TitleScore = Math.Round(titleScore, 4),
                    SkillsScore = Math.Round(skillsScore, 4),
                    ExperienceScore = Math.Round(expScore, 4),
                    DomainScore = Math.Round(domainScore, 4),
                    FinalScore = Math.Round(finalScore, 4),
                    Weights = new { TitleWeight = 0.15m, SkillsWeight = 0.45m, ExperienceWeight = 0.30m, DomainWeight = 0.10m }
                });

                if (existingScore != null)
                {
                    existingScore.MatchScore = finalScore;
                    existingScore.UpdatedAt = DateTime.UtcNow;
                    existingScore.MatchDetails = details;
                    existingScore.Status = "Completed";
                }
                else
                {
                    _context.CvJobMatchScores.Add(new CvJobMatchScores
                    {
                        UserId = cv.UserId, // Fix Wrong ID Bug: Lưu ID của Candidate thay vì Recruiter
                        CvId = cv.Id,
                        JobId = jobId,
                        RawJdText = job.Title,
                        MatchScore = finalScore,
                        MatchDetails = details,
                        Status = "Completed",
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Guid> SubmitMatchingJobAsync(Guid userId, PreparedMatchingRequest request, Guid? operationId = null)
        {
            var savedCv = request.Cv as PreparedSavedCvSource;
            var rawCv = request.Cv as PreparedRawCvSource;
            var savedJob = request.Jd as PreparedSavedJdSource;
            var rawJd = request.Jd as PreparedRawJdSource;

            var matchScore = new CvJobMatchScores
            {
                Id = operationId ?? Guid.NewGuid(),
                UserId = userId,
                CvId = savedCv?.CvId,
                CvFileName = savedCv?.FileName ?? rawCv?.FileName ?? "Pasted CV",
                JobId = savedJob?.JobId,
                JdTitle = savedJob?.Title ?? rawJd?.Title ?? "Pasted JD",
                RawJdText = null,
                MatchScore = 0,
                Status = "Pending",
                UpdatedAt = DateTime.UtcNow
            };

            _context.CvJobMatchScores.Add(matchScore);
            await _context.SaveChangesAsync();
            return matchScore.Id;
        }

        public async Task<CvJdMatchingExecutionResult> ExecuteAsync(
            Guid matchId,
            MatchingInputSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            var job = await _context.CvJobMatchScores
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == matchId && x.MatchType == "AI", cancellationToken);
            if (job is null)
                throw new KeyNotFoundException("MATCHING_JOB_NOT_FOUND");

            return await ProcessMatchingJobCoreAsync(
                matchId,
                job.UserId,
                request: null,
                snapshot,
                manageLifecycle: false,
                cancellationToken);
        }

        private async Task<CvJdMatchingExecutionResult> ProcessMatchingJobCoreAsync(
            Guid jobId,
            Guid userId,
            MatchingRequestDto? request,
            MatchingInputSnapshotV1? snapshot = null,
            bool manageLifecycle = true,
            CancellationToken cancellationToken = default)
        {
            var matchRecord = await _context.CvJobMatchScores
                .SingleOrDefaultAsync(x => x.Id == jobId && x.MatchType == "AI", cancellationToken);
            if (matchRecord == null)
                throw new KeyNotFoundException("MATCHING_JOB_NOT_FOUND");

            try
            {
                if (snapshot is not null &&
                    _matchingCvPreparationService is not null &&
                    _matchingJdPreparationService is not null &&
                    _rawJdFallbackMatchingService is not null)
                {
                    var preparedResult = await ProcessPreparedSnapshotAsync(userId, snapshot, cancellationToken);
                    if (!manageLifecycle)
                    {
                        return preparedResult;
                    }

                    matchRecord.Status = "Completed";
                    matchRecord.MatchScore = preparedResult.Score;
                    matchRecord.MatchDetails = preparedResult.MatchDetails;
                    matchRecord.CvAnalysisQuality = preparedResult.CvAnalysisQuality;
                    matchRecord.CvAnalysisCoverageJson = CvAnalysisMetadataReader.SerializeCoverage(preparedResult.CvAnalysisCoverage);
                    matchRecord.CvAnalysisDiagnosticsJson = CvAnalysisMetadataReader.SerializeDiagnostics(preparedResult.CvAnalysisDiagnostics);
                    matchRecord.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    return preparedResult;
                }

                if (manageLifecycle)
                {
                    matchRecord.Status = "Processing";
                    matchRecord.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                Cvs? savedCv = null;
                bool cvNeedsAiParse = false;      // Cần gọi AI parse ParsedData (saved CV hoặc Temp raw text)
                bool isCvTextJson = false;
                CvAnalysisValidationResult? cvAnalysis = null;

                var snapshotCvAnalysisJson = snapshot?.Cv.AnalysisJson;
                string cvText = !string.IsNullOrWhiteSpace(snapshotCvAnalysisJson)
                    ? snapshotCvAnalysisJson
                    : snapshot?.Cv.OriginalText ?? request?.CvText ?? string.Empty;
                
                if (snapshot != null)
                {
                    if (!string.IsNullOrWhiteSpace(snapshot.Cv.AnalysisJson))
                    {
                        cvAnalysis = _cvAnalysisResponseValidator.ValidateAndCanonicalize(snapshot.Cv.AnalysisJson);
                        if (cvAnalysis.IsUsable)
                        {
                            cvText = cvAnalysis.CanonicalJson;
                            isCvTextJson = true;
                        }
                        else
                        {
                            cvText = snapshot.Cv.OriginalText ?? string.Empty;
                            cvNeedsAiParse = true;
                        }
                    }
                    else
                    {
                        cvNeedsAiParse = true;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(cvText))
                {
                    // A client can paste JSON-looking CV text. It is still raw CV
                    // content and must pass through the trusted parser.
                    cvNeedsAiParse = true;
                }
                else if (matchRecord.CvId.HasValue)
                {
                    savedCv = await _matchingSourceRepository.GetOwnedCvForUpdateAsync(matchRecord.CvId.Value, userId);
                    if (savedCv is null)
                    {
                        throw new KeyNotFoundException("CV not found");
                    }

                    cvAnalysis = savedCv.ParseStatus == "SUCCESS"
                        ? ValidateAndCanonicalizeStoredCv(savedCv)
                        : CvAnalysisValidationResult.Invalid(
                            "CV_ANALYSIS_EMPTY_OUTPUT",
                            "STORED_ANALYSIS_NOT_READY",
                            "$");
                    if (cvAnalysis.IsUsable)
                    {
                        cvText = cvAnalysis.CanonicalJson;
                        isCvTextJson = true;
                    }
                    else
                    {
                        cvNeedsAiParse = true;
                    }
                }
                else
                {
                    throw new InvalidOperationException("INVALID_PREPARED_CV_SOURCE");
                }

                _logger.LogInformation("CV needs AI parse: {Parse}", cvNeedsAiParse);
                string jdRequirementsJson = "";
                Domain.Entities.JobPostings? savedJob = null;
                bool jdNeedsAiParse = false; // Cần gọi LLM Stage 1 cho JD

                if (snapshot != null)
                {
                    jdRequirementsJson = snapshot.Jd.AnalysisJson ?? string.Empty;
                    jdNeedsAiParse = string.IsNullOrWhiteSpace(jdRequirementsJson);
                }
                else if (request!.JobId.HasValue)
                {
                    savedJob = await _matchingSourceRepository.GetAccessiblePublishedJobAsync(request.JobId.Value, DateTime.UtcNow);
                    if (savedJob is null)
                    {
                        throw new KeyNotFoundException("Job not found");
                    }

                    if (savedJob.ParseStatus == "SUCCESS" && !string.IsNullOrWhiteSpace(savedJob.ParsedData))
                    {
                        _logger.LogInformation("[INFO] Stage 1 skipped. Using ParsedData from Job {JobId}", request.JobId);
                        jdRequirementsJson = savedJob.ParsedData;
                    }
                    else jdNeedsAiParse = true;
                }
                else if (!string.IsNullOrWhiteSpace(request!.RawJdText))
                {
                    jdNeedsAiParse = true;
                }

                // Scoped parser dependencies share one DbContext, so stage one must be ordered.
                if (cvNeedsAiParse && jdNeedsAiParse)
                    _logger.LogInformation("[INFO] Running CV extraction before JD Stage 1 parsing to keep scoped services isolated.");

                string parsedData = string.Empty;

                if (cvNeedsAiParse)
                {
                    if (savedCv != null)
                        parsedData = cancellationToken.CanBeCanceled
                            ? await _cvTextExtractorService.ExtractParsedDataFromUrlAsync(savedCv.FileUrl, savedCv.RawText ?? string.Empty, cancellationToken)
                            : await _cvTextExtractorService.ExtractParsedDataFromUrlAsync(savedCv.FileUrl, savedCv.RawText ?? string.Empty);
                    else
                        parsedData = cancellationToken.CanBeCanceled
                            ? await _cvTextExtractorService.ExtractParsedDataFromRawTextAsync(cvText, "pasted_text", request?.CvFileName, cancellationToken)
                            : await _cvTextExtractorService.ExtractParsedDataFromRawTextAsync(cvText, "pasted_text", request?.CvFileName);
                }

                // --- Xử lý kết quả CV ---
                if (cvNeedsAiParse)
                {
                    if (!string.IsNullOrWhiteSpace(parsedData))
                    {
                        cvAnalysis = _cvAnalysisResponseValidator.ValidateAndCanonicalize(parsedData);
                        if (!cvAnalysis.IsUsable)
                        {
                            throw new CvAnalysisValidationException(cvAnalysis);
                        }
                        if (savedCv != null)
                        {
                            savedCv.ParsedData = cvAnalysis.CanonicalJson;
                            ApplyCvAnalysisMetadata(savedCv, cvAnalysis);
                            _context.Cvs.Update(savedCv);
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        cvText = cvAnalysis.CanonicalJson;
                        isCvTextJson = true;
                    }
                    else
                    {
                        throw new InvalidOperationException("CV_ANALYSIS_EMPTY_OUTPUT");
                    }
                }
                else if (string.IsNullOrWhiteSpace(cvText) && savedCv != null)
                {
                    // CV đã parsed sẵn, không cần gọi AI
                    cvText = !string.IsNullOrWhiteSpace(savedCv.ParsedData) ? savedCv.ParsedData : savedCv.RawText ?? string.Empty;
                    isCvTextJson = !string.IsNullOrWhiteSpace(savedCv.ParsedData);
                }

                // Cập nhật tên CV nếu là CV từ hệ thống
                if (savedCv != null && string.IsNullOrEmpty(matchRecord.CvFileName))
                    matchRecord.CvFileName = savedCv.FileName ?? "Saved CV";

                if (string.IsNullOrWhiteSpace(cvText))
                {
                    throw new InvalidOperationException("CV_ANALYSIS_EMPTY_OUTPUT");
                }

                // --- Xử lý kết quả JD Stage 1 ---
                if (jdNeedsAiParse)
                {
                    jdRequirementsJson = await ExtractJdWithV2Async(
                        savedJob,
                        snapshot?.Jd.OriginalText ?? request?.RawJdText,
                        snapshot?.Jd.Title ?? request?.JdTitle,
                        cancellationToken);
                }

                if (string.IsNullOrWhiteSpace(jdRequirementsJson))
                {
                    throw new Exception("Cannot extract or retrieve JD requirements.");
                }

                var matchingPromptSnapshot = await _promptManagementService.GetActivePromptSnapshotAsync(
                    ITHunterview.Service.Constant.Prompts.BypassMatchingPrompt.Key);
                var stageTwoProjection = _jdRequirementProjector.Project(jdRequirementsJson);
                if (stageTwoProjection.Groups.Count == 0)
                {
                    throw new InvalidOperationException("INVALID_EFFECTIVE_JD_ANALYSIS");
                }

                if (!isCvTextJson)
                {
                    throw new InvalidOperationException("CV_ANALYSIS_INVALID_FOR_MATCHING");
                }
                var stageTwoCv = _cvStageTwoContextBuilder.Build(cvText);
                cvAnalysis ??= _cvAnalysisResponseValidator.ValidateAndCanonicalize(cvText);
                if (!cvAnalysis.IsUsable)
                {
                    throw new CvAnalysisValidationException(cvAnalysis);
                }
                var stageTwoCvContext = stageTwoCv.Json;

                try 
                {
                    var finalResult = await _jdStageTwoMatchingService.ExecuteAsync(
                        matchingPromptSnapshot,
                        stageTwoCvContext,
                        stageTwoProjection,
                        cancellationToken);
                    
                    if (!manageLifecycle)
                    {
                        return new CvJdMatchingExecutionResult(
                            finalResult.FinalScore,
                            finalResult.JsonString,
                            matchRecord.SfiaExtractResult,
                            stageTwoCv.Quality,
                            stageTwoCv.Coverage,
                            stageTwoCv.Diagnostics,
                            CompletionDisposition: finalResult.CompletionDisposition);
                    }

                    matchRecord.Status = "Completed";
                    matchRecord.MatchScore = finalResult.FinalScore;
                    matchRecord.MatchDetails = finalResult.JsonString;
                    matchRecord.CvAnalysisQuality = stageTwoCv.Quality;
                    matchRecord.CvAnalysisCoverageJson = CvAnalysisMetadataReader.SerializeCoverage(stageTwoCv.Coverage);
                    matchRecord.CvAnalysisDiagnosticsJson = CvAnalysisMetadataReader.SerializeDiagnostics(stageTwoCv.Diagnostics);
                    matchRecord.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (!manageLifecycle)
                        throw;

                    var classification = MatchingFailureClassifier.Classify(ex);
                    _logger.LogError(
                        "Stage 2 processing failed for match {MatchId}; code={ErrorCode}.",
                        matchRecord.Id,
                        classification.ErrorCode);
                    matchRecord.Status = "Failed";
                    matchRecord.ErrorMessage = classification.ErrorCode;
                    matchRecord.MatchDetails = string.Empty;
                    matchRecord.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    await _featureUsageUseCase.RefundFeatureUsageByReferenceAsync(
                        userId,
                        matchRecord.Id,
                        "Hoàn Coin do CV-JD matching không thể xử lý kết quả AI.");
                }

                return new CvJdMatchingExecutionResult(
                    matchRecord.MatchScore ?? 0m,
                    matchRecord.MatchDetails,
                    matchRecord.SfiaExtractResult,
                    matchRecord.CvAnalysisQuality,
                    CvAnalysisMetadataReader.ReadCoverageJson(matchRecord.CvAnalysisCoverageJson),
                    CvAnalysisMetadataReader.ReadDiagnosticsJson(matchRecord.CvAnalysisDiagnosticsJson));
            }
            catch (Exception ex)
            {
                if (!manageLifecycle)
                    throw;

                var failureCode = MatchingFailureClassifier.Classify(ex).ErrorCode;
                matchRecord.Status = "Failed";
                matchRecord.ErrorMessage = failureCode;
                matchRecord.MatchDetails = string.Empty;
                matchRecord.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _featureUsageUseCase.RefundFeatureUsageByReferenceAsync(
                    userId,
                    matchRecord.Id,
                    "Hoàn Coin do CV-JD matching thất bại.");
            }

            return new CvJdMatchingExecutionResult(
                matchRecord.MatchScore ?? 0m,
                matchRecord.MatchDetails,
                matchRecord.SfiaExtractResult,
                matchRecord.CvAnalysisQuality,
                CvAnalysisMetadataReader.ReadCoverageJson(matchRecord.CvAnalysisCoverageJson),
                CvAnalysisMetadataReader.ReadDiagnosticsJson(matchRecord.CvAnalysisDiagnosticsJson));
        }

        public async Task ProcessMatchingJobAsync(Guid jobId, Guid userId, PreparedMatchingRequest request)
        {
            try
            {
                await _matchingInputPreflightUseCase.RecheckAccessAsync(userId, request);
            }
            catch (Exception ex)
            {
                var matchRecord = await _context.CvJobMatchScores.FindAsync(jobId);
                if (matchRecord is null)
                {
                    return;
                }

                matchRecord.Status = "Failed";
                matchRecord.ErrorMessage = "MATCHING_SOURCE_ACCESS_REVOKED";
                matchRecord.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _featureUsageUseCase.RefundFeatureUsageByReferenceAsync(
                    userId,
                    matchRecord.Id,
                    "Hoàn Coin do CV hoặc JD không còn khả dụng trước khi matching.");
                _logger.LogWarning(
                    "Matching source access changed before processing match {MatchId}. ErrorType={ErrorType}",
                    jobId,
                    ex.GetType().Name);
                return;
            }

            var internalRequest = new MatchingRequestDto
            {
                CvId = (request.Cv as PreparedSavedCvSource)?.CvId,
                CvText = (request.Cv as PreparedRawCvSource)?.RawText,
                CvFileName = (request.Cv as PreparedSavedCvSource)?.FileName
                    ?? (request.Cv as PreparedRawCvSource)?.FileName,
                JobId = (request.Jd as PreparedSavedJdSource)?.JobId,
                RawJdText = (request.Jd as PreparedRawJdSource)?.RawText,
                JdTitle = (request.Jd as PreparedSavedJdSource)?.Title
                    ?? (request.Jd as PreparedRawJdSource)?.Title,
                Mode = request.Mode
            };

            await ProcessMatchingJobCoreAsync(jobId, userId, internalRequest);
        }

        private async Task<CvJdMatchingExecutionResult> ProcessPreparedSnapshotAsync(
            Guid userId,
            MatchingInputSnapshotV1 snapshot,
            CancellationToken cancellationToken)
        {
            // Keep extraction ordered: all preparation services are scoped and may
            // share the current DbContext. Prompt-pair metadata is deliberately
            // absent from this decision; source/quality state alone selects the path.
            var preparedCv = await _matchingCvPreparationService!.PrepareAsync(userId, snapshot, cancellationToken);
            var preparedJd = await _matchingJdPreparationService!.PrepareAsync(snapshot, cancellationToken);
            var cvContext = _cvStageTwoContextBuilder.Build(preparedCv.CanonicalJson);

            JdFitScoreCalculation score;
            JdAnalysisQuality jdQuality;
            JdAnalysisCoverage? jdCoverage;
            IReadOnlyList<JdAnalysisDiagnostic> jdDiagnostics;
            JdAnalysisPersistenceIntent? jdIntent;
            if (preparedJd is PreparedStructuredJdMatchingInput structured)
            {
                try
                {
                    var matchingPrompt = await _promptManagementService.GetActivePromptSnapshotAsync(
                        ITHunterview.Service.Constant.Prompts.BypassMatchingPrompt.Key,
                        cancellationToken);
                    score = await _jdStageTwoMatchingService.ExecuteAsync(
                        matchingPrompt,
                        cvContext.Json,
                        structured.Projection,
                        cancellationToken);
                }
                catch (Exception exception) when (
                    TryClassifyMatchingPromptAvailability(exception, out var promptFailureCode))
                {
                    _logger.LogWarning(
                        "Structured matching prompt unavailable. Code={Code}; projected requirements remain visible without a fabricated score.",
                        promptFailureCode);
                    score = _jdStageTwoMatchingService.CreateConfigurationUnavailableResult(
                        structured.Projection);
                }
                jdQuality = structured.Quality;
                jdCoverage = structured.Coverage;
                jdDiagnostics = structured.Diagnostics;
                jdIntent = structured.PersistenceIntent;
            }
            else if (preparedJd is PreparedRawJdMatchingInput raw)
            {
                score = await _rawJdFallbackMatchingService!.ExecuteAsync(
                    cvContext.Json,
                    raw.RawText,
                    raw.Title,
                    raw.Diagnostics,
                    cancellationToken);
                jdQuality = raw.Quality;
                jdCoverage = raw.Coverage;
                jdDiagnostics = raw.Diagnostics;
                jdIntent = raw.PersistenceIntent;
            }
            else
            {
                throw new InvalidOperationException("INVALID_PREPARED_JD_SOURCE");
            }

            return new CvJdMatchingExecutionResult(
                score.FinalScore,
                score.JsonString,
                null,
                cvContext.Quality,
                cvContext.Coverage,
                cvContext.Diagnostics,
                jdQuality,
                jdCoverage,
                jdDiagnostics,
                preparedCv.PersistenceIntent,
                jdIntent,
                score.CompletionDisposition);
        }

        private static bool TryClassifyMatchingPromptAvailability(
            Exception exception,
            out string code)
        {
            var message = exception.Message ?? string.Empty;
            if (exception is InvalidOperationException &&
                message.StartsWith("PROMPT_NOT_CONFIGURED:", StringComparison.Ordinal))
            {
                code = "PROMPT_NOT_CONFIGURED";
                return true;
            }

            if (exception is InvalidOperationException &&
                message.StartsWith("PROMPT_CONFIGURATION_INVALID:", StringComparison.Ordinal))
            {
                code = "PROMPT_CONFIGURATION_INVALID";
                return true;
            }

            if (exception is InvalidOperationException &&
                message.StartsWith("MATCHING_PROMPT_PLACEHOLDER_MISSING", StringComparison.Ordinal))
            {
                code = "MATCHING_PROMPT_PLACEHOLDER_MISSING";
                return true;
            }

            if (exception is InvalidOperationException &&
                message.StartsWith("MATCHING_PROMPT_PLACEHOLDER_INVALID", StringComparison.Ordinal))
            {
                code = "MATCHING_PROMPT_PLACEHOLDER_INVALID";
                return true;
            }

            if (exception is ArgumentException &&
                message.StartsWith("MATCHING_PROMPT_SCHEMA_MUTATION", StringComparison.Ordinal))
            {
                code = "MATCHING_PROMPT_SCHEMA_MUTATION";
                return true;
            }

            code = string.Empty;
            return false;
        }

        private async Task<string> ExtractJdWithV2Async(
            JobPostings? savedJob,
            string? rawJdText,
            string? requestedTitle,
            CancellationToken cancellationToken)
        {
            if (_jobAnalysisExtractionService == null)
            {
                throw new InvalidOperationException("JOB_ANALYSIS_EXTRACTION_SERVICE_NOT_CONFIGURED");
            }

            var snapshot = savedJob == null
                ? _jobAnalysisInputBuilder.BuildFromPastedText(requestedTitle, rawJdText)
                : _jobAnalysisInputBuilder.Build(savedJob);

            var extraction = cancellationToken.CanBeCanceled
                ? await _jobAnalysisExtractionService.ExtractWithActivePromptsAsync(snapshot, cancellationToken)
                : await _jobAnalysisExtractionService.ExtractWithActivePromptsAsync(snapshot);
            if (!extraction.Validation.IsUsable || extraction.Validation.Data == null)
            {
                throw new InvalidOperationException("INVALID_JD_ANALYSIS");
            }

            return _jobAnalysisExtractionService.SerializeEffectiveAnalysis(extraction.Validation.Data);
        }


        public async Task<MatchingResultDto?> GetMatchingResultAsync(Guid jobId, Guid userId)
        {
            var matchRecord = await _context.CvJobMatchScores
                .SingleOrDefaultAsync(m => m.Id == jobId && m.UserId == userId && m.HistoryHiddenAt == null);
                
            if (matchRecord == null) return null;
            var jdAnalysis = BuildJdAnalysisResult(matchRecord);
            var report = _jdMatchReportReader.Read(matchRecord.MatchDetails, matchRecord.MatchScore, matchRecord.MatchType);

            return new MatchingResultDto
            {
                Id = matchRecord.Id,
                CvId = matchRecord.CvId,
                CvFileName = matchRecord.CvFileName,
                JobId = matchRecord.JobId,
                JdTitle = matchRecord.JdTitle,
                Status = matchRecord.Status,
                ErrorCode = matchRecord.ErrorCode,
                ErrorMessage = matchRecord.ErrorMessage,
                CanRetry = string.Equals(matchRecord.Status, "Failed", StringComparison.Ordinal)
                    && MatchingRetryPolicy.IsManualRetryAllowed(matchRecord.ErrorCode),
                MatchDetails = report.ReportKind == MatchReportKinds.LegacySummary
                    ? matchRecord.MatchDetails
                    : null,
                ScorePercent = report.ScorePercent,
                ScoreAvailable = report.ScoreAvailable,
                ReportKind = report.ReportKind,
                MatchMethod = report.MatchMethod,
                Report = report,
                JdFit = new JdFitResultDto { Score = report.ScorePercent },
                JdAnalysis = jdAnalysis,
                CvAnalysis = BuildCvAnalysisResult(matchRecord)
            };
        }

        private static JdAnalysisResultDto? BuildJdAnalysisResult(CvJobMatchScores score)
        {
            var embedded = JdMatchMetadataReader.Read(score.MatchDetails);
            if (embedded is not null)
            {
                return embedded;
            }

            if (!score.JdAnalysisQuality.HasValue) return null;
            var quality = score.JdAnalysisQuality.Value;
            return new JdAnalysisResultDto
            {
                Quality = quality.ToString(),
                ScoreBasis = quality == JdAnalysisQuality.INVALID
                    ? "raw_text_fallback"
                    : "available_jd_analysis",
                RequirementSetComplete = quality != JdAnalysisQuality.INVALID,
                Coverage = JdAnalysisMetadataReader.ReadCoverageJson(score.JdAnalysisCoverageJson),
                WarningCodes = JdAnalysisMetadataReader.ReadDiagnosticsJson(score.JdAnalysisDiagnosticsJson)
                    .Select(item => item.Code)
                    .Distinct(StringComparer.Ordinal)
                    .Take(100)
                    .ToList()
            };
        }

        private static CvAnalysisResultDto? BuildCvAnalysisResult(CvJobMatchScores score)
        {
            if (!score.CvAnalysisQuality.HasValue) return null;
            return new CvAnalysisResultDto
            {
                Quality = score.CvAnalysisQuality.Value.ToString(),
                ScoreBasis = ReadScoreBasis(score.MatchDetails)
                    ?? (score.CvAnalysisQuality.Value == Domain.Enums.CvAnalysisQuality.COMPLETE
                        ? "complete_cv_analysis"
                        : "available_cv_analysis"),
                Coverage = CvAnalysisMetadataReader.ReadCoverageJson(score.CvAnalysisCoverageJson),
                WarningCodes = CvAnalysisMetadataReader.ReadDiagnosticsJson(score.CvAnalysisDiagnosticsJson)
                    .Select(item => item.Code)
                    .Distinct(StringComparer.Ordinal)
                    .Take(100)
                    .ToList()
            };
        }

        private static string? ReadScoreBasis(string? matchDetails)
        {
            if (string.IsNullOrWhiteSpace(matchDetails)) return null;
            try
            {
                using var document = JsonDocument.Parse(matchDetails);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
                foreach (var name in new[] { "scoreBasis", "ScoreBasis" })
                {
                    if (document.RootElement.TryGetProperty(name, out var value)
                        && value.ValueKind == JsonValueKind.String)
                    {
                        var result = value.GetString()?.Trim();
                        return string.IsNullOrWhiteSpace(result) || result.Length > 64 ? null : result;
                    }
                }
            }
            catch (JsonException)
            {
                // Match details from older rows can be non-JSON. Quality metadata
                // remains independently usable.
            }
            return null;
        }


        public async Task<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>> GetMatchHistoryAsync(Guid userId, int page, int pageSize, Guid? cvId = null)
        {
            var query = from s in _context.CvJobMatchScores
                        join c in _context.Cvs on s.CvId equals c.Id into cvs
                        from c in cvs.DefaultIfEmpty()
                        join j in _context.JobPostings on s.JobId equals j.Id into jobs
                        from j in jobs.DefaultIfEmpty()
                        where s.UserId == userId && s.HistoryHiddenAt == null
                        select new { Score = s, Cv = c, Job = j };

            if (cvId.HasValue)
            {
                query = query.Where(x => x.Score.CvId == cvId.Value);
            }

            query = query.OrderByDescending(x => x.Score.UpdatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var mappedItems = items.Select(x =>
            {
                var jdAnalysis = BuildJdAnalysisResult(x.Score);
                var report = _jdMatchReportReader.Read(x.Score.MatchDetails, x.Score.MatchScore, x.Score.MatchType);
                return new ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto
                {
                JobId = x.Score.Id,
                CvId = x.Score.CvId,
                CandidateId = x.Cv?.UserId,
                CvFileName = x.Cv?.FileName ?? x.Score.CvFileName ?? "Unknown CV",
                FileUrl = x.Cv?.FileUrl,
                SourceJobId = x.Score.JobId,
                JdTitle = x.Job?.Title ?? x.Score.JdTitle ?? x.Score.RawJdText,
                MatchScore = x.Score.MatchScore,
                ScorePercent = report.ScorePercent,
                ScoreAvailable = report.ScoreAvailable,
                ReportKind = report.ReportKind,
                MatchMethod = report.MatchMethod,
                Status = x.Score.Status,
                ErrorMessage = x.Score.ErrorMessage,
                UpdatedAt = x.Score.UpdatedAt,
                MatchType = x.Score.MatchType,
                CvAnalysisQuality = x.Score.CvAnalysisQuality,
                CvAnalysisScoreBasis = ReadScoreBasis(x.Score.MatchDetails),
                CvAnalysisCoverage = CvAnalysisMetadataReader.ReadCoverageJson(x.Score.CvAnalysisCoverageJson),
                JdAnalysisQuality = jdAnalysis?.Quality,
                JdAnalysisScoreBasis = jdAnalysis?.ScoreBasis,
                JdAnalysisCoverage = jdAnalysis?.Coverage
                };
            }).ToList();

            return new ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>
            {
                Items = mappedItems,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>> GetJobMatchHistoryAsync(Guid jobId, Guid recruiterId, int page, int pageSize)
        {
            var query = from s in _context.CvJobMatchScores.AsNoTracking()
                        join c in _context.Cvs on s.CvId equals c.Id into cvs
                        from c in cvs.DefaultIfEmpty()
                        where s.JobId == jobId
                        select new { Score = s, Cv = c };

            var authorizedRows = await query.ToListAsync();
            var total = authorizedRows.Count;
            var items = authorizedRows
                .Select(row => new
                {
                    row.Score,
                    row.Cv,
                    Report = _jdMatchReportReader.Read(
                        row.Score.MatchDetails,
                        row.Score.MatchScore,
                        row.Score.MatchType)
                })
                .OrderByDescending(row => row.Report.ScorePercent)
                .ThenByDescending(row => row.Score.UpdatedAt)
                .ThenBy(row => row.Score.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Get unlocked CV IDs for this recruiter
            var cvIds = items.Where(x => x.Score.CvId.HasValue).Select(x => x.Score.CvId!.Value).ToList();
            var unlockedCvIds = await _context.RecruiterUnlockedCvs
                .Where(u => u.RecruiterId == recruiterId && cvIds.Contains(u.CvId))
                .Select(u => u.CvId)
                .ToListAsync();
            var unlockedSet = new HashSet<Guid>(unlockedCvIds);

            // Check active subscription quota for recruiter
            var activeSub = await _context.UserSubscriptions
                .Where(us => us.UserId == recruiterId && us.Status == Domain.Enums.UserSubscriptionStatus.ACTIVE && us.EndDate >= DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            int unlockQuota = 0;
            int currentUsedQuota = 0;

            if (activeSub != null)
            {
                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == activeSub.SubId);
                if (sub != null && !string.IsNullOrEmpty(sub.FeaturesConfig))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(sub.FeaturesConfig);
                        if (doc.RootElement.TryGetProperty("unlockCvLimit", out var limitProp))
                        {
                            unlockQuota = limitProp.GetInt32();
                        }
                    }
                    catch { }

                    if (unlockQuota > 0)
                    {
                        currentUsedQuota = await _context.RecruiterUnlockedCvs
                            .CountAsync(u => u.RecruiterId == recruiterId && u.UnlockedVia == "SUBSCRIPTION" && u.UnlockedAt >= activeSub.StartDate && u.UnlockedAt <= activeSub.EndDate);
                    }
                }
            }

            int index = (page - 1) * pageSize + 1;
            var mappedItems = items.Select(x =>
            {
                var cvId = x.Score.CvId;
                bool isUnlocked = false;

                if (cvId.HasValue)
                {
                    if (unlockedSet.Contains(cvId.Value))
                    {
                        isUnlocked = true;
                    }
                    else if (unlockQuota > 0 && currentUsedQuota < unlockQuota)
                    {
                        // Active subscription quota available
                        isUnlocked = true;
                    }
                }

                var itemIndex = index++;
                var jdAnalysis = BuildJdAnalysisResult(x.Score);
                var report = x.Report;

                return new ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto
                {
                    JobId = x.Score.JobId ?? Guid.Empty,
                    CvId = cvId,
                    // STRICT ANTI-F12 DATA MASKING: Strip CandidateId, FileUrl, and real Name if locked
                    CandidateId = isUnlocked ? x.Cv?.UserId : null,
                    CvFileName = isUnlocked ? (x.Cv?.FileName ?? "Unknown CV") : $"Ứng viên #{itemIndex}",
                    FileUrl = isUnlocked ? x.Cv?.FileUrl : null,
                    SourceJobId = x.Score.Id,
                    JdTitle = x.Score.JdTitle,
                    MatchScore = x.Score.MatchScore,
                    ScorePercent = report.ScorePercent,
                    ScoreAvailable = report.ScoreAvailable,
                    ReportKind = report.ReportKind,
                    MatchMethod = report.MatchMethod,
                    Status = x.Score.Status,
                    ErrorMessage = x.Score.ErrorMessage,
                    UpdatedAt = x.Score.UpdatedAt,
                    MatchType = x.Score.MatchType,
                    CvAnalysisQuality = x.Score.CvAnalysisQuality,
                    CvAnalysisScoreBasis = ReadScoreBasis(x.Score.MatchDetails),
                    CvAnalysisCoverage = CvAnalysisMetadataReader.ReadCoverageJson(x.Score.CvAnalysisCoverageJson),
                    JdAnalysisQuality = jdAnalysis?.Quality,
                    JdAnalysisScoreBasis = jdAnalysis?.ScoreBasis,
                    JdAnalysisCoverage = jdAnalysis?.Coverage,
                    IsUnlocked = isUnlocked,
                    UnlockCost = 50
                };
            }).ToList();

            return new ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>
            {
                Items = mappedItems,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto> UnlockCandidateCvAsync(Guid recruiterId, ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateRequestDto dto)
        {
            var cv = await _context.Cvs.FindAsync(dto.CvId);
            if (cv == null)
            {
                return new ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ CV của ứng viên."
                };
            }

            // Check if already unlocked in DB
            var existingUnlock = await _context.RecruiterUnlockedCvs
                .FirstOrDefaultAsync(u => u.RecruiterId == recruiterId && u.CvId == dto.CvId);

            if (existingUnlock != null)
            {
                var currentWallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == recruiterId);
                return new ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto
                {
                    Success = true,
                    Message = "Hồ sơ ứng viên đã được mở khóa từ trước.",
                    UnlockedVia = existingUnlock.UnlockedVia,
                    CoinsDeducted = 0,
                    RemainingCoins = currentWallet?.Balance ?? 0,
                    CvId = cv.Id,
                    CandidateId = cv.UserId,
                    CvFileName = cv.FileName ?? "Candidate CV",
                    FileUrl = cv.FileUrl
                };
            }

            // Check Active Subscription
            var activeSub = await _context.UserSubscriptions
                .Where(us => us.UserId == recruiterId && us.Status == Domain.Enums.UserSubscriptionStatus.ACTIVE && us.EndDate >= DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            int unlockQuota = 0;
            if (activeSub != null)
            {
                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == activeSub.SubId);
                if (sub != null && !string.IsNullOrEmpty(sub.FeaturesConfig))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(sub.FeaturesConfig);
                        if (doc.RootElement.TryGetProperty("unlockCvLimit", out var limitProp))
                        {
                            unlockQuota = limitProp.GetInt32();
                        }
                    }
                    catch { }

                    if (unlockQuota > 0)
                    {
                        var usedQuota = await _context.RecruiterUnlockedCvs
                            .CountAsync(u => u.RecruiterId == recruiterId && u.UnlockedVia == "SUBSCRIPTION" && u.UnlockedAt >= activeSub.StartDate && u.UnlockedAt <= activeSub.EndDate);

                        if (usedQuota < unlockQuota)
                        {
                            // Free unlock via subscription
                            var newUnlockSub = new Domain.Entities.RecruiterUnlockedCvs
                            {
                                Id = Guid.NewGuid(),
                                RecruiterId = recruiterId,
                                CvId = dto.CvId,
                                JobId = dto.JobId,
                                CoinsSpent = 0,
                                UnlockedVia = "SUBSCRIPTION",
                                UnlockedAt = DateTime.UtcNow
                            };
                            _context.RecruiterUnlockedCvs.Add(newUnlockSub);
                            await _context.SaveChangesAsync();

                            var currentWallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == recruiterId);

                            return new ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto
                            {
                                Success = true,
                                Message = "Mở khóa hồ sơ ứng viên thành công bằng quyền Subscription!",
                                UnlockedVia = "SUBSCRIPTION",
                                CoinsDeducted = 0,
                                RemainingCoins = currentWallet?.Balance ?? 0,
                                CvId = cv.Id,
                                CandidateId = cv.UserId,
                                CvFileName = cv.FileName ?? "Candidate CV",
                                FileUrl = cv.FileUrl
                            };
                        }
                    }
                }
            }

            // Pay via Coins
            const int unlockCost = 50;
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == recruiterId);

            if (wallet == null || wallet.Balance < unlockCost)
            {
                int currentBalance = wallet?.Balance ?? 0;
                return new ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto
                {
                    Success = false,
                    Message = $"Số dư Coin không đủ (Hiện có: {currentBalance} Coin, Cần: {unlockCost} Coin). Vui lòng nạp thêm Coin hoặc nâng cấp gói Subscription.",
                    RemainingCoins = currentBalance
                };
            }

            // Deduct Coins
            wallet.Balance -= unlockCost;
            wallet.UpdatedAt = DateTime.UtcNow;
            _context.UserWallets.Update(wallet);

            var creditTx = new Domain.Entities.CreditTransactions
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = -unlockCost,
                TransactionType = Domain.Enums.CreditTransactionType.DEDUCT,
                ReferenceId = dto.CvId,
                Description = $"Mở khóa hồ sơ CV ứng viên ({cv.FileName ?? "Candidate CV"})",
                CreatedAt = DateTime.UtcNow
            };
            _context.CreditTransactions.Add(creditTx);

            var newUnlock = new Domain.Entities.RecruiterUnlockedCvs
            {
                Id = Guid.NewGuid(),
                RecruiterId = recruiterId,
                CvId = dto.CvId,
                JobId = dto.JobId,
                CoinsSpent = unlockCost,
                UnlockedVia = "COINS",
                UnlockedAt = DateTime.UtcNow
            };
            _context.RecruiterUnlockedCvs.Add(newUnlock);

            await _context.SaveChangesAsync();

            return new ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto
            {
                Success = true,
                Message = $"Mở khóa hồ sơ thành công! Đã dùng {unlockCost} Coin.",
                UnlockedVia = "COINS",
                CoinsDeducted = unlockCost,
                RemainingCoins = wallet.Balance,
                CvId = cv.Id,
                CandidateId = cv.UserId,
                CvFileName = cv.FileName ?? "Candidate CV",
                FileUrl = cv.FileUrl
            };
        }

    }
}
