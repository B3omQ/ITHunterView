using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence;

public sealed class MatchingSourceAnalysisPersistence : IMatchingSourceAnalysisPersistence
{
    private const string SuccessStatus = "SUCCESS";
    private const string RawFallbackStatus = "RAW_FALLBACK";
    private readonly ITHunterviewContext _context;
    private readonly IJobAnalysisInputBuilder _inputBuilder;

    public MatchingSourceAnalysisPersistence(
        ITHunterviewContext context,
        IJobAnalysisInputBuilder inputBuilder)
    {
        _context = context;
        _inputBuilder = inputBuilder;
    }

    public async Task<MatchingSourcePersistenceOutcome> TryPersistCvAsync(
        CvAnalysisPersistenceIntent intent,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        return await ExecuteInTransactionAsync(
            () => TryPersistCvCoreAsync(intent, ct),
            ct);
    }

    private async Task<MatchingSourcePersistenceOutcome> TryPersistCvCoreAsync(
        CvAnalysisPersistenceIntent intent,
        CancellationToken ct)
    {
        var cv = await LoadCvForUpdateAsync(intent.CvId, intent.OwnerId, ct);
        if (cv is null)
        {
            return MatchingSourcePersistenceOutcome.SourceMissing;
        }

        if (!string.Equals(
                MatchingSourceFingerprint.ForCv(cv.FileUrl, cv.RawText),
                intent.ExpectedSourceHash,
                StringComparison.Ordinal))
        {
            return MatchingSourcePersistenceOutcome.SourceChanged;
        }

        if (!string.Equals(
                MatchingSourceFingerprint.ForAnalysis(cv.ParsedData),
                intent.ExpectedAnalysisHash,
                StringComparison.Ordinal))
        {
            return MatchingSourcePersistenceOutcome.AnalysisChanged;
        }

        cv.ParsedData = intent.CanonicalJson;
        cv.ParseStatus = SuccessStatus;
        cv.ParseError = null;
        cv.AnalysisQuality = intent.Quality;
        cv.AnalysisCoverageJson = intent.CoverageJson;
        cv.AnalysisDiagnosticsJson = intent.DiagnosticsJson;
        ClearCvEmbeddings(cv);
        cv.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return MatchingSourcePersistenceOutcome.Persisted;
    }

    public async Task<MatchingSourcePersistenceOutcome> TryPersistJdAsync(
        JdAnalysisPersistenceIntent intent,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        return await ExecuteInTransactionAsync(
            () => TryPersistJdCoreAsync(intent, ct),
            ct);
    }

    private async Task<MatchingSourcePersistenceOutcome> TryPersistJdCoreAsync(
        JdAnalysisPersistenceIntent intent,
        CancellationToken ct)
    {
        var job = await LoadJobForUpdateAsync(intent.JobId, ct);
        if (job is null)
        {
            return MatchingSourcePersistenceOutcome.SourceMissing;
        }

        if (job.ActiveAnalysisRunId.HasValue && await IsActiveRunAsync(job.ActiveAnalysisRunId.Value, ct))
        {
            return MatchingSourcePersistenceOutcome.ActiveAnalysisInProgress;
        }

        if (!string.Equals(
                MatchingSourceFingerprint.ForJd(_inputBuilder.Build(job), _inputBuilder),
                intent.ExpectedSourceHash,
                StringComparison.Ordinal) ||
            job.AnalysisRevision != intent.ExpectedRevision)
        {
            return MatchingSourcePersistenceOutcome.SourceChanged;
        }

        if (!string.Equals(
                MatchingSourceFingerprint.ForAnalysis(job.ParsedData),
                intent.ExpectedAnalysisHash,
                StringComparison.Ordinal))
        {
            return MatchingSourcePersistenceOutcome.AnalysisChanged;
        }

        if (intent.Quality is JdAnalysisQuality.COMPLETE or JdAnalysisQuality.PARTIAL)
        {
            if (string.IsNullOrWhiteSpace(intent.CanonicalJson))
            {
                throw new ArgumentException("JD canonical analysis is required for a structured persistence intent.", nameof(intent));
            }

            job.ParsedData = intent.CanonicalJson;
            job.ParseStatus = SuccessStatus;
            job.ParseError = null;
        }
        else
        {
            job.ParsedData = null;
            job.ParseStatus = RawFallbackStatus;
            job.ParseError = BoundFailureCode(intent.FailureCode);
        }

        job.EffectiveAnalysisRevision = job.AnalysisRevision;
        job.EffectiveAnalysisRunId = null;
        ClearJobEmbeddings(job);
        job.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return MatchingSourcePersistenceOutcome.Persisted;
    }

    private async Task<Cvs?> LoadCvForUpdateAsync(Guid cvId, Guid ownerId, CancellationToken ct)
    {
        if (IsInMemoryProvider())
        {
            return await _context.Cvs.SingleOrDefaultAsync(
                cv => cv.Id == cvId && cv.UserId == ownerId && cv.DeletedAt == null,
                ct);
        }

        return await _context.Cvs
            .FromSqlInterpolated($"SELECT * FROM cvs WHERE id = {cvId} AND user_id = {ownerId} AND deleted_at IS NULL FOR UPDATE")
            .SingleOrDefaultAsync(ct);
    }

    private async Task<JobPostings?> LoadJobForUpdateAsync(Guid jobId, CancellationToken ct)
    {
        if (IsInMemoryProvider())
        {
            return await _context.JobPostings.SingleOrDefaultAsync(job => job.Id == jobId && job.DeletedAt == null, ct);
        }

        return await _context.JobPostings
            .FromSqlInterpolated($"SELECT * FROM job_postings WHERE id = {jobId} AND deleted_at IS NULL FOR UPDATE")
            .SingleOrDefaultAsync(ct);
    }

    private async Task<bool> IsActiveRunAsync(Guid runId, CancellationToken ct)
    {
        var status = await _context.JobAnalysisRuns
            .Where(run => run.Id == runId)
            .Select(run => (JobAnalysisStatus?)run.Status)
            .SingleOrDefaultAsync(ct);
        return status is JobAnalysisStatus.PENDING or JobAnalysisStatus.PROCESSING;
    }

    private bool IsInMemoryProvider() =>
        string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);

    /// <summary>
    /// A FOR UPDATE lock has to survive the source/hash comparison until the
    /// write is committed. PostgreSQL releases a lock at statement end when no
    /// explicit transaction exists, so do not rely on SaveChanges' later
    /// implicit transaction here.
    /// </summary>
    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        if (IsInMemoryProvider() || _context.Database.CurrentTransaction is not null)
        {
            return await action();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static string? BoundFailureCode(string? failureCode)
    {
        var value = failureCode?.Trim();
        return string.IsNullOrWhiteSpace(value) ? "INVALID_JD_ANALYSIS" : value[..Math.Min(value.Length, 200)];
    }

    private static void ClearCvEmbeddings(Cvs cv)
    {
        cv.TitleEmbedding = null;
        cv.SkillsEmbedding = null;
        cv.ExperienceEmbedding = null;
        cv.DomainEmbedding = null;
    }

    private static void ClearJobEmbeddings(JobPostings job)
    {
        job.TitleEmbedding = null;
        job.SkillsEmbedding = null;
        job.ExperienceEmbedding = null;
        job.DomainEmbedding = null;
    }
}
