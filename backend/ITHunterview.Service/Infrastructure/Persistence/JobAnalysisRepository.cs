using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class JobAnalysisRepository : IJobAnalysisRepository
    {
        private readonly ITHunterviewContext _context;
        private readonly ICandidateFeatureUsageUseCase _featureUsageUseCase;

        public JobAnalysisRepository(
            ITHunterviewContext context,
            ICandidateFeatureUsageUseCase featureUsageUseCase)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _featureUsageUseCase = featureUsageUseCase ?? throw new ArgumentNullException(nameof(featureUsageUseCase));
        }

        public async Task<JobAnalysisRequestContext?> GetRequestContextAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default)
        {
            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null || job.RecruiterId != recruiterId) return null;

            var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == job.CompanyId, ct);
            return new JobAnalysisRequestContext
            {
                Job = job,
                IsCompanyVerified = company?.Status == CompanyStatus.VERIFIED
            };
        }

        public async Task<JobAnalysisRuns?> GetRunAsync(Guid runId, CancellationToken ct = default)
        {
            return await _context.JobAnalysisRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == runId, ct);
        }

        public async Task<JobAnalysisRuns?> FindByIdempotencyKeyAsync(Guid jobId, string key, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            return await _context.JobAnalysisRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.JobId == jobId && r.IdempotencyKey == key, ct);
        }

        public async Task<JobAnalysisRuns?> FindReusableRunAsync(Guid jobId, int revision, string inputHash, CancellationToken ct = default)
        {
            return await _context.JobAnalysisRuns
                .AsNoTracking()
                .Where(r => r.JobId == jobId && r.InputRevision == revision && r.InputHash == inputHash && r.Status != JobAnalysisStatus.SUPERSEDED && r.Status != JobAnalysisStatus.FAILED)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> ActivateReusableRunAsync(Guid jobId, Guid runId, int expectedRevision, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            var job = await _context.JobPostings
                .FromSqlInterpolated($"SELECT * FROM job_postings WHERE id = {jobId} FOR UPDATE")
                .FirstOrDefaultAsync(ct);
            if (job == null || job.AnalysisRevision != expectedRevision)
            {
                return false;
            }

            var run = await _context.JobAnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId && r.JobId == jobId, ct);
            if (run == null || run.InputRevision != expectedRevision ||
                (run.Status != JobAnalysisStatus.PENDING && run.Status != JobAnalysisStatus.PROCESSING && run.Status != JobAnalysisStatus.READY))
            {
                return false;
            }

            job.ActiveAnalysisRunId = run.Id;
            job.AnalysisInputHash = run.InputHash;
            job.ParseStatus = run.Status == JobAnalysisStatus.READY ? "READY" : run.Status.ToString();
            job.ParseError = null;
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

        public async Task<JobAnalysisRuns?> GetActiveProcessingRunAsync(Guid jobId, string inputHash, CancellationToken ct = default)
        {
            return await _context.JobAnalysisRuns
                .AsNoTracking()
                .Where(r => r.JobId == jobId && r.InputHash == inputHash && (r.Status == JobAnalysisStatus.PENDING || r.Status == JobAnalysisStatus.PROCESSING))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<int> GetNextAttemptNumberAsync(Guid jobId, int revision, CancellationToken ct = default)
        {
            int maxAttempt = await _context.JobAnalysisRuns
                .Where(r => r.JobId == jobId && r.InputRevision == revision)
                .Select(r => (int?)r.AttemptNumber)
                .MaxAsync(ct) ?? 0;

            return maxAttempt + 1;
        }

        public async Task<JobAnalysisRuns> AddPendingRunAsync(JobAnalysisRuns run, CancellationToken ct = default)
        {
            return await CreatePendingRunAsync(run, ct);
        }

        public async Task<JobAnalysisRuns> CreatePendingRunAsync(JobAnalysisRuns run, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var previousRuns = await _context.JobAnalysisRuns
                .Where(r => r.JobId == run.JobId && (r.Status == JobAnalysisStatus.PENDING || r.Status == JobAnalysisStatus.PROCESSING))
                .ToListAsync(ct);

            foreach (var oldRun in previousRuns)
            {
                oldRun.Status = JobAnalysisStatus.SUPERSEDED;
            }

            _context.JobAnalysisRuns.Add(run);

            var job = await _context.JobPostings
                .FromSqlInterpolated($"SELECT * FROM job_postings WHERE id = {run.JobId} FOR UPDATE")
                .FirstOrDefaultAsync(ct);
            if (job != null)
            {
                job.ActiveAnalysisRunId = run.Id;
                job.AnalysisInputHash = run.InputHash;
                job.ParseStatus = "PENDING";
            }

            try
            {
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return run;
            }
            catch (DbUpdateException)
            {
                // The partial unique index allows only one live run per job
                // revision. A concurrent double-click/request should reuse that
                // run, not leak a database error to the recruiter.
                await tx.RollbackAsync(ct);
                _context.ChangeTracker.Clear();

                var activeRun = await _context.JobAnalysisRuns
                    .AsNoTracking()
                    .Where(r => r.JobId == run.JobId
                                && r.InputRevision == run.InputRevision
                                && (r.Status == JobAnalysisStatus.PENDING || r.Status == JobAnalysisStatus.PROCESSING))
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (activeRun != null)
                {
                    return activeRun;
                }

                throw;
            }
        }

        public async Task<IReadOnlyList<Guid>> ClaimPendingRunIdsAsync(int limit, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var now = DateTime.UtcNow;

            // Recover work abandoned by an application restart or a terminated
            // provider call.  A run is only re-queued after its lease expires.
            var staleProcessingCutoff = now.AddMinutes(-5);
            var staleRuns = await _context.JobAnalysisRuns
                .Where(r => r.Status == JobAnalysisStatus.PROCESSING
                            && ((r.LeaseExpiresAt != null && r.LeaseExpiresAt < now)
                                // Runs created before lease support (or interrupted
                                // before a lease was persisted) must not remain
                                // permanently invisible to the worker.
                                || (r.LeaseExpiresAt == null
                                    && r.StartedAt != null
                                    && r.StartedAt < staleProcessingCutoff)))
                .ToListAsync(ct);
            foreach (var staleRun in staleRuns)
            {
                staleRun.Status = JobAnalysisStatus.PENDING;
                staleRun.StartedAt = null;
                staleRun.LeaseExpiresAt = null;
                staleRun.LastHeartbeatAt = now;
            }

            // PostgreSQL row locks prevent multiple API instances from claiming
            // the same pending run and spending AI credits twice.
            var pendingRuns = await _context.JobAnalysisRuns
                .FromSqlInterpolated($@"SELECT *
                    FROM job_analysis_runs
                    WHERE status = {JobAnalysisStatus.PENDING.ToString()}
                    ORDER BY created_at
                    LIMIT {limit}
                    FOR UPDATE SKIP LOCKED")
                .ToListAsync(ct);

            if (pendingRuns.Count == 0) return Array.Empty<Guid>();

            var claimedIds = new List<Guid>();

            foreach (var run in pendingRuns)
            {
                run.Status = JobAnalysisStatus.PROCESSING;
                run.StartedAt = now;
                run.LastHeartbeatAt = now;
                run.LeaseExpiresAt = now.AddMinutes(5);
                claimedIds.Add(run.Id);
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return claimedIds;
        }

        public async Task<bool> TryMarkProviderCallStartedAsync(Guid runId, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            var run = await _context.JobAnalysisRuns
                .FromSqlInterpolated($"SELECT * FROM job_analysis_runs WHERE id = {runId} FOR UPDATE")
                .FirstOrDefaultAsync(ct);
            if (run == null || run.Status != JobAnalysisStatus.PROCESSING)
            {
                return false;
            }

            if (!run.ProviderCallStartedAt.HasValue)
            {
                run.ProviderCallStartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return true;
        }

        public async Task<bool> TryCompleteReadyAsync(
            Guid runId,
            JobAnalysisCompletion completion,
            CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var run = await _context.JobAnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
            if (run == null || run.Status != JobAnalysisStatus.PROCESSING)
            {
                return false;
            }

            var job = await _context.JobPostings
                .FromSqlInterpolated($"SELECT * FROM job_postings WHERE id = {run.JobId} FOR UPDATE")
                .FirstOrDefaultAsync(ct);
            if (job == null || job.AnalysisRevision != completion.ExpectedRevision || job.ActiveAnalysisRunId != runId)
            {
                run.Status = JobAnalysisStatus.SUPERSEDED;
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return false;
            }

            run.Status = JobAnalysisStatus.READY;
            run.RawAnalysisJson = completion.RawAnalysisJson;
            run.EffectiveAnalysisJson = completion.EffectiveAnalysisJson;
            run.AnalysisQuality = completion.Quality;
            run.AnalysisCoverageJson = completion.AnalysisCoverageJson;
            run.AnalysisDiagnosticsJson = completion.AnalysisDiagnosticsJson;
            run.ProviderName = completion.Provider;
            run.ModelName = completion.Model;
            run.FailureCode = null;
            run.ValidationErrorsJson = null;
            run.CompletedAt = DateTime.UtcNow;
            run.LeaseExpiresAt = null;
            run.LastHeartbeatAt = DateTime.UtcNow;
            run.DecisionVersion = completion.Decisions?.Count > 0 ? 1 : 0;

            // READY means parsing has completed; publication still requires
            // the recruiter to finalize the reviewed result.
            job.ParseStatus = completion.Quality == JdAnalysisQuality.INVALID ? "RAW_FALLBACK" : "READY";
            job.ParseError = null;

            if (completion.Decisions != null && completion.Decisions.Count > 0)
            {
                _context.JobSkillDecisions.AddRange(completion.Decisions);
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

        public async Task MarkFailedAsync(Guid runId, string failureCode, string validationErrorsJson, CancellationToken ct = default)
        {
            var run = await _context.JobAnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
            if (run != null)
            {
                run.Status = JobAnalysisStatus.FAILED;
                run.FailureCode = failureCode;
                run.ValidationErrorsJson = validationErrorsJson;
                run.AnalysisQuality = null;
                run.AnalysisCoverageJson = null;
                run.AnalysisDiagnosticsJson = null;
                run.RawAnalysisJson = null;
                run.EffectiveAnalysisJson = null;
                run.CompletedAt = DateTime.UtcNow;
                run.LeaseExpiresAt = null;
                run.LastHeartbeatAt = DateTime.UtcNow;

                var job = await _context.JobPostings
                    .FromSqlInterpolated($"SELECT * FROM job_postings WHERE id = {run.JobId} FOR UPDATE")
                    .FirstOrDefaultAsync(ct);
                if (job != null && job.ActiveAnalysisRunId == runId)
                {
                    job.ParseStatus = "FAILED";
                    job.ParseError = failureCode;
                }

                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task MarkSupersededAsync(Guid runId, CancellationToken ct = default)
        {
            var run = await _context.JobAnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
            if (run != null)
            {
                run.Status = JobAnalysisStatus.SUPERSEDED;
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<JobAnalysisPreviewDto?> GetPreviewAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default)
        {
            var job = await _context.JobPostings
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId, ct);

            if (job == null || job.RecruiterId != recruiterId) return null;

            Guid runId = job.ActiveAnalysisRunId ?? Guid.Empty;
            JobAnalysisRuns? run = null;
            if (runId != Guid.Empty)
            {
                run = await _context.JobAnalysisRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
            }

            if (run == null)
            {
                var lifecycleState = string.Equals(job.ParseStatus, "STALE", StringComparison.OrdinalIgnoreCase)
                    ? JobAnalysisLifecycleState.STALE
                    : JobAnalysisLifecycleState.NOT_REQUESTED;

                return new JobAnalysisPreviewDto
                {
                    JobId = jobId,
                    AnalysisRunId = Guid.Empty,
                    InputRevision = job.AnalysisRevision,
                    CurrentJobRevision = job.AnalysisRevision,
                    LifecycleState = lifecycleState,
                    IsCurrentAnalysis = false,
                    CanFinalize = false,
                    HasAnalysisRun = false,
                    BlockingReasons = lifecycleState == JobAnalysisLifecycleState.STALE
                        ? new List<string> { "Job source content changed. Run analysis again before publishing." }
                        : new List<string>()
                };
            }

            if (run.InputRevision != job.AnalysisRevision)
            {
                return new JobAnalysisPreviewDto
                {
                    JobId = jobId,
                    AnalysisRunId = Guid.Empty,
                    InputRevision = job.AnalysisRevision,
                    CurrentJobRevision = job.AnalysisRevision,
                    LifecycleState = JobAnalysisLifecycleState.STALE,
                    IsCurrentAnalysis = false,
                    CanFinalize = false,
                    HasAnalysisRun = false,
                    BlockingReasons = new List<string>
                    {
                        "The active analysis does not match the current job source content. Run analysis again before publishing."
                    }
                };
            }

            var decisions = await _context.JobSkillDecisions
                .AsNoTracking()
                .Include(d => d.SuggestedSkill)
                .Include(d => d.ResolvedSkill)
                .Where(d => d.JobAnalysisRunId == run.Id)
                .OrderBy(d => d.RawMention)
                .ToListAsync(ct);

            int decisionVersion = decisions.Count > 0 ? decisions.Max(d => d.DecisionVersion) : 0;

            var suggestions = decisions.Select(d => new JobSkillDecisionDto
            {
                Id = d.Id,
                RawMention = d.RawMention,
                NormalizedMention = d.NormalizedMention,
                Category = d.Category,
                Importance = d.Importance,
                SourceSection = d.SourceSection,
                EvidenceText = d.EvidenceText,
                SuggestedSkillId = d.SuggestedSkillId,
                SuggestedSkillName = d.SuggestedSkill?.Name,
                ResolvedSkillId = d.ResolvedSkillId,
                ResolvedSkillName = d.ResolvedSkill?.Name,
                ResolutionStatus = d.ResolutionStatus,
                DecisionStatus = d.DecisionStatus
            }).ToList();

            var blockingReasons = new List<string>();
            if (run.Status != JobAnalysisStatus.READY)
            {
                blockingReasons.Add($"Analysis run is currently in status '{run.Status}'. It must be READY to publish.");
            }

            var analysisQuality = run.AnalysisQuality;
            if (!analysisQuality.HasValue)
            {
                var persistedQuality = JdAnalysisMetadataReader.ReadQuality(run.EffectiveAnalysisJson);
                if (Enum.TryParse<JdAnalysisQuality>(persistedQuality, ignoreCase: true, out var parsedQuality))
                {
                    analysisQuality = parsedQuality;
                }
            }
            var analysisDiagnostics = JdAnalysisMetadataReader.ReadDiagnosticsJson(run.AnalysisDiagnosticsJson);
            if (analysisDiagnostics.Count == 0)
            {
                analysisDiagnostics = JdAnalysisMetadataReader.ReadDiagnostics(run.EffectiveAnalysisJson);
            }

            // AI owns the technical extraction.  A recruiter is not required to
            // approve individual technologies: only resolved dictionary skills
            // become tags/filters; all other validated requirements remain in the
            // detailed matching contract.

            return new JobAnalysisPreviewDto
            {
                JobId = jobId,
                HasAnalysisRun = true,
                AnalysisRunId = run.Id,
                InputRevision = run.InputRevision,
                CurrentJobRevision = job.AnalysisRevision,
                LifecycleState = ToLifecycleState(run.Status),
                IsCurrentAnalysis = run.InputRevision == job.AnalysisRevision && job.ActiveAnalysisRunId == run.Id,
                Status = run.Status,
                AnalysisQuality = analysisQuality,
                AnalysisCoverage = JdAnalysisMetadataReader.ReadCoverageJson(run.AnalysisCoverageJson)
                    ?? JdAnalysisMetadataReader.ReadCoverage(run.EffectiveAnalysisJson),
                AnalysisDiagnostics = analysisDiagnostics,
                UsesRawTextFallback = analysisQuality == JdAnalysisQuality.INVALID,
                DecisionVersion = decisionVersion,
                FailureCode = run.FailureCode,
                Suggestions = suggestions,
                CanFinalize = blockingReasons.Count == 0,
                BlockingReasons = blockingReasons,
                FinalActionLabel = "Publish",
                FinalTargetStatus = "PUBLISHED"
            };
        }

        private static JobAnalysisLifecycleState ToLifecycleState(JobAnalysisStatus status)
        {
            return status switch
            {
                JobAnalysisStatus.PENDING => JobAnalysisLifecycleState.PENDING,
                JobAnalysisStatus.PROCESSING => JobAnalysisLifecycleState.PROCESSING,
                JobAnalysisStatus.READY => JobAnalysisLifecycleState.READY,
                JobAnalysisStatus.FAILED => JobAnalysisLifecycleState.FAILED,
                _ => JobAnalysisLifecycleState.STALE
            };
        }

        public async Task<ApplyDecisionResult> ApplyDecisionsAsync(
            Guid jobId,
            Guid runId,
            Guid recruiterId,
            int expectedJobRevision,
            int expectedDecisionVersion,
            IReadOnlyList<JobSkillDecisionInputDto> decisions,
            CancellationToken ct = default)
        {
            var res = new ApplyDecisionResult();
            await using var tx = IsInMemoryProvider()
                ? null
                : await _context.Database.BeginTransactionAsync(ct);

            var job = await GetJobForUpdateAsync(jobId, ct);
            if (job == null || job.RecruiterId != recruiterId)
            {
                res.Success = false;
                res.ErrorCode = "JOB_NOT_FOUND";
                res.ErrorMessage = "Job posting not found or access denied.";
                return res;
            }

            if (job.AnalysisRevision != expectedJobRevision)
            {
                res.Success = false;
                res.ErrorCode = "ANALYSIS_STALE";
                res.ErrorMessage = "Job semantic requirements have changed. Please rerun analysis.";
                return res;
            }

            var run = await _context.JobAnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId && r.JobId == jobId, ct);
            if (run == null || run.InputRevision != job.AnalysisRevision || job.ActiveAnalysisRunId != runId)
            {
                res.Success = false;
                res.ErrorCode = "ANALYSIS_STALE";
                res.ErrorMessage = "Analysis run is not the current job revision. Please rerun analysis.";
                return res;
            }

            if (run.Status != JobAnalysisStatus.READY)
            {
                res.Success = false;
                res.ErrorCode = "RUN_NOT_READY";
                res.ErrorMessage = "Analysis run is not ready for decision updates.";
                return res;
            }

            if (run.DecisionVersion != expectedDecisionVersion)
            {
                res.Success = false;
                res.ErrorCode = "DECISION_VERSION_CONFLICT";
                res.ErrorMessage = $"Skill decision version mismatch. Expected '{expectedDecisionVersion}' but found '{run.DecisionVersion}'.";
                return res;
            }

            var existingDecisions = await _context.JobSkillDecisions
                .Where(d => d.JobAnalysisRunId == runId)
                .ToListAsync(ct);

            foreach (var input in decisions)
            {
                var target = existingDecisions.FirstOrDefault(d => d.Id == input.DecisionId);
                if (target != null)
                {
                    target.DecisionStatus = input.Decision;
                    if (input.ResolvedSkillId.HasValue)
                    {
                        var activeSkill = await _context.Skills.AsNoTracking().FirstOrDefaultAsync(s => s.Id == input.ResolvedSkillId.Value && s.Status == SkillStatus.ACTIVE, ct);
                        if (activeSkill != null)
                        {
                            target.ResolvedSkillId = activeSkill.Id;
                            target.ResolutionStatus = SkillResolutionStatus.MANUAL;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(input.Importance))
                    {
                        target.Importance = input.Importance.ToLowerInvariant();
                    }
                    target.DecisionVersion = expectedDecisionVersion + 1;
                    target.UpdatedAt = DateTime.UtcNow;
                }
            }

            run.DecisionVersion = expectedDecisionVersion + 1;

            await _context.SaveChangesAsync(ct);
            if (tx != null)
            {
                await tx.CommitAsync(ct);
            }

            res.Success = true;
            res.Preview = await GetPreviewAsync(jobId, recruiterId, ct);
            return res;
        }

        public async Task<FinalizeJobResult> FinalizeAsync(
            Guid jobId,
            Guid runId,
            Guid recruiterId,
            int expectedJobRevision,
            int expectedDecisionVersion,
            bool confirmNoStandardSkills,
            bool reviewRequired,
            CancellationToken ct = default)
        {
            var res = new FinalizeJobResult();
            await using var tx = IsInMemoryProvider()
                ? null
                : await _context.Database.BeginTransactionAsync(ct);

            var job = await GetJobForUpdateAsync(jobId, ct);
            if (job == null || job.RecruiterId != recruiterId)
            {
                res.Success = false;
                res.ErrorCode = "JOB_NOT_FOUND";
                res.ErrorMessage = "Job posting not found or access denied.";
                return res;
            }

            var isPublishedEdit = job.Status == JobStatus.PUBLISHED;
            var hasPendingPublishedAnalysis = isPublishedEdit
                && job.EffectiveAnalysisRevision != job.AnalysisRevision
                && job.ParseStatus?.ToUpperInvariant() is
                    "STALE" or "PENDING" or "PROCESSING" or "READY" or "FAILED" or "RAW_FALLBACK";

            if (isPublishedEdit && !hasPendingPublishedAnalysis)
            {
                res.Success = true;
                res.Job = job;
                res.SkillCount = await _context.JobSkillRequirements.CountAsync(jsr => jsr.JobId == jobId, ct);
                return res;
            }

            if (job.Status != JobStatus.DRAFT && !isPublishedEdit)
            {
                res.Success = false;
                res.ErrorCode = "JOB_NOT_EDITABLE";
                res.ErrorMessage = "Only an editable DRAFT or PUBLISHED job can be finalized.";
                return res;
            }

            if (job.IsBanned)
            {
                res.Success = false;
                res.ErrorCode = "JOB_BANNED";
                res.ErrorMessage = "A banned job posting cannot be finalized.";
                return res;
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == job.CompanyId, ct);
            if (company == null || company.Status != CompanyStatus.VERIFIED)
            {
                res.Success = false;
                res.ErrorCode = "COMPANY_NOT_VERIFIED";
                res.ErrorMessage = "Company must be verified before publishing a job posting.";
                return res;
            }

            if (job.AnalysisRevision != expectedJobRevision)
            {
                res.Success = false;
                res.ErrorCode = "ANALYSIS_STALE";
                res.ErrorMessage = "Job input revision mismatch.";
                return res;
            }

            var run = await _context.JobAnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId && r.JobId == jobId, ct);
            if (run == null || run.InputRevision != job.AnalysisRevision || job.ActiveAnalysisRunId != runId)
            {
                res.Success = false;
                res.ErrorCode = "ANALYSIS_STALE";
                res.ErrorMessage = "Analysis run is not the current job revision. Please rerun analysis.";
                return res;
            }

            if (run.Status != JobAnalysisStatus.READY)
            {
                res.Success = false;
                res.ErrorCode = "RUN_NOT_READY";
                res.ErrorMessage = "Analysis run is not READY.";
                return res;
            }

            if (run.DecisionVersion != expectedDecisionVersion)
            {
                res.Success = false;
                res.ErrorCode = "DECISION_VERSION_CONFLICT";
                res.ErrorMessage = $"Decision version mismatch during finalize. Expected '{expectedDecisionVersion}' but found '{run.DecisionVersion}'.";
                return res;
            }

            var acceptedDecisions = await _context.JobSkillDecisions
                .Where(d => d.JobAnalysisRunId == runId && d.DecisionStatus == SkillDecisionStatus.ACCEPTED && d.ResolvedSkillId != null)
                .ToListAsync(ct);

            if (acceptedDecisions.Count == 0 && !confirmNoStandardSkills)
            {
                res.Success = false;
                res.ErrorCode = "NO_STANDARD_SKILLS_UNCONFIRMED";
                res.ErrorMessage = "Job posting has no accepted standard skills. Confirmation is required.";
                return res;
            }

            if (!isPublishedEdit)
            {
                // The initial publish entitlement participates in this same transaction.
                // Updating an already-published job must not charge PostJob again.
                await _featureUsageUseCase.TryConsumeFeatureAsync(recruiterId, "PostJob", job.Id.ToString());
            }

            bool isRawFallback = run.AnalysisQuality == JdAnalysisQuality.INVALID;

            if (!isRawFallback && string.IsNullOrWhiteSpace(run.EffectiveAnalysisJson))
            {
                res.Success = false;
                res.ErrorCode = "ANALYSIS_RESULT_INTEGRITY_ERROR";
                res.ErrorMessage = "Analysis result is incomplete. Please retry analysis.";
                return res;
            }

            // Remove existing JSR
            var oldJsr = await _context.JobSkillRequirements.Where(jsr => jsr.JobId == jobId).ToListAsync(ct);
            _context.JobSkillRequirements.RemoveRange(oldJsr);

            // Distinct final skills by skill ID
            var distinctSkillIds = acceptedDecisions.Select(d => d.ResolvedSkillId!.Value).Distinct().ToList();
            var newJsrList = new List<JobSkillRequirements>();

            foreach (var skillId in distinctSkillIds)
            {
                var decision = acceptedDecisions.First(d => d.ResolvedSkillId == skillId);
                newJsrList.Add(new JobSkillRequirements
                {
                    JobId = jobId,
                    SkillId = skillId,
                    IsMandatory = decision.Importance.Equals("must_have", StringComparison.OrdinalIgnoreCase)
                });
            }

            _context.JobSkillRequirements.AddRange(newJsrList);

            job.ParsedData = isRawFallback ? null : run.EffectiveAnalysisJson;
            job.ParseStatus = isRawFallback ? "RAW_FALLBACK" : "SUCCESS";
            job.ParseError = null;
            job.EffectiveAnalysisRevision = job.AnalysisRevision;
            job.EffectiveAnalysisRunId = run.Id;

            // Clear embeddings to trigger rebuild. A raw-text fallback has no
            // structured metrics to embed, so it must also clear any stale
            // embeddings from an earlier analysis revision.
            job.TitleEmbedding = null;
            job.SkillsEmbedding = null;
            job.ExperienceEmbedding = null;
            job.DomainEmbedding = null;

            var targetStatus = isPublishedEdit
                ? JobStatus.PUBLISHED
                : reviewRequired ? JobStatus.PENDING_REVIEW : JobStatus.PUBLISHED;
            job.Status = targetStatus;
            if (!isPublishedEdit && targetStatus == JobStatus.PUBLISHED && !job.PublishedAt.HasValue)
            {
                job.PublishedAt = DateTime.UtcNow;
                job.ExpiresAt = job.PublishedAt.Value.AddDays(30);
            }
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            if (tx != null)
            {
                await tx.CommitAsync(ct);
            }

            res.Success = true;
            res.Job = job;
            res.SkillCount = newJsrList.Count;
            return res;
        }

        private Task<JobPostings?> GetJobForUpdateAsync(Guid jobId, CancellationToken ct)
        {
            if (IsInMemoryProvider())
            {
                return _context.JobPostings.FirstOrDefaultAsync(job => job.Id == jobId, ct);
            }

            return _context.JobPostings
                .FromSqlInterpolated($"SELECT * FROM job_postings WHERE id = {jobId} FOR UPDATE")
                .FirstOrDefaultAsync(ct);
        }

        private bool IsInMemoryProvider()
            => string.Equals(
                _context.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal);
    }
}
