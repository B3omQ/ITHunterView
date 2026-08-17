using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.UseCase;

public sealed class RecruiterCvScanUseCase : IRecruiterCvScanUseCase
{
    private const int UnlockCost = 50;

    private readonly ITHunterviewContext _context;
    private readonly IRecruiterCvScanRepository _scanRepository;
    private readonly IHardcodeCvJobPairMatcher _pairMatcher;
    private readonly ICvAnalysisResponseValidator _cvAnalysisValidator;

    public RecruiterCvScanUseCase(
        ITHunterviewContext context,
        IRecruiterCvScanRepository scanRepository,
        IHardcodeCvJobPairMatcher pairMatcher,
        ICvAnalysisResponseValidator cvAnalysisValidator)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scanRepository);
        ArgumentNullException.ThrowIfNull(pairMatcher);
        ArgumentNullException.ThrowIfNull(cvAnalysisValidator);
        _context = context;
        _scanRepository = scanRepository;
        _pairMatcher = pairMatcher;
        _cvAnalysisValidator = cvAnalysisValidator;
    }

    public async Task<RecruiterCvScanRunDto> ScanAsync(Guid recruiterUserId, Guid jobId, CancellationToken ct)
    {
        var scope = await ResolveOwnedScopeAsync(recruiterUserId, jobId, requireScannableJob: true, ct);
        var createdAt = DateTime.UtcNow;
        var run = await _scanRepository.CreatePendingAsync(new RecruiterCvScanRun
        {
            Id = Guid.NewGuid(),
            RecruiterUserId = recruiterUserId,
            RecruiterProfileId = scope.RecruiterProfileId,
            CompanyId = scope.CompanyId,
            JobId = scope.Job.Id,
            JobTitleSnapshot = scope.Job.Title,
            Status = MatchingScanRunStatus.Pending,
            CreatedAt = createdAt
        }, ct);

        try
        {
            if (!await _scanRepository.TryStartAsync(run.Id, DateTime.UtcNow, ct))
            {
                throw new InvalidOperationException("The new recruiter scan run could not be started.");
            }

            var eligibleCvs = await GetEligibleCvsAsync(ct);
            var matched = new List<(Cvs Cv, HardcodePairMatchResult Result)>();
            foreach (var cv in eligibleCvs)
            {
                ct.ThrowIfCancellationRequested();
                var match = await _pairMatcher.MatchAsync(cv, scope.Job, ct);
                matched.Add((cv, match));
            }

            var results = matched
                .OrderByDescending(pair => pair.Result.MatchScore.HasValue)
                .ThenByDescending(pair => pair.Result.MatchScore)
                .ThenBy(pair => pair.Cv.Id)
                .Select((pair, index) => new RecruiterCvScanResult
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    CvId = pair.Cv.Id,
                    CandidateUserId = pair.Cv.UserId,
                    MatchScore = pair.Result.MatchScore,
                    MatchDetails = pair.Result.MatchDetails,
                    CvAnalysisQuality = pair.Result.CvAnalysisQuality,
                    CvAnalysisCoverageJson = pair.Result.CvAnalysisCoverageJson,
                    CvAnalysisDiagnosticsJson = pair.Result.CvAnalysisDiagnosticsJson,
                    Rank = index + 1
                })
                .ToArray();

            var completedAt = DateTime.UtcNow;
            await _scanRepository.CompleteAsync(run.Id, results, completedAt, ct);
            return new RecruiterCvScanRunDto
            {
                RunId = run.Id,
                JobId = run.JobId,
                Status = MatchingScanRunStatus.Completed.ToString(),
                CreatedAt = run.CreatedAt,
                CompletedAt = completedAt
            };
        }
        catch
        {
            await _scanRepository.FailAsync(
                run.Id,
                "RECRUITER_SCAN_FAILED",
                "Recruiter CV scan failed.",
                DateTime.UtcNow,
                CancellationToken.None);
            throw;
        }
    }

    public async Task<PagedResult<RecruiterCvScanResultDto>> GetLatestSuccessfulAsync(
        Guid recruiterUserId,
        Guid jobId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var scope = await ResolveOwnedScopeAsync(recruiterUserId, jobId, requireScannableJob: false, ct);
        var latest = await _scanRepository.GetLatestCompletedAsync(
            recruiterUserId,
            scope.CompanyId,
            jobId,
            ct);
        if (latest is null)
        {
            return new PagedResult<RecruiterCvScanResultDto>
            {
                Items = [], TotalCount = 0, Page = page, PageSize = pageSize
            };
        }

        var (items, totalCount) = await _scanRepository.GetResultPageAsync(
            latest.Id,
            (page - 1) * pageSize,
            pageSize,
            ct);
        var cvIds = items.Select(item => item.CvId).ToArray();
        var unlockedCvIds = await _context.RecruiterUnlockedCvs
            .AsNoTracking()
            .Where(unlock =>
                unlock.RecruiterId == recruiterUserId &&
                unlock.Status == RecruiterCvUnlockStatus.Completed &&
                cvIds.Contains(unlock.CvId))
            .Select(unlock => unlock.CvId)
            .ToListAsync(ct);
        var unlocked = unlockedCvIds.ToHashSet();

        return new PagedResult<RecruiterCvScanResultDto>
        {
            Items = items.Select(item => new RecruiterCvScanResultDto
            {
                ScanResultId = item.Id,
                AnonymousLabel = $"Candidate #{item.Rank}",
                Rank = item.Rank,
                MatchScore = item.MatchScore,
                MatchDetails = item.MatchDetails,
                IsUnlocked = unlocked.Contains(item.CvId),
                UnlockCost = UnlockCost
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task<(Guid RecruiterProfileId, Guid CompanyId, JobPostings Job)> ResolveOwnedScopeAsync(
        Guid recruiterUserId,
        Guid jobId,
        bool requireScannableJob,
        CancellationToken ct)
    {
        var profile = await _context.RecruiterProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == recruiterUserId, ct);
        if (profile?.CompanyId is null)
        {
            throw new UnauthorizedAccessException("Recruiter profile and company are required.");
        }

        var job = await _context.JobPostings
            .SingleOrDefaultAsync(item =>
                item.Id == jobId &&
                item.RecruiterId == profile.Id &&
                item.CompanyId == profile.CompanyId &&
                item.DeletedAt == null &&
                (!requireScannableJob || item.Status == JobStatus.PUBLISHED),
                ct);
        if (job is null)
        {
            throw new UnauthorizedAccessException("The recruiter does not own a scannable job in this company.");
        }

        return (profile.Id, profile.CompanyId.Value, job);
    }

    private async Task<IReadOnlyList<Cvs>> GetEligibleCvsAsync(CancellationToken ct)
    {
        var candidates = await (
            from cv in _context.Cvs
            join profile in _context.CandidateProfiles on cv.UserId equals profile.UserId
            where cv.DeletedAt == null &&
                  cv.IsPrimary &&
                  profile.IsVisibleToRecruiters &&
                  cv.ParseStatus == "SUCCESS" &&
                  !string.IsNullOrWhiteSpace(cv.ParsedData)
            select cv)
            .ToListAsync(ct);

        return candidates
            .Where(cv => _cvAnalysisValidator.ValidateStoredCanonical(cv.ParsedData).IsUsable)
            .ToArray();
    }
}
