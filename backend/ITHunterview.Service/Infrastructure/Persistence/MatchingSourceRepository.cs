using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence;

public sealed class MatchingSourceRepository : IMatchingSourceRepository
{
    private readonly ITHunterviewContext _context;

    public MatchingSourceRepository(ITHunterviewContext context)
    {
        _context = context;
    }

    public Task<Cvs?> GetOwnedCvAsync(Guid cvId, Guid userId, CancellationToken ct = default)
    {
        return _context.Cvs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                cv => cv.Id == cvId && cv.UserId == userId && cv.DeletedAt == null,
                ct);
    }

    public Task<Cvs?> GetOwnedCvForUpdateAsync(Guid cvId, Guid userId, CancellationToken ct = default)
    {
        return _context.Cvs
            .FirstOrDefaultAsync(
                cv => cv.Id == cvId && cv.UserId == userId && cv.DeletedAt == null,
                ct);
    }

    public Task<JobPostings?> GetAccessiblePublishedJobAsync(
        Guid jobId,
        DateTime utcNow,
        CancellationToken ct = default)
    {
        return _context.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                job => job.Id == jobId
                    && job.DeletedAt == null
                    && job.Status == JobStatus.PUBLISHED
                    && !job.IsBanned
                    && (!job.ExpiresAt.HasValue || job.ExpiresAt.Value >= utcNow),
                ct);
    }

    public Task<JobPostings?> GetAccessibleJobAsync(
        Guid jobId,
        Guid candidateId,
        DateTime utcNow,
        CancellationToken ct = default)
    {
        return _context.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                job => job.Id == jobId
                    && job.DeletedAt == null
                    && (
                        (job.Status == JobStatus.PUBLISHED
                         && !job.IsBanned
                         && (!job.ExpiresAt.HasValue || job.ExpiresAt.Value >= utcNow))
                        || _context.UserSavedJobs.Any(saved =>
                            saved.UserId == candidateId && saved.JobId == jobId)
                    ),
                ct);
    }
}
