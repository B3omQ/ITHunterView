using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.UseCase
{

    public class CvJobMatchingUseCase : ICvJobMatchingUseCase, ICvJdOneToOneMatchingEngine
    {
        private readonly ITHunterviewContext _context;
        private readonly IAiEmbeddingService _aiService;
        private readonly ICvTextExtractorService _cvTextExtractorService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CvJobMatchingUseCase> _logger;
        private readonly IPromptManagementService _promptManagementService;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IAiService _textAiService;
        private readonly IJobAnalysisExtractionService? _jobAnalysisExtractionService;
        private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;
        private readonly IMatchingInputPreflightUseCase _matchingInputPreflightUseCase;
        private readonly IMatchingSourceRepository _matchingSourceRepository;
        private readonly ICvAnalysisResponseValidator _cvAnalysisResponseValidator;
        private readonly IJdRequirementProjector _jdRequirementProjector;
        private readonly JdStageTwoContextBuilder _jdStageTwoContextBuilder;
        private readonly JdStageTwoResponseValidator _jdStageTwoResponseValidator;
        private readonly JdFitScoreCalculator _jdFitScoreCalculator;
        private readonly CvStageTwoContextBuilder _cvStageTwoContextBuilder;

        public CvJobMatchingUseCase(
            ITHunterviewContext context, 
            IAiEmbeddingService aiService,
            ICvTextExtractorService cvTextExtractorService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<CvJobMatchingUseCase> logger,
            IPromptManagementService promptManagementService,
            ISystemConfigRepository systemConfigRepository,
            IAiService textAiService,
            ICandidateFeatureUsageUseCase featureUsageUseCase,
            IMatchingInputPreflightUseCase matchingInputPreflightUseCase,
            IMatchingSourceRepository matchingSourceRepository,
            ICvAnalysisResponseValidator cvAnalysisResponseValidator,
            IJobAnalysisExtractionService? jobAnalysisExtractionService = null,
            IJdRequirementProjector? jdRequirementProjector = null,
            JdStageTwoContextBuilder? jdStageTwoContextBuilder = null,
            JdStageTwoResponseValidator? jdStageTwoResponseValidator = null,
            JdFitScoreCalculator? jdFitScoreCalculator = null,
            CvStageTwoContextBuilder? cvStageTwoContextBuilder = null)
        {
            _context = context;
            _aiService = aiService;
            _cvTextExtractorService = cvTextExtractorService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _promptManagementService = promptManagementService;
            _systemConfigRepository = systemConfigRepository;
            _textAiService = textAiService;
            _featureUsageUseCase = featureUsageUseCase;
            _matchingInputPreflightUseCase = matchingInputPreflightUseCase;
            _matchingSourceRepository = matchingSourceRepository;
            _cvAnalysisResponseValidator = cvAnalysisResponseValidator;
            _jobAnalysisExtractionService = jobAnalysisExtractionService;
            _jdRequirementProjector = jdRequirementProjector ?? new JdRequirementProjector();
            _jdStageTwoContextBuilder = jdStageTwoContextBuilder ?? new JdStageTwoContextBuilder();
            _jdStageTwoResponseValidator = jdStageTwoResponseValidator ?? new JdStageTwoResponseValidator();
            _jdFitScoreCalculator = jdFitScoreCalculator ?? new JdFitScoreCalculator();
            _cvStageTwoContextBuilder = cvStageTwoContextBuilder ?? new CvStageTwoContextBuilder();
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
            if (string.IsNullOrWhiteSpace(cv.ParsedData) || cv.ParseStatus != "SUCCESS")
            {
                _logger.LogInformation("[INFO] On-demand parsing CV {CvId} in AI Matching.", cv.Id);
                var parsedData = await _cvTextExtractorService.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText);
                
                if (string.IsNullOrWhiteSpace(parsedData))
                {
                    cv.ParseStatus = "FAILED";
                    _context.Cvs.Update(cv);
                    await _context.SaveChangesAsync();
                    throw new Exception($"Cannot parse CV data on-demand for CV {cv.Id}.");
                }
                
                cv.ParsedData = parsedData;
                cv.ParseStatus = "SUCCESS";
                _context.Cvs.Update(cv);
                await _context.SaveChangesAsync();
            }
        }

        private bool TryCanonicalizeStoredV2Cv(Cvs cv)
        {
            if (!IsCvAnalysisV2(cv.ParsedData) || string.IsNullOrWhiteSpace(cv.RawText))
            {
                return false;
            }

            var sourceType = cv.FileName?.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) == true
                ? "docx_text"
                : "pdf_text";
            var result = _cvAnalysisResponseValidator.ValidateAndCanonicalize(
                cv.ParsedData,
                new CvAnalysisInputSnapshot(cv.RawText, sourceType, cv.FileName, DateOnly.FromDateTime(DateTime.UtcNow)));
            if (!result.IsValid)
            {
                _logger.LogWarning("Stored CV analysis requires reparsing. CvId={CvId}; FailureCode={FailureCode}", cv.Id, result.FailureCode);
                return false;
            }

            cv.ParsedData = result.CanonicalJson;
            return true;
        }

        private static bool IsCvAnalysisV2(string? parsedData)
        {
            if (string.IsNullOrWhiteSpace(parsedData)) return false;
            try
            {
                using var document = JsonDocument.Parse(parsedData);
                return document.RootElement.ValueKind == JsonValueKind.Object &&
                       document.RootElement.TryGetProperty("schema_version", out var schemaVersion) &&
                       string.Equals(schemaVersion.GetString(), "cv-analysis/v2", StringComparison.Ordinal);
            }
            catch (JsonException)
            {
                return false;
            }
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
                .FirstOrDefaultAsync(x => x.Id == matchId && x.MatchType == "AI", cancellationToken);
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
                .FirstOrDefaultAsync(x => x.Id == jobId && x.MatchType == "AI", cancellationToken);
            if (matchRecord == null)
                throw new KeyNotFoundException("MATCHING_JOB_NOT_FOUND");

            try
            {
                if (manageLifecycle)
                {
                    matchRecord.Status = "Processing";
                    matchRecord.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                Cvs? savedCv = null;
                bool cvNeedsAiParse = false;      // Cần gọi AI parse ParsedData (saved CV hoặc Temp raw text)
                bool isCvTextJson = false;

                string cvText = snapshot?.Cv.AnalysisJson ?? snapshot?.Cv.OriginalText ?? request?.CvText ?? string.Empty;
                
                if (snapshot != null)
                {
                    cvNeedsAiParse = string.IsNullOrWhiteSpace(snapshot.Cv.AnalysisJson);
                    isCvTextJson = !cvNeedsAiParse;
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

                    if (string.IsNullOrWhiteSpace(savedCv.ParsedData) || savedCv.ParseStatus != "SUCCESS" || !TryCanonicalizeStoredV2Cv(savedCv))
                        cvNeedsAiParse = true;
                }
                else
                {
                    throw new InvalidOperationException("INVALID_PREPARED_CV_SOURCE");
                }

                _logger.LogInformation("CV needs AI parse: {Parse}", cvNeedsAiParse);
                string jdRequirementsJson = "";
                List<ScoringRequirement> requirementsList = new List<ScoringRequirement>();
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
                        parsedData = await _cvTextExtractorService.ExtractParsedDataFromUrlAsync(savedCv.FileUrl, savedCv.RawText);
                    else
                        parsedData = await _cvTextExtractorService.ExtractParsedDataFromRawTextAsync(cvText, "pasted_text", request?.CvFileName);
                }

                // --- Xử lý kết quả CV ---
                if (cvNeedsAiParse)
                {
                    if (!string.IsNullOrWhiteSpace(parsedData))
                    {
                        if (savedCv != null)
                        {
                            savedCv.ParsedData = parsedData;
                            savedCv.ParseStatus = "SUCCESS";
                            _context.Cvs.Update(savedCv);
                            await _context.SaveChangesAsync();
                        }
                        cvText = parsedData;
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
                        snapshot?.Jd.Title ?? request?.JdTitle);
                }

                if (string.IsNullOrWhiteSpace(jdRequirementsJson))
                {
                    throw new Exception("Cannot extract or retrieve JD requirements.");
                }

                var matchingPromptSnapshot = await _promptManagementService.GetActivePromptSnapshotAsync(
                    ITHunterview.Service.Constant.Prompts.BypassMatchingPrompt.Key);
                var useV3MatchingContract = ITHunterview.Service.Constant.Prompts.JdMatchingPromptContract.IsV3(
                    matchingPromptSnapshot.ModelConfig);
                JdRequirementProjection? stageTwoProjection = null;
                JdStageTwoContext? stageTwoJdContext = null;
                if (useV3MatchingContract)
                {
                    stageTwoProjection = _jdRequirementProjector.Project(jdRequirementsJson);
                    if (stageTwoProjection.Groups.Count == 0)
                    {
                        throw new InvalidOperationException("INVALID_EFFECTIVE_JD_ANALYSIS");
                    }
                    stageTwoJdContext = _jdStageTwoContextBuilder.Build(stageTwoProjection);
                }

                // Legacy prompt contract: retain the existing flat request payload until
                // the configured prompt explicitly opts into jd-matching/v3.
                try 
                {
                    var stage1Doc = JsonDocument.Parse(jdRequirementsJson);
                    if (stage1Doc.RootElement.TryGetProperty("matching_metrics", out var metrics))
                    {
                        if (metrics.TryGetProperty("requirement_groups", out var groups) && groups.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var group in groups.EnumerateArray())
                            {
                                if (!group.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) continue;
                                var names = items.EnumerateArray()
                                    .Where(item => item.TryGetProperty("skill_name", out var name) && name.ValueKind == JsonValueKind.String)
                                    .Select(item => item.GetProperty("skill_name").GetString())
                                    .Where(name => !string.IsNullOrWhiteSpace(name))
                                    .ToList();
                                if (names.Count == 0) continue;
                                var groupId = group.TryGetProperty("group_id", out var id) ? id.GetString() : null;
                                if (string.IsNullOrWhiteSpace(groupId)) throw new InvalidOperationException("INVALID_EFFECTIVE_JD_ANALYSIS");
                                requirementsList.Add(new ScoringRequirement
                                {
                                    ReqId = groupId,
                                    NormalizedText = string.Join(" | ", names),
                                    Category = "tech_skill",
                                    Importance = group.TryGetProperty("importance", out var groupImportance) ? groupImportance.GetString() ?? "must_have" : "must_have",
                                    DetailVerbatim = string.Join("; ", items.EnumerateArray().Select(item => item.TryGetProperty("detail_verbatim", out var detail) ? detail.GetString() : null).Where(detail => !string.IsNullOrWhiteSpace(detail))),
                                    CategoryWeight = 1m,
                                    Operator = group.TryGetProperty("operator", out var operation) ? operation.GetString() ?? "all_of" : "all_of",
                                    MinSatisfied = group.TryGetProperty("min_satisfied", out var minimum) && minimum.TryGetInt32(out var min) ? min : names.Count,
                                    Evidence = string.Join("; ", items.EnumerateArray().SelectMany(item => item.TryGetProperty("evidences", out var evidence) && evidence.ValueKind == JsonValueKind.Array ? evidence.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()) : Enumerable.Empty<string?>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct())
                                });
                            }
                        }
                        else if (metrics.TryGetProperty("requirements_list", out var reqs) && reqs.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var r in reqs.EnumerateArray())
                            {
                                string category = r.TryGetProperty("category", out var cat) ? cat.GetString() : "tech_skill";
                                if (category == "seniority_fit") continue; // remove explicitly if any

                                requirementsList.Add(new ScoringRequirement
                                {
                                    ReqId = Guid.NewGuid().ToString("N").Substring(0, 8),
                                    NormalizedText = r.TryGetProperty("skill_name", out var sn) ? sn.GetString() : "",
                                    Category = category,
                                    Importance = r.TryGetProperty("importance", out var imp) ? imp.GetString() : "nice_to_have",
                                    DetailVerbatim = r.TryGetProperty("detail_verbatim", out var dv) ? dv.GetString() : "",
                                    CategoryWeight = GetCategoryWeight(category)
                                });
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to parse Stage 1 JD Requirements: " + ex.Message);
                }

                if (!useV3MatchingContract && !requirementsList.Any())
                {
                    throw new Exception("No requirements extracted from JD.");
                }

                // Serialize for Stage 2
                var parsedJdJson = useV3MatchingContract
                    ? stageTwoJdContext!.Json
                    : JsonSerializer.Serialize(requirementsList.Select(r => new {
                        r.ReqId, r.NormalizedText, r.Category, r.Importance, r.DetailVerbatim, r.Operator, r.MinSatisfied, r.Evidence
                    }), new JsonSerializerOptions { WriteIndented = true });

                if (!isCvTextJson)
                {
                    throw new InvalidOperationException("CV_ANALYSIS_INVALID_FOR_MATCHING");
                }
                var stageTwoCvContext = _cvStageTwoContextBuilder.Build(cvText).Json;

                // 3. Prompt Stage 2
                var variables = new Dictionary<string, string>
                {
                    { "CV_TEXT", stageTwoCvContext },
                    { "PARSED_JD_REQUIREMENTS", parsedJdJson }
                };
                var prompt = matchingPromptSnapshot.Content;
                foreach (var variable in variables)
                {
                    var placeholder = $"[{variable.Key}]";
                    if (!prompt.Contains(placeholder, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"MATCHING_PROMPT_PLACEHOLDER_MISSING:{placeholder}");
                    }
                    prompt = prompt.Replace(placeholder, variable.Value, StringComparison.Ordinal);
                }

                prompt += useV3MatchingContract
                    ? "\n\n[SYSTEM CRITICAL]: Return compact JSON only. Score every itemId exactly once; do not omit itemScores, handlerCode, or handlerScore."
                    : "\n\n[SYSTEM CRITICAL]: Your output token limit is strictly capped. You MUST minify the JSON output (NO line breaks, NO indentation). Keep 'reasoning' under 20 words. Omit 'confidence' entirely.";

                _logger.LogInformation("Starting CV-JD AI scoring for match {MatchId} with {RequirementCount} validated JD requirements under {MatchingContract}.",
                    matchRecord.Id,
                    useV3MatchingContract ? stageTwoJdContext!.RequirementItemCount : requirementsList.Count,
                    useV3MatchingContract ? JdStageTwoContextBuilder.Contract : "jd-matching/legacy");

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    throw new Exception("Active Prompt for JD_MATCHING_PROMPT not found.");
                }

                // 4. Call LLM (Stage 2)
                string llmResponseText = await CallLlmBypassAsync(prompt, cancellationToken);

                try 
                {
                    string cleanLlmResp = ExtractJsonFromText(llmResponseText);
                    var jsonDoc = JsonDocument.Parse(cleanLlmResp);
                    var finalResult = useV3MatchingContract
                        ? _jdFitScoreCalculator.Calculate(stageTwoProjection!, _jdStageTwoResponseValidator.Validate(jsonDoc, stageTwoProjection!))
                        : ToJdFitScoreCalculation(CalculateFinalMatchResult(requirementsList, jsonDoc));
                    
                    if (!manageLifecycle)
                    {
                        return new CvJdMatchingExecutionResult(
                            finalResult.FinalScore,
                            finalResult.JsonString,
                            matchRecord.SfiaExtractResult);
                    }

                    matchRecord.Status = "Completed";
                    matchRecord.MatchScore = finalResult.FinalScore;
                    matchRecord.MatchDetails = finalResult.JsonString;
                    matchRecord.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (!manageLifecycle)
                        throw new InvalidOperationException("MATCHING_STAGE2_OUTPUT_INVALID", ex);

                    _logger.LogError("Stage 2 output rejected for match {MatchId}; code={ErrorCode}.", matchRecord.Id, "AI_OUTPUT_INVALID");
                    matchRecord.Status = "Failed";
                    matchRecord.ErrorMessage = "AI_OUTPUT_INVALID";
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
                    matchRecord.SfiaExtractResult);
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
                matchRecord.SfiaExtractResult);
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
                _logger.LogWarning(ex, "Matching source access changed before processing match {MatchId}", jobId);
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

        private async Task<string> ExtractJdWithV2Async(JobPostings? savedJob, string? rawJdText, string? requestedTitle)
        {
            if (_jobAnalysisExtractionService == null)
            {
                throw new InvalidOperationException("JOB_ANALYSIS_EXTRACTION_SERVICE_NOT_CONFIGURED");
            }

            var snapshot = savedJob == null
                ? new JobAnalysisInputBuilder().Build(new JobPostings
                {
                    Title = requestedTitle ?? string.Empty,
                    Description = JobPostingRichText.ToPlainText(rawJdText),
                    Requirements = string.Empty
                })
                : new JobAnalysisInputBuilder().Build(savedJob);

            var extraction = await _jobAnalysisExtractionService.ExtractWithActivePromptsAsync(snapshot);
            if (!extraction.Validation.IsValid || extraction.Validation.Data == null)
            {
                throw new InvalidOperationException($"INVALID_JD_ANALYSIS: {string.Join("; ", extraction.Validation.Errors)}");
            }

            return _jobAnalysisExtractionService.SerializeEffectiveAnalysis(extraction.Validation.Data);
        }

        private static decimal GetCategoryWeight(string category) => category switch
        {
            "tech_skill" => 1.0m,
            "experience" => 0.9m,
            "seniority_fit" => 0.9m,
            "domain_knowledge" => 0.7m,
            "language" => 0.6m,
            "education" => 0.5m,
            "soft_skill" => 0.4m,
            _ => 0.5m
        };

        private FinalMatchResult CalculateFinalMatchResult(List<ScoringRequirement> requirements, JsonDocument stage2Response)
        {
            LegacyJdStageTwoResponseValidator.Validate(
                stage2Response,
                requirements.Select(requirement => requirement.ReqId).ToArray());
            var root = stage2Response.RootElement;
            
            // 1. Process scores
            var scoreElements = root.TryGetProperty("scores", out var scoresProp) && scoresProp.ValueKind == JsonValueKind.Array
                ? scoresProp.EnumerateArray().ToList()
                : new List<JsonElement>();

            var finalScores = new List<object>();
            decimal poolA_Actual = 0m;
            decimal poolA_Max = 0m;
            decimal poolB_Actual = 0m;
            decimal poolB_Max = 0m;
            int criticalGapsCount = 0;
            var coreTechnicalMustHaveScores = new List<decimal>();

            foreach (var req in requirements)
            {
                // Find matching score from LLM
                var llmScore = scoreElements.FirstOrDefault(s => s.TryGetProperty("reqId", out var id) && id.GetString() == req.ReqId);
                
                string handlerCode = "UNKNOWN";
                decimal handlerScore = 0m;
                string reasoning = "";
                string flag = null;

                if (llmScore.ValueKind != JsonValueKind.Undefined)
                {
                    if (llmScore.TryGetProperty("handlerCode", out var hc)) handlerCode = hc.GetString();
                    if (llmScore.TryGetProperty("handlerScore", out var hs)) handlerScore = hs.GetDecimal();
                    if (llmScore.TryGetProperty("reasoning", out var rs)) reasoning = rs.GetString();
                    if (llmScore.TryGetProperty("flag", out var fl) && fl.ValueKind == JsonValueKind.String) flag = fl.GetString();
                }

                if (req.Importance == "must_have" && req.Category == "tech_skill")
                {
                    coreTechnicalMustHaveScores.Add(handlerScore);
                }

                // Ensure flag = CRITICAL_GAP if must_have and score = 0
                if (req.Importance == "must_have" && handlerScore == 0.0m)
                {
                    flag = "CRITICAL_GAP";
                    criticalGapsCount++;
                }

                // Math calculation
                decimal weightedScore = handlerScore * req.CategoryWeight;
                
                if (req.Importance == "must_have")
                {
                    poolA_Actual += weightedScore;
                    poolA_Max += 1.0m * req.CategoryWeight;
                }
                else
                {
                    poolB_Actual += weightedScore;
                    poolB_Max += 1.0m * req.CategoryWeight;
                }

                // Reconstruct full JSON object for this requirement
                finalScores.Add(new
                {
                    reqId = req.ReqId,
                    normalizedText = req.NormalizedText,
                    importance = req.Importance,
                    category = req.Category,
                    categoryWeight = req.CategoryWeight,
                    entities = new { }, // empty
                    handlerUsed = req.Category, // map category to handlerUsed
                    handlerCode = handlerCode,
                    handlerScore = handlerScore,
                    reasoning = reasoning,
                    confidence = "high", // Default
                    flag = flag
                });
            }

            // KSW_01 is a universal condition: all core technical must-haves
            // must be absent, not merely one missing technical requirement.
            var ksw01Triggered = coreTechnicalMustHaveScores.Count > 0 &&
                coreTechnicalMustHaveScores.All(score => score == 0m);

            // Math: Calculate Pool Percentages
            decimal poolAPercentage = poolA_Max > 0 ? (poolA_Actual / poolA_Max) * 70m : 70m; // Max 70 points
            decimal poolBPercentage = poolB_Max > 0 ? (poolB_Actual / poolB_Max) * 30m : 30m; // Max 30 points

            // Apply RULE_TC1_02: Pool A capped
            bool poolACapped = false;
            if (criticalGapsCount >= 2)
            {
                poolACapped = true;
                if (poolAPercentage > 28m) poolAPercentage = 28m;
            }

            // Process Penalties
            var penaltiesOutput = new List<object>();
            decimal totalDeduction = 0m;

            // Add auto-detected penalties
            if (poolACapped)
            {
                penaltiesOutput.Add(new { code = "RULE_TC1_02", triggered = true, deduction = 0, evidence = ">= 2 CRITICAL GAPs found. Pool A capped at 28 points." });
            }

            // LLM Penalties
            if (root.TryGetProperty("penalties", out var llmPenalties) && llmPenalties.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in llmPenalties.EnumerateArray())
                {
                    if (p.TryGetProperty("triggered", out var triggered) && triggered.GetBoolean())
                    {
                        decimal deduction = 15m; // Default for PNL_TC1_01
                        totalDeduction += deduction;
                        string code = p.TryGetProperty("code", out var cd) ? cd.GetString() : "UNKNOWN";
                        string evidence = p.TryGetProperty("evidence", out var ev) ? ev.GetString() : "";
                        penaltiesOutput.Add(new { code = code, triggered = true, deduction = deduction, evidence = evidence });
                    }
                }
            }

            // Add KSW_01 Penalty output
            if (ksw01Triggered)
            {
                penaltiesOutput.Add(new { code = "KSW_01", triggered = true, deduction = 0, evidence = "100% core tech skill is completely missing." });
            }

            // Final score
            decimal rawScore = poolAPercentage + poolBPercentage - totalDeduction;
            decimal finalScore = Math.Max(0m, Math.Min(100m, rawScore));
            if (ksw01Triggered) finalScore = 15m; // Force kill switch

            // Determine Result
            string resultText = finalScore >= 80 ? "Highly Suitable" : finalScore >= 60 ? "Suitable" : finalScore >= 40 ? "Partially Suitable" : "Not Suitable";

            // Extract other sections from LLM
            object criticalGaps = new object[] { };
            if (root.TryGetProperty("criticalGaps", out var cg)) criticalGaps = JsonSerializer.Deserialize<object>(cg.GetRawText());
            
            object improvements = new object[] { };
            if (root.TryGetProperty("improvements", out var imp)) improvements = JsonSerializer.Deserialize<object>(imp.GetRawText());

            string narrative = root.TryGetProperty("narrative", out var n) ? n.GetString() : "N/A";

            // Reconstruct the giant JSON structure for the frontend
            var finalJsonObj = new
            {
                mode = "jd_fit",
                jdFit = new
                {
                    score = Math.Round(finalScore, 1),
                    result = resultText,
                    killSwitchTriggered = ksw01Triggered,
                    poolACapped = poolACapped,
                    poolA = new { score = Math.Round(poolAPercentage, 1), max = 70 },
                    poolB = new { score = Math.Round(poolBPercentage, 1), max = 30 },
                    requirementScores = finalScores,
                    criticalGaps = criticalGaps,
                    penalties = penaltiesOutput,
                    narrative = narrative
                },
                improvements = improvements,
                processingTime = 1000 // Fixed or measured
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return new FinalMatchResult
            {
                FinalScore = finalScore,
                JsonString = JsonSerializer.Serialize(finalJsonObj, options)
            };
        }

        private static JdFitScoreCalculation ToJdFitScoreCalculation(FinalMatchResult legacyResult) =>
            new(legacyResult.FinalScore, legacyResult.JsonString);



        private async Task<string> CallLlmBypassAsync(string prompt, CancellationToken cancellationToken)
        {
            var provider = _configuration["AiSettings:DefaultProvider"] ?? "Gemini";
            var modelName = _configuration[$"AiSettings:Providers:{provider}:Model"] ?? "gemini-1.5-flash-latest";
            var apiKey = _configuration[$"AiSettings:Providers:{provider}:ApiKey"];
            var endpoint = _configuration[$"AiSettings:Providers:{provider}:Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models";
            
            if (string.IsNullOrWhiteSpace(apiKey)) 
            {
                var dbKeyConfig = await _systemConfigRepository.GetByKeyAsync($"AiApiKey_{provider}");
                apiKey = dbKeyConfig?.ConfigValue;
            }

            if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception($"API Key is missing for Bypass Flow ({provider}).");

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2); // Thêm timeout dài cho LLM

            if (provider == "Gemini" || modelName.Contains("gemini", StringComparison.OrdinalIgnoreCase))
            {
                if (endpoint.EndsWith("/")) endpoint = endpoint.TrimEnd('/');
                var url = $"{endpoint}/{modelName}:generateContent?key={apiKey}";
                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new 
                    { 
                        maxOutputTokens = 8192,
                        temperature = 0.2,
                        responseMimeType = "application/json"
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                    }
                };

                int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    string responseContent = "";
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = JsonContent.Create(payload)
                        };
                        using var response = await client.SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            cancellationToken);
                        responseContent = await BoundedHttpContentReader.ReadAsStringAsync(
                            response.Content,
                            BoundedHttpContentReader.DefaultMaxBytes,
                            cancellationToken);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            using var jsonDoc = JsonDocument.Parse(responseContent);
                            if (jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && 
                                candidates.ValueKind == JsonValueKind.Array && 
                                candidates.GetArrayLength() > 0)
                            {
                                var candidate = candidates[0];
                                
                                if (candidate.TryGetProperty("finishReason", out var frProp))
                                {
                                    var finishReason = frProp.GetString();
                                    if (finishReason != "STOP")
                                    {
                                        _logger.LogWarning("Gemini stopped unexpectedly with finishReason: {FinishReason}", finishReason);
                                    }
                                }

                                if (candidate.TryGetProperty("content", out var content) &&
                                    content.TryGetProperty("parts", out var parts) &&
                                    parts.ValueKind == JsonValueKind.Array &&
                                    parts.GetArrayLength() > 0 &&
                                    parts[0].TryGetProperty("text", out var textElement))
                                {
                                    var text = textElement.GetString() ?? string.Empty;
                                    
                                    text = ExtractJsonFromText(text);

                                    try 
                                    {
                                        using (var testParse = JsonDocument.Parse(text)) { } 
                                        return text; 
                                    }
                                    catch (JsonException)
                                    {
                                        // Never repair or fabricate truncated JSON; retry with a fresh provider response.
                                        _logger.LogWarning(
                                            "Gemini returned invalid JSON on attempt {Attempt}; retrying. ResponseLength={ResponseLength}.",
                                            attempt,
                                            text.Length);
                                        if (attempt == maxRetries)
                                        {
                                            throw new InvalidOperationException("AI_PROVIDER_INVALID_JSON");
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Gemini response missing content or parts. Attempt {Attempt}.", attempt);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Gemini response missing candidates (might be blocked). Attempt {Attempt}.", attempt);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Gemini API returned HTTP {StatusCode} on attempt {Attempt}.", response.StatusCode, attempt);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exception during Gemini API call on attempt {Attempt}", attempt);
                        if (attempt == maxRetries)
                        {
                            throw new InvalidOperationException("AI_PROVIDER_REQUEST_FAILED", ex);
                        }
                    }

                    if (attempt == maxRetries)
                    {
                        throw new InvalidOperationException("AI_PROVIDER_HTTP_ERROR");
                    }
                    
                    await Task.Delay(2000, cancellationToken);
                }
            }
            else if (modelName.Contains("claude"))
            {
                var url = "https://api.anthropic.com/v1/messages";
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                client.DefaultRequestHeaders.Add("User-Agent", "ITHunterview-Bypass");

                var payload = new
                {
                    model = modelName,
                    max_tokens = 4000,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(payload)
                };
                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                var responseContent = await BoundedHttpContentReader.ReadAsStringAsync(
                    response.Content,
                    BoundedHttpContentReader.DefaultMaxBytes,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("AI_PROVIDER_HTTP_ERROR");

                var jsonDoc = JsonDocument.Parse(responseContent);
                var text = jsonDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                return text ?? string.Empty;
            }

            throw new Exception("Unsupported Model Name for Bypass Flow.");
        }

        public async Task<MatchingResultDto?> GetMatchingResultAsync(Guid jobId, Guid userId)
        {
            var matchRecord = await _context.CvJobMatchScores
                .FirstOrDefaultAsync(m => m.Id == jobId && m.UserId == userId);
                
            if (matchRecord == null) return null;

            return new MatchingResultDto
            {
                Id = matchRecord.Id,
                CvId = matchRecord.CvId,
                CvFileName = matchRecord.CvFileName,
                JobId = matchRecord.JobId,
                JdTitle = matchRecord.JdTitle,
                Status = matchRecord.Status,
                ErrorMessage = matchRecord.ErrorMessage,
                MatchDetails = matchRecord.MatchDetails,
                JdFit = new JdFitResultDto { Score = matchRecord.MatchScore ?? 0m }
            };
        }

        private string ExtractJsonFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Phòng trường hợp nó trả về {...} không có markdown nhưng thừa chữ
            var startIndex = text.IndexOf('{');
            var endIndex = text.LastIndexOf('}');
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                return text.Substring(startIndex, endIndex - startIndex + 1).Trim();
            }
            
            // Nếu không có } ở cuối (bị truncate), lấy từ { đến cuối
            return text.Trim();
        }

        public async Task<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>> GetMatchHistoryAsync(Guid userId, int page, int pageSize, Guid? cvId = null)
        {
            var query = from s in _context.CvJobMatchScores
                        join c in _context.Cvs on s.CvId equals c.Id into cvs
                        from c in cvs.DefaultIfEmpty()
                        join j in _context.JobPostings on s.JobId equals j.Id into jobs
                        from j in jobs.DefaultIfEmpty()
                        where s.UserId == userId
                        select new { Score = s, Cv = c, Job = j };

            if (cvId.HasValue)
            {
                query = query.Where(x => x.Score.CvId == cvId.Value);
            }

            query = query.OrderByDescending(x => x.Score.UpdatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var mappedItems = items.Select(x => new ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto
            {
                JobId = x.Score.Id,
                CvId = x.Score.CvId,
                CandidateId = x.Cv?.UserId,
                CvFileName = x.Cv?.FileName ?? x.Score.CvFileName ?? "Unknown CV",
                FileUrl = x.Cv?.FileUrl,
                SourceJobId = x.Score.JobId,
                JdTitle = x.Job?.Title ?? x.Score.JdTitle ?? x.Score.RawJdText,
                MatchScore = x.Score.MatchScore,
                Status = x.Score.Status,
                ErrorMessage = x.Score.ErrorMessage,
                UpdatedAt = x.Score.UpdatedAt,
                MatchType = x.Score.MatchType
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
            var query = from s in _context.CvJobMatchScores
                        join c in _context.Cvs on s.CvId equals c.Id into cvs
                        from c in cvs.DefaultIfEmpty()
                        where s.JobId == jobId
                        orderby s.MatchScore descending
                        select new { Score = s, Cv = c };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

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
                    Status = x.Score.Status,
                    ErrorMessage = x.Score.ErrorMessage,
                    UpdatedAt = x.Score.UpdatedAt,
                    MatchType = x.Score.MatchType,
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

        public async Task DeleteMatchHistoryAsync(Guid jobId, Guid userId)
        {
            var matchRecord = await _context.CvJobMatchScores
                .FirstOrDefaultAsync(m => m.Id == jobId && m.UserId == userId);

            if (matchRecord == null)
            {
                throw new KeyNotFoundException("Match history not found or you do not have permission to delete it.");
            }

            _context.CvJobMatchScores.Remove(matchRecord);
            await _context.SaveChangesAsync();
        }

        private class ScoringRequirement
        {
            public string ReqId { get; set; }
            public string NormalizedText { get; set; }
            public string Category { get; set; }
            public string Importance { get; set; }
            public string DetailVerbatim { get; set; }
            public decimal CategoryWeight { get; set; }
            public string Operator { get; set; } = "all_of";
            public int MinSatisfied { get; set; }
            public string Evidence { get; set; } = string.Empty;
        }

        private class FinalMatchResult
        {
            public decimal FinalScore { get; set; }
            public string JsonString { get; set; }
        }
    }
}
