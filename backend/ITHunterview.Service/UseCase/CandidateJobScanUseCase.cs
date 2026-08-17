using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase;

/// <summary>
/// Orchestrates the free, hardcode-only Candidate scan product.  It deliberately
/// depends only on Candidate scan persistence and the pair matcher: one-to-one
/// result, billing, Save, Apply, and notification workflows are not collaborators.
/// </summary>
public sealed class CandidateJobScanUseCase : ICandidateJobScanUseCase
{
    private readonly ICandidateJobScanRepository _scanRepository;
    private readonly ICvRepository _cvRepository;
    private readonly IJobPostingRepository _jobPostingRepository;
    private readonly IHardcodeCvJobPairMatcher _pairMatcher;
    private readonly ICandidateJobScanQueue _queue;

    public CandidateJobScanUseCase(
        ICandidateJobScanRepository scanRepository,
        ICvRepository cvRepository,
        IJobPostingRepository jobPostingRepository,
        IHardcodeCvJobPairMatcher pairMatcher,
        ICandidateJobScanQueue queue)
    {
        ArgumentNullException.ThrowIfNull(scanRepository);
        ArgumentNullException.ThrowIfNull(cvRepository);
        ArgumentNullException.ThrowIfNull(jobPostingRepository);
        ArgumentNullException.ThrowIfNull(pairMatcher);
        ArgumentNullException.ThrowIfNull(queue);
        _scanRepository = scanRepository;
        _cvRepository = cvRepository;
        _jobPostingRepository = jobPostingRepository;
        _pairMatcher = pairMatcher;
        _queue = queue;
    }

    public async Task<CandidateJobScanAcceptedDto> CreateRunAsync(
        Guid candidateUserId,
        Guid cvId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var cv = await _cvRepository.GetByIdAsync(cvId);
        if (cv is null || cv.UserId != candidateUserId || cv.DeletedAt is not null)
        {
            throw new KeyNotFoundException("CV not found");
        }

        var run = new CandidateJobScanRun
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateUserId,
            CvId = cv.Id,
            CvFileNameSnapshot = cv.FileName ?? string.Empty,
            Status = MatchingScanRunStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _scanRepository.CreatePendingAsync(run, ct);
        try
        {
            await _queue.EnqueueAsync(new CandidateJobScanRequest(run.Id, candidateUserId, cv.Id), ct);
        }
        catch
        {
            await _scanRepository.FailAsync(run.Id, "CANDIDATE_SCAN_QUEUE_FAILED", "Candidate scan could not be queued.", DateTime.UtcNow, CancellationToken.None);
            throw;
        }
        return new CandidateJobScanAcceptedDto(run.Id, MatchingScanRunStatus.Pending.ToString());
    }

    public async Task ProcessRunAsync(Guid runId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var run = await _scanRepository.GetPendingOrProcessingByIdAsync(runId, ct);
        if (run is null || !await _scanRepository.TryStartAsync(runId, DateTime.UtcNow, ct))
        {
            return;
        }

        try
        {
            var cv = await _cvRepository.GetByIdAsync(run.CvId);
            if (cv is null || cv.UserId != run.CandidateUserId || cv.DeletedAt is not null)
            {
                throw new KeyNotFoundException("CV not found");
            }

            var jobs = _jobPostingRepository.GetQueryable()
                .Where(job => job.Status == JobStatus.PUBLISHED &&
                              !job.IsBanned &&
                              job.DeletedAt == null &&
                              job.ParseStatus == "SUCCESS")
                .ToList();

            var matches = new List<(JobPostings Job, HardcodePairMatchResult Match)>();
            foreach (var job in jobs)
            {
                ct.ThrowIfCancellationRequested();
                matches.Add((job, await _pairMatcher.MatchAsync(cv, job, ct)));
            }

            var results = matches
                .OrderByDescending(match => match.Match.MatchScore.HasValue)
                .ThenByDescending(match => match.Match.MatchScore)
                .ThenBy(match => match.Job.Title)
                .ThenBy(match => match.Job.Id)
                .Select((match, index) => new CandidateJobScanResult
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    JobId = match.Job.Id,
                    JobTitleSnapshot = match.Job.Title,
                    MatchScore = match.Match.MatchScore,
                    MatchDetails = match.Match.MatchDetails,
                    CvAnalysisQuality = match.Match.CvAnalysisQuality,
                    CvAnalysisCoverageJson = match.Match.CvAnalysisCoverageJson,
                    CvAnalysisDiagnosticsJson = match.Match.CvAnalysisDiagnosticsJson,
                    Rank = index + 1
                })
                .ToList();

            await _scanRepository.CompleteAsync(run.Id, results, DateTime.UtcNow, ct);
        }
        catch (Exception exception)
        {
            await _scanRepository.FailAsync(
                run.Id,
                "CANDIDATE_SCAN_FAILED",
                exception.Message,
                DateTime.UtcNow,
                CancellationToken.None);
            throw;
        }
    }

    public async Task<PagedResult<CandidateJobScanResultDto>> GetLatestSuccessfulAsync(
        Guid candidateUserId,
        Guid cvId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var run = await _scanRepository.GetLatestCompletedAsync(candidateUserId, cvId, ct);
        if (run is null)
        {
            return new PagedResult<CandidateJobScanResultDto>
            {
                Items = [], TotalCount = 0, Page = page, PageSize = pageSize, Total = 0, TotalItems = 0
            };
        }

        var (items, totalCount) = await _scanRepository.GetResultPageAsync(run.Id, (page - 1) * pageSize, pageSize, ct);
        return new PagedResult<CandidateJobScanResultDto>
        {
            Items = items.Select(item => new CandidateJobScanResultDto(
                item.Id, item.RunId, item.JobId, item.JobTitleSnapshot, item.MatchScore,
                item.MatchDetails, item.CvAnalysisQuality, item.CvAnalysisCoverageJson,
                item.CvAnalysisDiagnosticsJson, item.Rank)).ToList(),
            TotalCount = totalCount,
            Total = totalCount,
            TotalItems = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
