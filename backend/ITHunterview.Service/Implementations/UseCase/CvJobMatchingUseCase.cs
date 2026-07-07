using System;
using System.Linq;
using System.Text.Json;
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

namespace ITHunterview.Service.Implementations.UseCase
{
    public class MatchingWeights
    {
        public decimal TitleWeight { get; set; } = 0.15m;
        public decimal SkillsWeight { get; set; } = 0.45m;
        public decimal ExperienceWeight { get; set; } = 0.30m;
        public decimal DomainWeight { get; set; } = 0.10m;
    }

    public class CvJobMatchingUseCase : ICvJobMatchingUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly IAiEmbeddingService _aiService;
        private readonly ICvTextExtractorService _cvTextExtractorService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CvJobMatchingUseCase> _logger;

        public CvJobMatchingUseCase(
            ITHunterviewContext context, 
            IAiEmbeddingService aiService,
            ICvTextExtractorService cvTextExtractorService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<CvJobMatchingUseCase> logger)
        {
            _context = context;
            _aiService = aiService;
            _cvTextExtractorService = cvTextExtractorService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
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
            var distance = v1.CosineDistance(v2);
            var score = 1.0m - (decimal)distance;
            return score < 0 ? 0 : score;
        }

        public async Task MatchCvWithAllJobsAsync(Guid cvId)
        {
            var cv = await _context.Cvs.FindAsync(cvId);
            if (cv == null) throw new Exception("CV not found");

            await GenerateEmbeddingsForCvAsync(cv);

            // Fetch all jobs that have embeddings
            var jobs = await _context.JobPostings
                .Where(j => j.TitleEmbedding != null && j.SkillsEmbedding != null && j.ExperienceEmbedding != null && j.DomainEmbedding != null)
                .ToListAsync();

            var weights = new MatchingWeights();

            var matchScores = new List<CvJobMatchScores>();

            foreach (var job in jobs)
            {
                var titleScore = CalculateComponentScore(cv.TitleEmbedding, job.TitleEmbedding);
                var skillsScore = CalculateComponentScore(cv.SkillsEmbedding, job.SkillsEmbedding);
                var expScore = CalculateComponentScore(cv.ExperienceEmbedding, job.ExperienceEmbedding);
                var domainScore = CalculateComponentScore(cv.DomainEmbedding, job.DomainEmbedding);

                var finalScore = (titleScore * weights.TitleWeight) +
                                 (skillsScore * weights.SkillsWeight) +
                                 (expScore * weights.ExperienceWeight) +
                                 (domainScore * weights.DomainWeight);

                var details = JsonSerializer.Serialize(new 
                {
                    TitleScore = Math.Round(titleScore, 4),
                    SkillsScore = Math.Round(skillsScore, 4),
                    ExperienceScore = Math.Round(expScore, 4),
                    DomainScore = Math.Round(domainScore, 4),
                    FinalScore = Math.Round(finalScore, 4),
                    Weights = weights
                });

                var existingScore = await _context.CvJobMatchScores
                    .FirstOrDefaultAsync(s => s.CvId == cvId && s.JobId == job.Id);

                if (existingScore != null)
                {
                    existingScore.MatchScore = finalScore;
                    existingScore.UpdatedAt = DateTime.UtcNow;
                    existingScore.MatchDetails = details;
                }
                else
                {
                    _context.CvJobMatchScores.Add(new CvJobMatchScores
                    {
                        Id = Guid.NewGuid(),
                        CvId = cvId,
                        JobId = job.Id,
                        RawJdText = job.Title,
                        MatchScore = finalScore,
                        MatchDetails = details,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task MatchJobWithAllCvsAsync(Guid jobId)
        {
            var job = await _context.JobPostings.FindAsync(jobId);
            if (job == null) throw new Exception("Job not found");

            await GenerateEmbeddingsForJobAsync(job);

            var cvs = await _context.Cvs
                .Where(c => c.TitleEmbedding != null && c.SkillsEmbedding != null && c.ExperienceEmbedding != null && c.DomainEmbedding != null)
                .ToListAsync();

            var weights = new MatchingWeights();

            foreach (var cv in cvs)
            {
                var titleScore = CalculateComponentScore(cv.TitleEmbedding, job.TitleEmbedding);
                var skillsScore = CalculateComponentScore(cv.SkillsEmbedding, job.SkillsEmbedding);
                var expScore = CalculateComponentScore(cv.ExperienceEmbedding, job.ExperienceEmbedding);
                var domainScore = CalculateComponentScore(cv.DomainEmbedding, job.DomainEmbedding);

                var finalScore = (titleScore * weights.TitleWeight) +
                                 (skillsScore * weights.SkillsWeight) +
                                 (expScore * weights.ExperienceWeight) +
                                 (domainScore * weights.DomainWeight);

                var details = JsonSerializer.Serialize(new 
                {
                    TitleScore = Math.Round(titleScore, 4),
                    SkillsScore = Math.Round(skillsScore, 4),
                    ExperienceScore = Math.Round(expScore, 4),
                    DomainScore = Math.Round(domainScore, 4),
                    FinalScore = Math.Round(finalScore, 4),
                    Weights = weights
                });

                var existingScore = await _context.CvJobMatchScores
                    .FirstOrDefaultAsync(s => s.CvId == cv.Id && s.JobId == jobId);

                if (existingScore != null)
                {
                    existingScore.MatchScore = finalScore;
                    existingScore.UpdatedAt = DateTime.UtcNow;
                    existingScore.MatchDetails = details;
                }
                else
                {
                    _context.CvJobMatchScores.Add(new CvJobMatchScores
                    {
                        Id = Guid.NewGuid(),
                        CvId = cv.Id,
                        JobId = jobId,
                        RawJdText = job.Title,
                        MatchScore = finalScore,
                        MatchDetails = details,
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
                CvId = request.CvId ?? Guid.Empty,
                JobId = request.JobId,
                RawJdText = "", // Bắt buộc hoặc optional
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
                    // KHẮC PHỤC KHUYẾT ĐIỂM: Xử lý file Upload từ Frontend
                    if (!string.IsNullOrWhiteSpace(request.CvUrl))
                    {
                        cvText = await _cvTextExtractorService.ExtractTextFromUrlAsync(request.CvUrl);
                    }
                    else 
                    {
                        var cv = await _context.Cvs.FindAsync(matchRecord.CvId);
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
                        jdText = $"{job.Title}\n{job.Description}\n{job.Requirements}";
                    }
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

                // Giới hạn input để tránh nổ token
                if (cvText.Length > 20000) cvText = cvText.Substring(0, 20000);
                if (jdText.Length > 15000) jdText = jdText.Substring(0, 15000);

                // 3. Prompt
                var prompt = ITHunterview.Service.Constant.Prompts.BypassMatchingPrompt.GetPrompt(cvText, jdText);

                // 4. Call LLM
                string llmResponseText = await CallLlmBypassAsync(prompt);

                // Deserialize to extract final score
                decimal finalScore = 0m;
                try 
                {
                    var jsonDoc = JsonDocument.Parse(llmResponseText);
                    if (jsonDoc.RootElement.TryGetProperty("jdFit", out var jdFitElement))
                    {
                        if (jdFitElement.TryGetProperty("score", out var scoreElement))
                        {
                            finalScore = scoreElement.GetDecimal();
                        }
                    }
                }
                catch { /* Ignore parse error for score, just keep 0 */ }

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

        private async Task<string> CallLlmBypassAsync(string prompt)
        {
            var modelName = _configuration["AiBypassConfig:ModelName"];
            var apiKey = _configuration["AiBypassConfig:ApiKey"];
            
            if (string.IsNullOrWhiteSpace(modelName)) modelName = "gemini-1.5-flash";
            if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("API Key is missing for Bypass Flow in appsettings.");

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2); // Thêm timeout dài cho LLM

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
                        response_mime_type = "application/json",
                        maxOutputTokens = 8192, // Tránh JSON bị cắt cụt giữa chừng
                        temperature = 0.2 // Cần sự chính xác cao
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
                        var text = jsonDoc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString() ?? string.Empty;
                        
                        // Log 500 ký tự đầu để debug
                        _logger.LogInformation("Gemini raw text (first 500 chars): {Text}", text.Length > 500 ? text.Substring(0, 500) : text);

                        // Fallback: nếu model vẫn cố wrap trong markdown dù đã bật JSON mode
                        if (text.TrimStart().StartsWith("```"))
                        {
                            var startIdx = text.IndexOf('\n') + 1;
                            var endIdx = text.LastIndexOf("```");
                            if (endIdx > startIdx)
                                text = text.Substring(startIdx, endIdx - startIdx).Trim();
                        }

                        return text;
                    }

                    // Nếu lỗi 503 hoặc các lỗi API khác, thử lại
                    if (attempt == maxRetries)
                    {
                        throw new Exception($"Gemini API Error after {maxRetries} attempts: {responseContent}");
                    }
                    
                    // Đợi 2 giây trước khi thử lại
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
                JobId = matchRecord.JobId,
                Status = matchRecord.Status,
                ErrorMessage = matchRecord.ErrorMessage,
                MatchDetails = matchRecord.MatchDetails,
                JdFit = new JdFitResultDto { Score = matchRecord.MatchScore ?? 0m }
            };
        }
    }
}
