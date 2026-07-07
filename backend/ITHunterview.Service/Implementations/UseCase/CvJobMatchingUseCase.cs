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

        public CvJobMatchingUseCase(ITHunterviewContext context, IAiEmbeddingService aiService)
        {
            _context = context;
            _aiService = aiService;
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
    }
}
