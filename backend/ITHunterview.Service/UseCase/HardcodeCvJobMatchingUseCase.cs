using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.UseCase;

public class HardcodeCvJobMatchingUseCase : IHardcodeCvJobMatchingUseCase
{
    private readonly ITHunterviewContext _context;
    private readonly ILogger<HardcodeCvJobMatchingUseCase> _logger;
    private readonly HardcodeCvJobPairMatcher _pairMatcher;

    public HardcodeCvJobMatchingUseCase(
        ITHunterviewContext context,
        ICvTextExtractorService cvTextExtractorService,
        ILogger<HardcodeCvJobMatchingUseCase> logger,
        HardcodeJdRequirementScoringService hardcodeJdRequirementScoringService,
        ICvAnalysisResponseValidator cvAnalysisResponseValidator)
    {
        _context = context;
        _logger = logger;
        _pairMatcher = new HardcodeCvJobPairMatcher(
            context,
            cvTextExtractorService,
            new ForwardingLogger<HardcodeCvJobPairMatcher>(logger),
            hardcodeJdRequirementScoringService,
            cvAnalysisResponseValidator);
    }

    public async Task MatchCvWithAllJobsHardcodeAsync(Guid cvId, Guid userId)
    {
        var cv = await _context.Cvs.FindAsync(cvId);
        if (cv == null) throw new Exception("CV not found");

        await _pairMatcher.PrepareCvAsync(cv);

        var existingScores = await _context.CvJobMatchScores
            // Saved-CV/pasted-JD history intentionally has no JobId and cannot
            // correspond to any published job in this bulk matching pass.
            .Where(s => s.CvId == cvId && s.UserId == userId && s.JobId.HasValue)
            .ToDictionaryAsync(s => s.JobId!.Value);

        var jobs = await _context.JobPostings.AsNoTracking()
            .Where(j => j.Status == JobStatus.PUBLISHED)
            .ToListAsync();

        foreach (var job in jobs)
        {
            if (job.ParseStatus != "SUCCESS") continue; // Skip unparsed jobs to avoid inaccurate 0% matches

            await _pairMatcher.PrepareJobAsync(job);
            existingScores.TryGetValue(job.Id, out var existingScore);
            if (existingScore != null && existingScore.Status != "Pending")
            {
                continue; // Do not rescan or overwrite
            }

            var result = await _pairMatcher.MatchAsync(cv, job);
            ApplyResult(cv, job, existingScore, result);
        }

        await _context.SaveChangesAsync();
    }

    public async Task MatchJobWithAllCvsHardcodeAsync(Guid jobId, Guid userId)
    {
        var job = await _context.JobPostings.FindAsync(jobId);
        if (job == null) throw new Exception("Job not found");
        if (job.ParseStatus != "SUCCESS") throw new Exception($"Job posting is currently in status '{job.ParseStatus ?? "PENDING"}'. AI analysis must complete before matching.");

        await _pairMatcher.PrepareJobAsync(job);

        var existingScores = await _context.CvJobMatchScores
            // Saved-JD/pasted-CV history intentionally has no CvId and cannot
            // correspond to any saved CV in this bulk matching pass.
            .Where(s => s.JobId == jobId && s.CvId.HasValue) // Do not filter by recruiter UserId.
            .ToDictionaryAsync(s => s.CvId!.Value);

        var cvs = await _context.Cvs
            .Include(c => c.User)
            .ThenInclude(u => u.CandidateProfile)
            .Where(c => c.IsPrimary
                     && c.User.CandidateProfile != null
                     && c.User.CandidateProfile.IsVisibleToRecruiters == true // Fix Privacy Bug
                     && c.ParseStatus == "SUCCESS")
            .ToListAsync();

        foreach (var cv in cvs)
        {
            if (cv.ParseStatus != "SUCCESS") continue; // Skip unparsed CVs to avoid inaccurate 0% matches

            try
            {
                await _pairMatcher.PrepareCvAsync(cv);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Skipping CV {CvId} because its analysis is unusable. FailureType={FailureType}",
                    cv.Id,
                    ex.GetType().Name);
                continue;
            }

            if (existingScores.TryGetValue(cv.Id, out var existingScore) &&
                existingScore.Status != "Pending")
            {
                continue; // Do not rescan or overwrite
            }

            var result = await _pairMatcher.MatchAsync(cv, job);
            ApplyResult(cv, job, existingScore, result);
        }

        await _context.SaveChangesAsync();
    }

    private void ApplyResult(
        Cvs cv,
        JobPostings job,
        CvJobMatchScores? existingScore,
        HardcodePairMatchResult result)
    {
        var target = existingScore ?? new CvJobMatchScores
        {
            UserId = cv.UserId,
            CvId = cv.Id,
            JobId = job.Id,
            RawJdText = job.Title
        };
        target.MatchScore = result.MatchScore;
        target.MatchDetails = result.MatchDetails;
        target.Status = "Completed";
        if (result.MatchScore.HasValue || existingScore == null)
        {
            target.MatchType = "Hardcode";
        }
        target.UpdatedAt = DateTime.UtcNow;
        if (!result.MatchScore.HasValue)
        {
            target.ErrorCode = null;
            target.ErrorMessage = null;
        }
        ApplyCvAnalysisMetadata(target, result);
        if (existingScore == null)
        {
            _context.CvJobMatchScores.Add(target);
        }
    }

    private static void ApplyCvAnalysisMetadata(
        CvJobMatchScores score,
        HardcodePairMatchResult result)
    {
        score.CvAnalysisQuality = result.CvAnalysisQuality;
        score.CvAnalysisCoverageJson = result.CvAnalysisCoverageJson;
        score.CvAnalysisDiagnosticsJson = result.CvAnalysisDiagnosticsJson;
    }

    private sealed class ForwardingLogger<T> : ILogger<T>
    {
        private readonly ILogger _inner;

        public ForwardingLogger(ILogger inner)
        {
            _inner = inner;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            _inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
