using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Helpers;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.UseCase
{
    public interface IJobAnalysisUseCase
    {
        Task<JobAnalysisStatusDto> RequestAnalysisAsync(Guid jobId, Guid recruiterId, AnalyzeJobRequestDto dto, CancellationToken ct = default);
        Task<JobAnalysisPreviewDto?> GetPreviewAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default);
        Task<JobAnalysisPreviewDto> UpdateDecisionsAsync(Guid jobId, Guid runId, Guid recruiterId, UpdateJobSkillDecisionsDto dto, CancellationToken ct = default);
        Task<FinalizeJobResponseDto> FinalizeAsync(Guid jobId, Guid recruiterId, FinalizeJobRequestDto dto, CancellationToken ct = default);
    }

    public class JobAnalysisUseCase : IJobAnalysisUseCase
    {
        private readonly ITHunterviewContext _context;
        private readonly IJobAnalysisRepository _jobAnalysisRepository;
        private readonly IJobAnalysisInputBuilder _inputBuilder;
        private readonly IPromptManagementService _promptService;
        private readonly ILogger<JobAnalysisUseCase> _logger;

        public JobAnalysisUseCase(
            ITHunterviewContext context,
            IJobAnalysisRepository jobAnalysisRepository,
            IJobAnalysisInputBuilder inputBuilder,
            IPromptManagementService promptService,
            ILogger<JobAnalysisUseCase> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jobAnalysisRepository = jobAnalysisRepository ?? throw new ArgumentNullException(nameof(jobAnalysisRepository));
            _inputBuilder = inputBuilder ?? throw new ArgumentNullException(nameof(inputBuilder));
            _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<JobAnalysisStatusDto> RequestAnalysisAsync(Guid jobId, Guid recruiterId, AnalyzeJobRequestDto dto, CancellationToken ct = default)
        {
            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null || job.RecruiterId != recruiterId)
            {
                throw new KeyNotFoundException("Job posting not found or access denied.");
            }

            if (job.Status != JobStatus.DRAFT)
            {
                throw new InvalidOperationException("ONLY_DRAFT_JOB_CAN_BE_ANALYZED: Analysis can only be requested for jobs in DRAFT status.");
            }

            if (dto.ExpectedRevision != job.AnalysisRevision)
            {
                throw new InvalidOperationException($"ANALYSIS_STALE: Expected revision '{dto.ExpectedRevision}' does not match current job revision '{job.AnalysisRevision}'.");
            }

            var systemPromptSnapshot = await _promptService.GetActivePromptSnapshotAsync("JD_ANALYSIS_V2_SYSTEM", ct);
            var userPromptSnapshot = await _promptService.GetActivePromptSnapshotAsync("JD_ANALYSIS_V2_USER", ct);

            var inputSnapshot = _inputBuilder.Build(job);
            string inputHash = _inputBuilder.ComputeHash(inputSnapshot, systemPromptSnapshot.VersionId, userPromptSnapshot.VersionId);

            // Check if reusable READY run exists for exact same hash
            var existingReady = await _jobAnalysisRepository.GetReusableReadyRunAsync(jobId, inputHash, ct);
            if (existingReady != null)
            {
                return new JobAnalysisStatusDto
                {
                    JobId = jobId,
                    AnalysisRunId = existingReady.Id,
                    InputRevision = existingReady.InputRevision,
                    Status = JobAnalysisStatus.READY,
                    CreatedAt = existingReady.CreatedAt,
                    CompletedAt = existingReady.CompletedAt
                };
            }

            // Check if active PENDING or PROCESSING run exists for exact same hash
            var existingProcessing = await _jobAnalysisRepository.GetActiveProcessingRunAsync(jobId, inputHash, ct);
            if (existingProcessing != null)
            {
                return new JobAnalysisStatusDto
                {
                    JobId = jobId,
                    AnalysisRunId = existingProcessing.Id,
                    InputRevision = existingProcessing.InputRevision,
                    Status = existingProcessing.Status,
                    CreatedAt = existingProcessing.CreatedAt,
                    CompletedAt = existingProcessing.CompletedAt
                };
            }

            // Create new PENDING run
            string rawInputJson = System.Text.Json.JsonSerializer.Serialize(inputSnapshot);
            var newRun = new JobAnalysisRuns
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                InputRevision = job.AnalysisRevision,
                InputHash = inputHash,
                Status = JobAnalysisStatus.PENDING,
                SystemPromptVersionId = systemPromptSnapshot.VersionId,
                UserPromptVersionId = userPromptSnapshot.VersionId,
                SchemaVersion = "jd-analysis/v2",
                RawInputSnapshot = rawInputJson,
                RequestedBy = recruiterId,
                CreatedAt = DateTime.UtcNow
            };

            await _jobAnalysisRepository.AddPendingRunAsync(newRun, ct);

            return new JobAnalysisStatusDto
            {
                JobId = jobId,
                AnalysisRunId = newRun.Id,
                InputRevision = newRun.InputRevision,
                Status = JobAnalysisStatus.PENDING,
                CreatedAt = newRun.CreatedAt
            };
        }

        public async Task<JobAnalysisPreviewDto?> GetPreviewAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default)
        {
            return await _jobAnalysisRepository.GetPreviewAsync(jobId, recruiterId, ct);
        }

        public async Task<JobAnalysisPreviewDto> UpdateDecisionsAsync(Guid jobId, Guid runId, Guid recruiterId, UpdateJobSkillDecisionsDto dto, CancellationToken ct = default)
        {
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
                throw new InvalidOperationException($"{result.ErrorCode}: {result.ErrorMessage}");
            }

            return result.Preview!;
        }

        public async Task<FinalizeJobResponseDto> FinalizeAsync(Guid jobId, Guid recruiterId, FinalizeJobRequestDto dto, CancellationToken ct = default)
        {
            // Determine system review gate configuration
            var config = await _context.SystemConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.ConfigKey == "JobPostingReviewRequired", ct);
            bool reviewRequired = config != null && bool.TryParse(config.ConfigValue, out bool req) && req;

            JobStatus targetStatus = reviewRequired ? JobStatus.PENDING_REVIEW : JobStatus.PUBLISHED;

            var result = await _jobAnalysisRepository.FinalizeAsync(
                jobId,
                dto.AnalysisRunId,
                recruiterId,
                dto.ExpectedJobRevision,
                dto.ExpectedDecisionVersion,
                targetStatus,
                ct);

            if (!result.Success)
            {
                throw new InvalidOperationException($"{result.ErrorCode}: {result.ErrorMessage}");
            }

            return new FinalizeJobResponseDto
            {
                JobId = jobId,
                Status = result.Job!.Status.ToString(),
                PublishedAt = result.Job.PublishedAt,
                SkillCount = result.SkillCount
            };
        }
    }
}
