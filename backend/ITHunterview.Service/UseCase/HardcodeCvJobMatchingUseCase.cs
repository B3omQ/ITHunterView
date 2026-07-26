using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.UseCase
{
    public class HardcodeCvJobMatchingUseCase : IHardcodeCvJobMatchingUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly ICvTextExtractorService _cvTextExtractorService;
        private readonly ILogger<HardcodeCvJobMatchingUseCase> _logger;

        public HardcodeCvJobMatchingUseCase(
            ITHunterviewContext context,
            ICvTextExtractorService cvTextExtractorService,
            ILogger<HardcodeCvJobMatchingUseCase> logger)
        {
            _context = context;
            _cvTextExtractorService = cvTextExtractorService;
            _logger = logger;
        }

        private JsonElement? GetJsonElement(string? jsonString, string fieldName)
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

        private List<string> ExtractJsonArray(string? jsonString, string fieldName)
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

        private int ExtractJsonInt(string? jsonString, string fieldName)
        {
            var element = GetJsonElement(jsonString, fieldName);
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Number)
            {
                if (element.Value.TryGetInt32(out int val))
                    return val;
            }
            return 0;
        }

        private decimal CalculateSkillsScore(List<string> cvSkills, List<string> jobSkills)
        {
            if (jobSkills == null || jobSkills.Count == 0) return 0.5m;
            if (cvSkills == null || cvSkills.Count == 0) return 0m;

            var cvSet = cvSkills.Select(s => s.ToLower()).ToHashSet();
            var jobSet = jobSkills.Select(s => s.ToLower()).ToHashSet();

            int matchCount = jobSet.Count(j => cvSet.Contains(j));
            return (decimal)matchCount / jobSet.Count;
        }

        private decimal CalculateTitleScore(List<string> cvTitles, List<string> jobTitles)
        {
            if (jobTitles == null || jobTitles.Count == 0) return 0.5m;
            if (cvTitles == null || cvTitles.Count == 0) return 0m;

            var cvSet = cvTitles.Select(s => s.ToLower()).ToHashSet();
            var jobSet = jobTitles.Select(s => s.ToLower()).ToHashSet();

            if (jobSet.Any(j => cvSet.Contains(j))) return 1.0m;
            return 0m;
        }

        private decimal CalculateExperienceScore(int cvYears, int jobYears)
        {
            if (jobYears <= 0) return 0.5m;
            if (cvYears >= jobYears) return 1.0m;
            return (decimal)cvYears / jobYears;
        }

        private decimal CalculateDomainScore(List<string> cvDomains, List<string> jobDomains)
        {
            if (jobDomains == null || jobDomains.Count == 0) return 0.5m;
            if (cvDomains == null || cvDomains.Count == 0) return 0m;

            var cvSet = cvDomains.Select(s => s.ToLower()).ToHashSet();
            var jobSet = jobDomains.Select(s => s.ToLower()).ToHashSet();

            int matchCount = jobSet.Count(j => cvSet.Contains(j));
            if (matchCount > 0) return 1.0m;
            return 0.3m;
        }

        private class ParsedMetrics
        {
            public List<string> Titles { get; set; } = new();
            public List<string> Skills { get; set; } = new();
            public int Exp { get; set; }
            public List<string> Domains { get; set; } = new();
        }

        private ParsedMetrics ExtractMetrics(string? parsedData)
        {
            return new ParsedMetrics
            {
                Titles = ExtractJsonArray(parsedData, "matching_metrics.job_titles_normalized"),
                Skills = ExtractJsonArray(parsedData, "matching_metrics.skills_normalized"),
                Exp = ExtractJsonInt(parsedData, "matching_metrics.total_years_exp"),
                Domains = ExtractJsonArray(parsedData, "matching_metrics.domains")
            };
        }

        private void ProcessMatching(Cvs cv, ParsedMetrics cvMetrics, JobPostings job, ParsedMetrics jobMetrics, Guid userId, CvJobMatchScores? existingScore)
        {
            if (existingScore != null && existingScore.Status != "Pending")
            {
                return; // Do not rescan or overwrite
            }

            if (existingScore != null && existingScore.Status != "Pending")
            {
                return; // Do not rescan or overwrite
            }

            var titleScore = CalculateTitleScore(cvMetrics.Titles, jobMetrics.Titles);
            var skillsScore = CalculateSkillsScore(cvMetrics.Skills, jobMetrics.Skills);
            var expScore = CalculateExperienceScore(cvMetrics.Exp, jobMetrics.Exp);
            var domainScore = CalculateDomainScore(cvMetrics.Domains, jobMetrics.Domains);

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

            if (existingScore != null)
            {
                existingScore.MatchScore = finalScore;
                existingScore.UpdatedAt = DateTime.UtcNow;
                existingScore.MatchDetails = details;
                existingScore.Status = "Completed";
                existingScore.MatchType = "Hardcode";
            }
            else
            {
                _context.CvJobMatchScores.Add(new CvJobMatchScores
                {
                    UserId = cv.UserId, // Fix Wrong ID Bug: Lưu ID của Candidate
                    CvId = cv.Id,
                    JobId = job.Id,
                    RawJdText = job.Title,
                    MatchScore = finalScore,
                    MatchDetails = details,
                    MatchType = "Hardcode",
                    Status = "Completed",
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task EnsureCvIsParsedAsync(Cvs cv)
        {
            if (string.IsNullOrWhiteSpace(cv.ParsedData) || cv.ParseStatus != "SUCCESS")
            {
                _logger.LogInformation("[INFO] On-demand parsing CV {CvId} in Hardcode Matching.", cv.Id);
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

        public async Task MatchCvWithAllJobsHardcodeAsync(Guid cvId, Guid userId)
        {
            var cv = await _context.Cvs.FindAsync(cvId);
            if (cv == null) throw new Exception("CV not found");
            
            await EnsureCvIsParsedAsync(cv);

            var cvMetrics = ExtractMetrics(cv.ParsedData);

            var existingScores = await _context.CvJobMatchScores
                .Where(s => s.CvId == cvId && s.UserId == userId)
                .ToDictionaryAsync(s => s.JobId);

            var jobs = await _context.JobPostings.AsNoTracking().Where(j => j.Status == ITHunterview.Domain.Enums.JobStatus.PUBLISHED).ToListAsync();
            
            foreach (var job in jobs)
            {
                if (job.ParseStatus != "SUCCESS") continue; // Skip unparsed jobs to avoid inaccurate 0% matches

                existingScores.TryGetValue(job.Id, out var existingScore);
                var jobMetrics = ExtractMetrics(job.ParsedData);
                ProcessMatching(cv, cvMetrics, job, jobMetrics, userId, existingScore);
            }

            await _context.SaveChangesAsync();
        }

        public async Task MatchJobWithAllCvsHardcodeAsync(Guid jobId, Guid userId)
        {
            var job = await _context.JobPostings.FindAsync(jobId);
            if (job == null) throw new Exception("Job not found");
            if (job.ParseStatus != "SUCCESS") throw new Exception($"Job posting is currently in status '{job.ParseStatus ?? "PENDING"}'. AI analysis must complete before matching.");

            var jobMetrics = ExtractMetrics(job.ParsedData);

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
                         && c.ParseStatus == "SUCCESS")
                .ToListAsync();
            
            foreach (var cv in cvs)
            {
                if (cv.ParseStatus != "SUCCESS") continue; // Skip unparsed CVs to avoid inaccurate 0% matches

                existingScores.TryGetValue(cv.Id, out var existingScore);
                var cvMetrics = ExtractMetrics(cv.ParsedData);
                ProcessMatching(cv, cvMetrics, job, jobMetrics, userId, existingScore);
            }

            await _context.SaveChangesAsync();
        }
    }
}
