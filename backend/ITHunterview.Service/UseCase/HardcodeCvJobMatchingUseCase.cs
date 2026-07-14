using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.UseCase
{
    public class HardcodeCvJobMatchingUseCase : IHardcodeCvJobMatchingUseCase
    {
        private readonly ITHunterviewContext _context;

        public HardcodeCvJobMatchingUseCase(ITHunterviewContext context)
        {
            _context = context;
        }

        private string ExtractJsonField(string? jsonString, string fieldName)
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
            catch { }
            return string.Empty;
        }

        private decimal CalculateSkillsScore(string cvSkillsStr, string jobSkillsStr)
        {
            if (string.IsNullOrWhiteSpace(jobSkillsStr)) return 0.5m; // Neutral score if JD has no skills
            if (string.IsNullOrWhiteSpace(cvSkillsStr)) return 0m;

            var cvSkills = cvSkillsStr.ToLower().Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToHashSet();
            var jobSkills = jobSkillsStr.ToLower().Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToHashSet();

            if (jobSkills.Count == 0) return 0.5m;

            int matchCount = jobSkills.Count(j => cvSkills.Any(c => c.Contains(j) || j.Contains(c)));
            return (decimal)matchCount / jobSkills.Count;
        }

        private decimal CalculateTitleScore(string cvTitle, string jobTitle)
        {
            if (string.IsNullOrWhiteSpace(jobTitle)) return 0.5m;
            if (string.IsNullOrWhiteSpace(cvTitle)) return 0m;

            cvTitle = cvTitle.ToLower();
            jobTitle = jobTitle.ToLower();

            if (cvTitle.Contains(jobTitle) || jobTitle.Contains(cvTitle)) return 1.0m;

            var jobTokens = jobTitle.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int matchCount = jobTokens.Count(t => cvTitle.Contains(t));
            
            return jobTokens.Length > 0 ? (decimal)matchCount / jobTokens.Length : 0m;
        }

        private decimal CalculateExperienceScore(string cvExp, string jobExp)
        {
            if (string.IsNullOrWhiteSpace(jobExp)) return 0.5m;
            if (string.IsNullOrWhiteSpace(cvExp)) return 0m;

            var numRegex = new Regex(@"\d+");
            var cvMatch = numRegex.Match(cvExp);
            var jobMatch = numRegex.Match(jobExp);

            if (cvMatch.Success && jobMatch.Success)
            {
                int cvYears = int.Parse(cvMatch.Value);
                int jobYears = int.Parse(jobMatch.Value);
                if (cvYears >= jobYears) return 1.0m;
                return (decimal)cvYears / jobYears; // e.g. 2 years vs 3 years = 0.66
            }

            // Fallback to keyword matching
            cvExp = cvExp.ToLower();
            jobExp = jobExp.ToLower();
            if (jobExp.Contains("senior") && !cvExp.Contains("senior")) return 0.2m;
            if (jobExp.Contains("junior") && cvExp.Contains("senior")) return 1.0m;
            
            return 0.5m;
        }

        private decimal CalculateDomainScore(string cvDomain, string jobDomain)
        {
            if (string.IsNullOrWhiteSpace(jobDomain)) return 0.5m;
            if (string.IsNullOrWhiteSpace(cvDomain)) return 0m;

            cvDomain = cvDomain.ToLower();
            jobDomain = jobDomain.ToLower();

            if (cvDomain.Contains(jobDomain) || jobDomain.Contains(cvDomain)) return 1.0m;
            return 0.3m;
        }

        private async Task ProcessMatching(Cvs cv, JobPostings job, Guid userId)
        {
            var cvTitle = ExtractJsonField(cv.ParsedData, "job_title");
            var cvSkills = ExtractJsonField(cv.ParsedData, "skills");
            var cvExp = ExtractJsonField(cv.ParsedData, "experience");
            var cvDomain = ExtractJsonField(cv.ParsedData, "domain");
            if (string.IsNullOrEmpty(cvDomain)) cvDomain = ExtractJsonField(cv.ParsedData, "experience");

            var jobTitle = ExtractJsonField(job.ParsedData, "position.title");
            if (string.IsNullOrEmpty(jobTitle)) jobTitle = job.Title;
            var jobSkills = ExtractJsonField(job.ParsedData, "tech_requirements");
            if (string.IsNullOrEmpty(jobSkills)) jobSkills = job.Requirements;
            var jobExp = ExtractJsonField(job.ParsedData, "seniority_signals") + " " + ExtractJsonField(job.ParsedData, "engineering_expectations");
            var jobDomain = ExtractJsonField(job.ParsedData, "domain");
            if (string.IsNullOrEmpty(jobDomain)) jobDomain = job.Description;

            var titleScore = CalculateTitleScore(cvTitle, jobTitle);
            var skillsScore = CalculateSkillsScore(cvSkills, jobSkills);
            var expScore = CalculateExperienceScore(cvExp, jobExp);
            var domainScore = CalculateDomainScore(cvDomain, jobDomain);

            var finalScore = (titleScore * 0.15m) +
                             (skillsScore * 0.45m) +
                             (expScore * 0.30m) +
                             (domainScore * 0.10m);

            var details = JsonSerializer.Serialize(new 
            {
                Method = "Hardcode",
                TitleScore = Math.Round(titleScore, 4),
                SkillsScore = Math.Round(skillsScore, 4),
                ExperienceScore = Math.Round(expScore, 4),
                DomainScore = Math.Round(domainScore, 4),
                FinalScore = Math.Round(finalScore, 4),
                Weights = new { TitleWeight = 0.15m, SkillsWeight = 0.45m, ExperienceWeight = 0.30m, DomainWeight = 0.10m }
            });

            var existingScore = await _context.CvJobMatchScores
                .FirstOrDefaultAsync(s => s.CvId == cv.Id && s.JobId == job.Id && s.UserId == userId);

            if (existingScore != null)
            {
                existingScore.MatchScore = finalScore;
                existingScore.UpdatedAt = DateTime.UtcNow;
                existingScore.MatchDetails = details;
                existingScore.MatchType = "Hardcode";
            }
            else
            {
                _context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CvId = cv.Id,
                    JobId = job.Id,
                    RawJdText = job.Title,
                    MatchScore = finalScore,
                    MatchDetails = details,
                    MatchType = "Hardcode",
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        public async Task MatchCvWithAllJobsHardcodeAsync(Guid cvId, Guid userId)
        {
            var cv = await _context.Cvs.FindAsync(cvId);
            if (cv == null) throw new Exception("CV not found");

            var jobs = await _context.JobPostings.Where(j => j.Status == ITHunterview.Domain.Enums.JobStatus.PUBLISHED).ToListAsync();
            foreach (var job in jobs)
            {
                await ProcessMatching(cv, job, userId);
            }

            await _context.SaveChangesAsync();
        }

        public async Task MatchJobWithAllCvsHardcodeAsync(Guid jobId, Guid userId)
        {
            var job = await _context.JobPostings.FindAsync(jobId);
            if (job == null) throw new Exception("Job not found");

            var cvs = await _context.Cvs.Where(c => c.IsPrimary).ToListAsync();
            foreach (var cv in cvs)
            {
                await ProcessMatching(cv, job, userId);
            }

            await _context.SaveChangesAsync();
        }
    }
}
