using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.UseCase
{
    public class JobAnalysisUseCase : IJobAnalysisUseCase
    {
        private readonly IJobAnalysisRepository _jobAnalysisRepository;
        private readonly IJobAnalysisInputBuilder _inputBuilder;
        private readonly IPromptManagementService _promptService;
        private readonly ILogger<JobAnalysisUseCase> _logger;

        public JobAnalysisUseCase(
            IJobAnalysisRepository jobAnalysisRepository,
            IJobAnalysisInputBuilder inputBuilder,
            IPromptManagementService promptService,
            ILogger<JobAnalysisUseCase> logger)
        {
            _jobAnalysisRepository = jobAnalysisRepository ?? throw new ArgumentNullException(nameof(jobAnalysisRepository));
            _inputBuilder = inputBuilder ?? throw new ArgumentNullException(nameof(inputBuilder));
            _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<JobAnalysisStatusDto> RequestAnalysisAsync(Guid jobId, Guid recruiterId, AnalyzeJobRequestDto dto, CancellationToken ct = default)
        {
            var reqContext = await _jobAnalysisRepository.GetRequestContextAsync(jobId, recruiterId, ct);
            if (reqContext == null)
            {
                throw new KeyNotFoundException("Job posting not found or access denied.");
            }

            var job = reqContext.Job;
            if (job.Status != JobStatus.DRAFT)
            {
                throw new InvalidOperationException("ONLY_DRAFT_JOB_CAN_BE_ANALYZED: Analysis can only be requested for jobs in DRAFT status.");
            }

            if (!reqContext.IsCompanyVerified)
            {
                throw new InvalidOperationException("UNVERIFIED_COMPANY: Company must be VERIFIED to run AI analysis.");
            }

            if (dto.ExpectedRevision != job.AnalysisRevision)
            {
                throw JobAnalysisException.AnalysisStale($"Expected revision '{dto.ExpectedRevision}' does not match current job revision '{job.AnalysisRevision}'.");
            }

            // Check idempotency key
            if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
            {
                var existingIdempotentRun = await _jobAnalysisRepository.FindByIdempotencyKeyAsync(jobId, dto.IdempotencyKey, ct);
                if (existingIdempotentRun != null)
                {
                    if (existingIdempotentRun.InputRevision == job.AnalysisRevision)
                    {
                        if (!await _jobAnalysisRepository.ActivateReusableRunAsync(jobId, existingIdempotentRun.Id, job.AnalysisRevision, ct))
                        {
                            throw JobAnalysisException.AnalysisStale("The existing analysis run is no longer current.");
                        }
                        return MapToStatusDto(existingIdempotentRun, isReused: true,
                            isQueued: existingIdempotentRun.Status == JobAnalysisStatus.PENDING || existingIdempotentRun.Status == JobAnalysisStatus.PROCESSING);
                    }
                    throw JobAnalysisException.InvalidPayload("IDEMPOTENCY_KEY_REUSED: Idempotency key reused for a different job revision.");
                }
            }

            var promptPair = await _promptService.GetActivePromptPairSnapshotAsync(
                JdAnalysisPromptContract.SystemPromptKey,
                JdAnalysisPromptContract.UserPromptKey,
                ct);
            var sysPrompt = promptPair.System;
            var userPrompt = promptPair.User;
            if (string.IsNullOrWhiteSpace(promptPair.Contract))
            {
                throw new InvalidOperationException("JD_ANALYSIS_PROMPT_CONTRACT_MISSING: Active JD prompt pair must declare its analysis contract.");
            }

            var snapshot = _inputBuilder.Build(job);
            var analysisInputHash = _inputBuilder.ComputeAnalysisHash(snapshot, sysPrompt.VersionId, userPrompt.VersionId, promptPair.Contract);

            // Reusable run check
            var reusableRun = await _jobAnalysisRepository.FindReusableRunAsync(jobId, job.AnalysisRevision, analysisInputHash, ct);
            if (reusableRun != null)
            {
                if (reusableRun.Status == JobAnalysisStatus.READY)
                {
                    if (!await _jobAnalysisRepository.ActivateReusableRunAsync(jobId, reusableRun.Id, job.AnalysisRevision, ct))
                    {
                        throw JobAnalysisException.AnalysisStale("The reusable analysis run is no longer current.");
                    }
                    return MapToStatusDto(reusableRun, isReused: true);
                }
                if (reusableRun.Status == JobAnalysisStatus.PENDING || reusableRun.Status == JobAnalysisStatus.PROCESSING)
                {
                    if (!await _jobAnalysisRepository.ActivateReusableRunAsync(jobId, reusableRun.Id, job.AnalysisRevision, ct))
                    {
                        throw JobAnalysisException.AnalysisStale("The reusable analysis run is no longer current.");
                    }
                    return MapToStatusDto(reusableRun, isReused: true, isQueued: true);
                }
            }

            int nextAttempt = await _jobAnalysisRepository.GetNextAttemptNumberAsync(jobId, job.AnalysisRevision, ct);
            string rawSnapshotJson = _inputBuilder.SerializeCanonical(snapshot);

            var newRun = new JobAnalysisRuns
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                InputRevision = job.AnalysisRevision,
                InputHash = analysisInputHash,
                AttemptNumber = nextAttempt,
                IdempotencyKey = string.IsNullOrWhiteSpace(dto.IdempotencyKey) ? null : dto.IdempotencyKey,
                Status = JobAnalysisStatus.PENDING,
                SystemPromptVersionId = sysPrompt.VersionId,
                UserPromptVersionId = userPrompt.VersionId,
                SchemaVersion = promptPair.Contract,
                RawInputSnapshot = rawSnapshotJson,
                RequestedBy = recruiterId,
                DecisionVersion = 0,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _jobAnalysisRepository.CreatePendingRunAsync(newRun, ct);
            _logger.LogInformation("Created job analysis run {RunId} (attempt {Attempt}) for Job {JobId} revision {Revision}.", created.Id, nextAttempt, jobId, job.AnalysisRevision);

            return MapToStatusDto(created, isQueued: true);
        }

        public async Task<JobAnalysisStatusDto> RetryAnalysisAsync(Guid jobId, Guid runId, Guid recruiterId, AnalyzeJobRequestDto dto, CancellationToken ct = default)
        {
            var reqContext = await _jobAnalysisRepository.GetRequestContextAsync(jobId, recruiterId, ct);
            if (reqContext == null)
            {
                throw new KeyNotFoundException("Job posting not found or access denied.");
            }

            var job = reqContext.Job;
            if (job.Status != JobStatus.DRAFT)
            {
                throw new InvalidOperationException("ONLY_DRAFT_JOB_CAN_BE_ANALYZED: Job is not in DRAFT status.");
            }

            var targetRun = await _jobAnalysisRepository.GetRunAsync(runId, ct);
            if (targetRun == null || targetRun.JobId != jobId)
            {
                throw new KeyNotFoundException("Referenced analysis run not found.");
            }

            if (targetRun.Status != JobAnalysisStatus.FAILED)
            {
                throw new InvalidOperationException("ONLY_FAILED_RUN_CAN_BE_RETRIED: Only FAILED analysis runs can be retried.");
            }

            if (targetRun.InputRevision != job.AnalysisRevision)
            {
                throw JobAnalysisException.AnalysisStale("Cannot retry failed run from a previous revision.");
            }

            return await RequestAnalysisAsync(jobId, recruiterId, dto, ct);
        }

        public async Task<JobAnalysisPreviewDto?> GetPreviewAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default)
        {
            return await _jobAnalysisRepository.GetPreviewAsync(jobId, recruiterId, ct);
        }

        public async Task<JobAnalysisPreviewDto> UpdateDecisionsAsync(Guid jobId, Guid runId, Guid recruiterId, UpdateJobSkillDecisionsDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var result = await _jobAnalysisRepository.ApplyDecisionsAsync(
                jobId,
                runId,
                recruiterId,
                dto.ExpectedJobRevision,
                dto.ExpectedDecisionVersion,
                dto.Decisions,
                ct);

            if (!result.Success)
            {
                if (result.ErrorCode == "ANALYSIS_STALE")
                {
                    throw JobAnalysisException.AnalysisStale(result.ErrorMessage ?? "Analysis state is stale.");
                }
                if (result.ErrorCode == "DECISION_VERSION_CONFLICT")
                {
                    throw JobAnalysisException.DecisionVersionConflict(result.ErrorMessage ?? "Decision version conflict.");
                }
                throw JobAnalysisException.InvalidPayload(result.ErrorMessage ?? "Failed to apply decisions.");
            }

            return result.Preview!;
        }

        public async Task<FinalizeJobResponseDto> FinalizeAsync(Guid jobId, Guid recruiterId, FinalizeJobRequestDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var result = await _jobAnalysisRepository.FinalizeAsync(
                jobId,
                dto.AnalysisRunId,
                recruiterId,
                dto.ExpectedJobRevision,
                dto.ExpectedDecisionVersion,
                dto.ConfirmNoStandardSkills,
                // There is currently no staff job-review workflow or approval
                // endpoint.  Finalization must therefore publish the reviewed
                // draft instead of trapping it permanently in PENDING_REVIEW.
                reviewRequired: false,
                ct);

            if (!result.Success)
            {
                switch (result.ErrorCode)
                {
                    case "ANALYSIS_STALE":
                        throw JobAnalysisException.AnalysisStale(result.ErrorMessage);
                    case "DECISION_VERSION_CONFLICT":
                        throw JobAnalysisException.DecisionVersionConflict(result.ErrorMessage);
                    case "INCOMPLETE_REVIEW":
                    case "NO_STANDARD_SKILLS_UNCONFIRMED":
                        throw JobAnalysisException.IncompleteReview(result.ErrorMessage ?? "Recruiter review is incomplete.");
                    default:
                        throw JobAnalysisException.InvalidPayload(result.ErrorMessage ?? "Failed to finalize job posting.");
                }
            }

            return new FinalizeJobResponseDto
            {
                Success = true,
                Message = "Job posting finalized successfully.",
                JobId = jobId,
                Status = result.Job!.Status.ToString(),
                FinalJobStatus = result.Job!.Status.ToString(),
                SkillCount = result.SkillCount,
                ParseStatus = result.Job.ParseStatus,
                PublishedAt = result.Job.PublishedAt
            };
        }

        private static JobAnalysisStatusDto MapToStatusDto(JobAnalysisRuns run, bool isReused = false, bool isQueued = false)
        {
            return new JobAnalysisStatusDto
            {
                RunId = run.Id,
                JobId = run.JobId,
                InputRevision = run.InputRevision,
                CurrentJobRevision = run.InputRevision,
                Status = run.Status,
                FailureCode = run.FailureCode,
                CreatedAt = run.CreatedAt,
                CompletedAt = run.CompletedAt,
                IsReused = isReused,
                IsQueued = isQueued
            };
        }
    }
}
