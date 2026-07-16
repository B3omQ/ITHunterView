using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Infrastructure.Persistence;
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

namespace ITHunterview.Service.UseCase
{

    public class CvJobMatchingUseCase : ICvJobMatchingUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly IAiEmbeddingService _aiService;
        private readonly ICvTextExtractorService _cvTextExtractorService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CvJobMatchingUseCase> _logger;
        private readonly IPromptManagementService _promptManagementService;

        public CvJobMatchingUseCase(
            ITHunterviewContext context, 
            IAiEmbeddingService aiService,
            ICvTextExtractorService cvTextExtractorService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<CvJobMatchingUseCase> logger,
            IPromptManagementService promptManagementService)
        {
            _context = context;
            _aiService = aiService;
            _cvTextExtractorService = cvTextExtractorService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _promptManagementService = promptManagementService;
        }

        public string ExtractJsonField(string? jsonString, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) return string.Empty;
            try
            {
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;
                
                if (root.TryGetProperty(fieldName, out var element))
                {
                    return element.ToString() ?? string.Empty;
                }
                
                // For deep nested properties like "position.title"
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
                            return string.Empty;
                        }
                    }
                    return current.ToString() ?? string.Empty;
                }
            }
            catch
            {
                // Ignore parse errors, just return empty
            }
            return string.Empty;
        }

        private async Task GenerateEmbeddingsForCvAsync(Cvs cv)
        {
            bool updated = false;
            
            if (cv.TitleEmbedding == null)
            {
                var titleText = ExtractJsonField(cv.ParsedData, "job_title");
                if (string.IsNullOrEmpty(titleText)) titleText = "Unknown Title";
                cv.TitleEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(titleText));
                updated = true;
            }
            if (cv.SkillsEmbedding == null)
            {
                var skillsText = ExtractJsonField(cv.ParsedData, "skills");
                if (string.IsNullOrEmpty(skillsText)) skillsText = "No skills provided";
                cv.SkillsEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(skillsText));
                updated = true;
            }
            if (cv.ExperienceEmbedding == null)
            {
                var expText = ExtractJsonField(cv.ParsedData, "experience");
                if (string.IsNullOrEmpty(expText)) expText = "No experience provided";
                cv.ExperienceEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(expText));
                updated = true;
            }
            if (cv.DomainEmbedding == null)
            {
                // Fallback to experience if domain is missing
                var domainText = ExtractJsonField(cv.ParsedData, "domain");
                if (string.IsNullOrEmpty(domainText)) domainText = ExtractJsonField(cv.ParsedData, "experience");
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
            
            if (job.TitleEmbedding == null)
            {
                var titleText = ExtractJsonField(job.ParsedData, "position.title");
                if (string.IsNullOrEmpty(titleText)) titleText = job.Title ?? "Unknown Title";
                job.TitleEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(titleText));
                updated = true;
            }
            if (job.SkillsEmbedding == null)
            {
                var skillsText = ExtractJsonField(job.ParsedData, "tech_requirements");
                if (string.IsNullOrEmpty(skillsText)) skillsText = job.Requirements ?? "No requirements provided";
                job.SkillsEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(skillsText));
                updated = true;
            }
            if (job.ExperienceEmbedding == null)
            {
                var expText = ExtractJsonField(job.ParsedData, "seniority_signals") + " " + ExtractJsonField(job.ParsedData, "engineering_expectations");
                if (string.IsNullOrWhiteSpace(expText)) expText = job.Responsibilities ?? "No responsibilities provided";
                job.ExperienceEmbedding = new Vector(await _aiService.GenerateEmbeddingAsync(expText));
                updated = true;
            }
            if (job.DomainEmbedding == null)
            {
                var domainText = ExtractJsonField(job.ParsedData, "domain");
                if (string.IsNullOrEmpty(domainText)) domainText = job.Description ?? "Unknown domain";
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

            // Fetch all jobs that have embeddings
            var jobs = await _context.JobPostings
                .Where(j => j.Status == ITHunterview.Domain.Enums.JobStatus.PUBLISHED && j.TitleEmbedding != null && j.SkillsEmbedding != null && j.ExperienceEmbedding != null && j.DomainEmbedding != null)
                .ToListAsync();

            var matchScores = new List<CvJobMatchScores>();

            foreach (var job in jobs)
            {
                var existingScore = await _context.CvJobMatchScores
                    .FirstOrDefaultAsync(s => s.CvId == cvId && s.JobId == job.Id && s.UserId == userId);

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
                        Id = Guid.NewGuid(),
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

            await GenerateEmbeddingsForJobAsync(job);

            var cvs = await _context.Cvs
                .Where(c => c.IsPrimary && c.TitleEmbedding != null && c.SkillsEmbedding != null && c.ExperienceEmbedding != null && c.DomainEmbedding != null)
                .ToListAsync();

            foreach (var cv in cvs)
            {
                var existingScore = await _context.CvJobMatchScores
                    .FirstOrDefaultAsync(s => s.CvId == cv.Id && s.JobId == jobId && s.UserId == userId);

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
                        Id = Guid.NewGuid(),
                        UserId = userId,
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

        public async Task<Guid> SubmitMatchingJobAsync(Guid userId, MatchingRequestDto request)
        {
            var matchScore = new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CvId = request.CvId,
                CvFileName = request.CvFileName ?? (string.IsNullOrEmpty(request.CvText) && string.IsNullOrEmpty(request.CvUrl) ? null : "Bypass CV"),
                JobId = request.JobId,
                JdTitle = request.JdTitle ?? (string.IsNullOrEmpty(request.RawJdText) ? null : "Bypass JD"),
                RawJdText = null,
                MatchScore = 0,
                Status = "Pending",
                UpdatedAt = DateTime.UtcNow
            };

            _context.CvJobMatchScores.Add(matchScore);
            await _context.SaveChangesAsync();
            return matchScore.Id;
        }

        public async Task ProcessMatchingJobAsync(Guid jobId, Guid userId, MatchingRequestDto request)
        {
            var matchRecord = await _context.CvJobMatchScores.FindAsync(jobId);
            if (matchRecord == null) return;

            try
            {
                matchRecord.Status = "Processing";
                matchRecord.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 1. Get CV Text
                string cvText = request.CvText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(cvText))
                {
                    // KHáº®C PHá»¤C KHUYáº¾T ÄIá»‚M: Xá»­ lÃ½ file Upload tá»« Frontend
                    if (!string.IsNullOrWhiteSpace(request.CvUrl))
                    {
                        cvText = await _cvTextExtractorService.ExtractTextFromUrlAsync(request.CvUrl);
                    }
                    else 
                    {
                        var cv = matchRecord.CvId.HasValue ? await _context.Cvs.FindAsync(matchRecord.CvId.Value) : null;
                        if (cv != null)
                        {
                            if (!string.IsNullOrWhiteSpace(cv.ParsedData))
                                cvText = cv.ParsedData;
                            else if (!string.IsNullOrWhiteSpace(cv.FileUrl))
                                cvText = await _cvTextExtractorService.ExtractTextFromUrlAsync(cv.FileUrl);
                        }
                    }
                }

                // 2. Get JD Text
                string jdText = request.RawJdText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(jdText) && request.JobId.HasValue)
                {
                    var job = await _context.JobPostings.FindAsync(request.JobId.Value);
                    if (job != null)
                    {
                        jdText = $"{job.Title}\n\nDescription:\n{job.Description}\n\nResponsibilities:\n{job.Responsibilities}\n\nRequirements:\n{job.Requirements}\n\nBenefits & Perks:\n{job.Benefits}";
                        if (string.IsNullOrEmpty(matchRecord.JdTitle)) matchRecord.JdTitle = job.Title;
                    }
                }
                
                // Cáº­p nháº­t tÃªn CV náº¿u lÃ  CV tá»« há»‡ thá»‘ng
                if (matchRecord.CvId.HasValue && string.IsNullOrEmpty(matchRecord.CvFileName))
                {
                    var cv = await _context.Cvs.FindAsync(matchRecord.CvId.Value);
                    if (cv != null) matchRecord.CvFileName = cv.FileName ?? "Saved CV";
                }

                if (string.IsNullOrWhiteSpace(cvText))
                {
                    var cvSource = request.CvId.HasValue ? $"CV ID={request.CvId}" : "uploaded file";
                    var urlDebug = !string.IsNullOrWhiteSpace(request.CvUrl) ? $"[URL: {request.CvUrl}] " : "";
                    throw new Exception($"Cannot extract text from CV ({cvSource}). {urlDebug}The file URL may be an invalid PDF/DOCX or blocked by Cloudinary. Please try using 'Paste Text' tab instead.");
                }

                if (string.IsNullOrWhiteSpace(jdText))
                {
                    var jdSource = request.JobId.HasValue ? $"Job ID={request.JobId}" : "provided JD";
                    throw new Exception($"Cannot extract Job Description text ({jdSource}). The job posting may have no description. Please try using 'Paste JD Text' tab instead.");
                }

                // Giá»›i háº¡n input Ä‘á»ƒ trÃ¡nh ná»• token
                if (cvText.Length > 20000) cvText = cvText.Substring(0, 20000);
                if (jdText.Length > 15000) jdText = jdText.Substring(0, 15000);

                // 3. Prompt
                var variables = new Dictionary<string, string>
                {
                    { "CV_TEXT", cvText },
                    { "JD_TEXT", jdText }
                };
                var prompt = await _promptManagementService.GetActivePromptContentWithVariablesAsync(
                    ITHunterview.Service.Constant.Prompts.BypassMatchingPrompt.Key, variables);

                _logger.LogInformation("\n========== START LLM PROMPT FOR CV-JD MATCHING ==========\n{Prompt}\n========== END LLM PROMPT ==========\n", prompt);

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    throw new Exception("Active Prompt for JD_MATCHING_PROMPT not found. Please contact Administrator.");
                }

                // 4. Call LLM
                string llmResponseText = await CallLlmBypassAsync(prompt);

                // Deserialize to extract final score and validate JSON
                decimal finalScore = 0m;
                try 
                {
                    var jsonDoc = JsonDocument.Parse(llmResponseText);
                    finalScore = EnforceScoreRules(jsonDoc);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON Parse Error.");
                    
                    // Ghi Ä‘Ã¨ tráº¡ng thÃ¡i Failed nhÆ°ng váº«n giá»¯ chuá»—i rÃ¡c trong MatchDetails Ä‘á»ƒ debug
                    matchRecord.Status = "Failed";
                    matchRecord.ErrorMessage = "LLM returned invalid JSON format. Backend failed to parse.";
                    matchRecord.MatchDetails = llmResponseText;
                    matchRecord.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return; // ThoÃ¡t hÃ m sá»›m, khÃ´ng gÃ¡n Completed ná»¯a
                }

                matchRecord.Status = "Completed";
                matchRecord.MatchScore = finalScore;
                matchRecord.MatchDetails = llmResponseText;
                matchRecord.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                matchRecord.Status = "Failed";
                matchRecord.ErrorMessage = ex.Message;
                matchRecord.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private decimal EnforceScoreRules(JsonDocument jsonDoc)
        {
            if (!jsonDoc.RootElement.TryGetProperty("jdFit", out var jdFit))
                return 0m;

            // 1. Kill-Switch check
            if (jdFit.TryGetProperty("killSwitchTriggered", out var ksw) && ksw.GetBoolean())
            {
                _logger.LogInformation("KSW_01 triggered â€” score frozen at 15");
                return 15m;
            }

            // 2. Láº¥y raw score
            decimal rawScore = 0m;
            if (jdFit.TryGetProperty("score", out var scoreEl))
                rawScore = scoreEl.GetDecimal();

            // 3. Pool A cap check
            decimal poolAScore = 0m;
            if (jdFit.TryGetProperty("poolA", out var poolA) && 
                poolA.TryGetProperty("score", out var paScore))
            {
                poolAScore = paScore.GetDecimal();
            }
            
            bool poolACapped = false;
            if (jdFit.TryGetProperty("poolACapped", out var capped))
                poolACapped = capped.GetBoolean();

            if (poolACapped && poolAScore > 28m)
            {
                // Recalculate: clamp Pool A táº¡i 28 vÃ  recompute total
                decimal poolBScore = 0m;
                if (jdFit.TryGetProperty("poolB", out var poolB) &&
                    poolB.TryGetProperty("score", out var pbScore))
                    poolBScore = pbScore.GetDecimal();
                rawScore = 28m + poolBScore;
                _logger.LogInformation("RULE_TC1_02: Pool A capped. Recalculated score = {Score}", rawScore);
            }

            // 4. TÃ­nh láº¡i penalty deductions
            decimal totalDeduction = 0m;
            if (jdFit.TryGetProperty("penalties", out var penalties) &&
                penalties.ValueKind == JsonValueKind.Array)
            {
                foreach (var penalty in penalties.EnumerateArray())
                {
                    if (penalty.TryGetProperty("triggered", out var triggered) && triggered.GetBoolean())
                    {
                        if (penalty.TryGetProperty("deduction", out var ded))
                        {
                            var dedVal = ded.GetDecimal();
                            // Deduction cÃ³ thá»ƒ lÃ  sá»‘ Ã¢m hoáº·c dÆ°Æ¡ng â†’ normalize thÃ nh dÆ°Æ¡ng
                            totalDeduction += Math.Abs(dedVal);
                            _logger.LogInformation("Penalty triggered. Deduction = {Deduction}", dedVal);
                        }
                    }
                }
            }

            decimal finalScore = rawScore - totalDeduction;
            return Math.Max(0m, Math.Min(100m, finalScore)); // Clamp [0, 100]
        }

        private async Task<string> CallLlmBypassAsync(string prompt)
        {
            var modelName = _configuration["AiBypassConfig:ModelName"];
            var apiKey = _configuration["AiBypassConfig:ApiKey"];
            
            if (string.IsNullOrWhiteSpace(modelName)) modelName = _configuration["AiSettings:Providers:Gemini:Model"] ?? "gemini-1.5-flash-latest";
            if (string.IsNullOrWhiteSpace(apiKey)) apiKey = _configuration["AiSettings:Providers:Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("API Key is missing for Bypass Flow in appsettings.");

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2); // ThÃªm timeout dÃ i cho LLM

            if (modelName.Contains("gemini"))
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new 
                    { 
                        maxOutputTokens = 8192, // TrÃ¡nh JSON bá»‹ cáº¯t cá»¥t giá»¯a chá»«ng
                        temperature = 0.2 // Cáº§n sá»± chÃ­nh xÃ¡c cao
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                    }
                };

                // Simple Retry Policy (3 attempts)
                int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    var response = await client.PostAsJsonAsync(url, payload);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        var candidate = jsonDoc.RootElement.GetProperty("candidates")[0];
                        
                        if (candidate.TryGetProperty("finishReason", out var frProp))
                        {
                            var finishReason = frProp.GetString();
                            if (finishReason != "STOP")
                            {
                                _logger.LogWarning("Gemini stopped unexpectedly with finishReason: {FinishReason}", finishReason);
                            }
                        }

                        var text = candidate
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString() ?? string.Empty;
                        
                        // Log 500 kÃ½ tá»± Ä‘áº§u Ä‘á»ƒ debug
                        _logger.LogInformation("Gemini raw text (first 500 chars): {Text}", text.Length > 500 ? text.Substring(0, 500) : text);

                        // Fallback: DÃ¹ng Regex bÃ³c JSON ra khá»i markdown ```json ... ``` hoáº·c láº¥y tháº³ng náº¿u lÃ  JSON thuáº§n
                        text = ExtractJsonFromText(text);

                        // THÃŠM: Validate JSON ngay táº¡i Ä‘Ã¢y, náº¿u Ä‘á»©t Ä‘uÃ´i/há»ng thÃ¬ nÃ©m lá»—i Ä‘á»ƒ Retry
                        try 
                        {
                            using (var testParse = JsonDocument.Parse(text)) { } // Chá»‰ Ä‘á»ƒ test
                            return text; // Há»£p lá»‡, tráº£ vá»
                        }
                        catch (JsonException ex)
                        {
                            if (attempt == maxRetries)
                            {
                                throw new Exception($"Gemini tráº£ vá» JSON lá»—i sau {maxRetries} láº§n thá»­. Lá»—i: {ex.Message}");
                            }
                            _logger.LogWarning("Gemini sinh JSON lá»—i á»Ÿ láº§n thá»­ {Attempt}. Sáº½ retry. Text 500 chars: {Text}", attempt, text.Length > 500 ? text.Substring(0, 500) : text);
                        }
                    }

                    // Náº¿u lá»—i 503 hoáº·c cÃ¡c lá»—i API khÃ¡c, thá»­ láº¡i
                    if (attempt == maxRetries)
                    {
                        throw new Exception($"Gemini API Error after {maxRetries} attempts: {responseContent}");
                    }
                    
                    // Äá»£i 2 giÃ¢y trÆ°á»›c khi thá»­ láº¡i
                    await Task.Delay(2000);
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

                var response = await client.PostAsJsonAsync(url, payload);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Claude API Error: {responseContent}");

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

            // PhÃ²ng trÆ°á»ng há»£p nÃ³ tráº£ vá» {...} khÃ´ng cÃ³ markdown nhÆ°ng thá»«a chá»¯
            var startIndex = text.IndexOf('{');
            var endIndex = text.LastIndexOf('}');
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                return text.Substring(startIndex, endIndex - startIndex + 1).Trim();
            }

            return text.Trim();
        }
        public async Task<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>> GetMatchHistoryAsync(Guid userId, int page, int pageSize, Guid? cvId = null)
        {
            var query = from s in _context.CvJobMatchScores
                        join c in _context.Cvs on s.CvId equals c.Id into cvs
                        from c in cvs.DefaultIfEmpty()
                        join j in _context.JobPostings on s.JobId equals j.Id into jobs
                        from j in jobs.DefaultIfEmpty()
                        where s.UserId == userId && j != null
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
                CvFileName = x.Cv?.FileName ?? x.Score.CvFileName ?? "Unknown CV",
                FileUrl = x.Cv?.FileUrl,
                SourceJobId = x.Score.JobId,
                JdTitle = x.Score.JdTitle,
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

        public async Task<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>> GetJobMatchHistoryAsync(Guid jobId, Guid userId, int page, int pageSize)
        {
            var query = from s in _context.CvJobMatchScores
                        join c in _context.Cvs on s.CvId equals c.Id into cvs
                        from c in cvs.DefaultIfEmpty()
                        where s.JobId == jobId && s.UserId == userId
                        orderby s.MatchScore descending
                        select new { Score = s, Cv = c };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var mappedItems = items.Select(x => new ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto
            {
                JobId = x.Score.JobId ?? Guid.Empty,
                CvId = x.Score.CvId,
                CvFileName = x.Cv?.FileName ?? "Unknown CV",
                FileUrl = x.Cv?.FileUrl,
                SourceJobId = x.Score.Id, // using this for primary key mapping if needed
                JdTitle = x.Score.JdTitle,
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
    }
}
