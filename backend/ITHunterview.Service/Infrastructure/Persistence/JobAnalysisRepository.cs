using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public sealed class ApplyDecisionResult
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public JobAnalysisPreviewDto? Preview { get; set; }
    }

    public sealed class FinalizeJobResult
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public JobPostings? Job { get; set; }
        public int SkillCount { get; set; }
    }

    public interface IJobAnalysisRepository
    {
        Task<JobAnalysisRuns?> GetRunAsync(Guid runId, CancellationToken ct = default);
        Task<JobAnalysisRuns?> GetReusableReadyRunAsync(Guid jobId, string inputHash, CancellationToken ct = default);
        Task<JobAnalysisRuns?> GetActiveProcessingRunAsync(Guid jobId, string inputHash, CancellationToken ct = default);
        Task<JobAnalysisRuns> AddPendingRunAsync(JobAnalysisRuns run, CancellationToken ct = default);
        Task<IReadOnlyList<Guid>> ClaimPendingRunIdsAsync(int limit, CancellationToken ct = default);
        Task<bool> TryCompleteReadyAsync(
            Guid runId,
            int expectedRevision,
            string rawJson,
            string effectiveJson,
            IReadOnlyList<JobSkillDecisions> decisions,
            string? provider,
            string? model,
            CancellationToken ct = default);
        Task MarkFailedAsync(Guid runId, string failureCode, string validationErrorsJson, CancellationToken ct = default);
        Task<JobAnalysisPreviewDto?> GetPreviewAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default);
        Task<ApplyDecisionResult> ApplyDecisionsAsync(
            Guid jobId,
            Guid runId,
            Guid recruiterId,
            int expectedJobRevision,
            int expectedDecisionVersion,
            IReadOnlyList<JobSkillDecisionInputDto> decisions,
            CancellationToken ct = default);
        Task<FinalizeJobResult> FinalizeAsync(
            Guid jobId,
            Guid runId,
            Guid recruiterId,
            int expectedJobRevision,
            int expectedDecisionVersion,
            JobStatus targetStatus,
            CancellationToken ct = default);
    }

    public class JobAnalysisRepository : IJobAnalysisRepository
    {
        private readonly ITHunterviewContext _context;

        public JobAnalysisRepository(ITHunterviewContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<JobAnalysisRuns?> GetRunAsync(Guid runId, CancellationToken ct = default)
        {
            return await _context.JobAnalysisRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == runId, ct);
        }

        public async Task<JobAnalysisRuns?> GetReusableReadyRunAsync(Guid jobId, string inputHash, CancellationToken ct = default)
        {
            return await _context.JobAnalysisRuns
                .AsNoTracking()
                .Where(r => r.JobId == jobId && r.InputHash == inputHash && r.Status == JobAnalysisStatus.READY)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<JobAnalysisRuns?> GetActiveProcessingRunAsync(Guid jobId, string inputHash, CancellationToken ct = default)
        {
            return await _context.JobAnalysisRuns
                .AsNoTracking()
                .Where(r => r.JobId == jobId && r.InputHash == inputHash && (r.Status == JobAnalysisStatus.PENDING || r.Status == JobAnalysisStatus.PROCESSING))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<JobAnalysisRuns> AddPendingRunAsync(JobAnalysisRuns run, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            // Mark existing pending/processing/ready runs as SUPERSEDED if revision changed
            var previousRuns = await _context.JobAnalysisRuns
                .Where(r => r.JobId == run.JobId && r.InputRevision < run.InputRevision &&
                           (r.Status == JobAnalysisStatus.PENDING || r.Status == JobAnalysisStatus.PROCESSING || r.Status == JobAnalysisStatus.READY))
                .ToListAsync(ct);

            foreach (var oldRun in previousRuns)
            {
                oldRun.Status = JobAnalysisStatus.SUPERSEDED;
            }

            _context.JobAnalysisRuns.Add(run);

            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == run.JobId, ct);
            if (job != null)
            {
                job.ActiveAnalysisRunId = run.Id;
                job.AnalysisInputHash = run.InputHash;
                job.ParseStatus = "PENDING";
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return run;
        }

        public async Task<IReadOnlyList<Guid>> ClaimPendingRunIdsAsync(int limit, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var pendingRuns = await _context.JobAnalysisRuns
                .Where(r => r.Status == JobAnalysisStatus.PENDING)
                .OrderBy(r => r.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);

            if (pendingRuns.Count == 0) return Array.Empty<Guid>();

            var claimedIds = new List<Guid>();
            DateTime now = DateTime.UtcNow;

            foreach (var run in pendingRuns)
            {
                run.Status = JobAnalysisStatus.PROCESSING;
                run.StartedAt = now;
                claimedIds.Add(run.Id);
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return claimedIds;
        }

        public async Task<bool> TryCompleteReadyAsync(
            Guid runId,
            int expectedRevision,
            string rawJson,
            string effectiveJson,
            IReadOnlyList<JobSkillDecisions> decisions,
            string? provider,
            string? model,
            CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var run = await _context.JobAnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
            if (run == null || run.Status != JobAnalysisStatus.PROCESSING)
            {
                return false;
            }

            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == run.JobId, ct);
            if (job == null || job.AnalysisRevision != expectedRevision || job.ActiveAnalysisRunId != runId)
            {
                run.Status = JobAnalysisStatus.SUPERSEDED;
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return false;
            }

            run.Status = JobAnalysisStatus.READY;
            run.RawAnalysisJson = rawJson;
            run.EffectiveAnalysisJson = effectiveJson;
            run.ProviderName = provider;
            run.ModelName = model;
            run.CompletedAt = DateTime.UtcNow;

            if (decisions != null && decisions.Count > 0)
            {
                _context.JobSkillDecisions.AddRange(decisions);
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
                run.CompletedAt = DateTime.UtcNow;

                var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == run.JobId, ct);
                if (job != null && job.ActiveAnalysisRunId == runId)
                {
                    job.ParseStatus = "FAILED";
                    job.ParseError = failureCode;
                }

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
                return new JobAnalysisPreviewDto
                {
                    JobId = jobId,
                    AnalysisRunId = Guid.Empty,
                    InputRevision = job.AnalysisRevision,
                    Status = JobAnalysisStatus.PENDING,
                    CanFinalize = false,
                    BlockingReasons = new List<string> { "No analysis has been initiated for this job." }
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
                DecisionStatus = d.DecisionStatus,
                Confidence = d.Confidence
            }).ToList();

            var blockingReasons = new List<string>();
            if (run.Status != JobAnalysisStatus.READY)
            {
                blockingReasons.Add($"Analysis run is currently in status '{run.Status}'. It must be READY to publish.");
            }

            var unmappedOrPendingDecisions = decisions.Where(d => d.DecisionStatus == SkillDecisionStatus.PENDING).ToList();
            if (unmappedOrPendingDecisions.Any())
            {
                blockingReasons.Add($"There are {unmappedOrPendingDecisions.Count} skill proposals requiring recruiter review.");
            }

            var unresolvedSkills = decisions.Where(d => d.DecisionStatus == SkillDecisionStatus.ACCEPTED && d.ResolvedSkillId == null).ToList();
            if (unresolvedSkills.Any())
            {
                blockingReasons.Add($"{unresolvedSkills.Count} accepted skills are not mapped to a master skill in the dictionary.");
            }

            return new JobAnalysisPreviewDto
            {
                JobId = jobId,
                AnalysisRunId = run.Id,
                InputRevision = run.InputRevision,
                Status = run.Status,
                DecisionVersion = decisionVersion,
                FailureCode = run.FailureCode,
                Suggestions = suggestions,
                CanFinalize = blockingReasons.Count == 0,
                BlockingReasons = blockingReasons,
                FinalActionLabel = "Publish",
                FinalTargetStatus = "PUBLISHED"
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
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == jobId, ct);
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
            if (run == null || run.Status != JobAnalysisStatus.READY)
            {
                res.Success = false;
                res.ErrorCode = "RUN_NOT_READY";
                res.ErrorMessage = "Analysis run is not ready for decision updates.";
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

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

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
            JobStatus targetStatus,
            CancellationToken ct = default)
        {
            var res = new FinalizeJobResult();
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null || job.RecruiterId != recruiterId)
            {
                res.Success = false;
                res.ErrorCode = "JOB_NOT_FOUND";
                res.ErrorMessage = "Job posting not found or access denied.";
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
            if (run == null || run.Status != JobAnalysisStatus.READY)
            {
                res.Success = false;
                res.ErrorCode = "RUN_NOT_READY";
                res.ErrorMessage = "Analysis run is not READY.";
                return res;
            }

            var acceptedDecisions = await _context.JobSkillDecisions
                .Where(d => d.JobAnalysisRunId == runId && d.DecisionStatus == SkillDecisionStatus.ACCEPTED && d.ResolvedSkillId != null)
                .ToListAsync(ct);

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

            // Copy effective analysis JSON into ParsedData compatibility projection
            job.ParsedData = run.EffectiveAnalysisJson ?? run.RawAnalysisJson;
            job.ParseStatus = "SUCCESS";
            job.ParseError = null;

            // Clear embeddings to trigger rebuild
            job.TitleEmbedding = null;
            job.SkillsEmbedding = null;
            job.ExperienceEmbedding = null;
            job.DomainEmbedding = null;

            job.Status = targetStatus;
            if (targetStatus == JobStatus.PUBLISHED && !job.PublishedAt.HasValue)
            {
                job.PublishedAt = DateTime.UtcNow;
            }
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            res.Success = true;
            res.Job = job;
            res.SkillCount = newJsrList.Count;
            return res;
        }
    }
}
