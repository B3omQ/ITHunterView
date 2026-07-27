using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.Interface.Persistence
{
    public class JobAnalysisRequestContext
    {
        public JobPostings Job { get; set; } = null!;
        public bool IsCompanyVerified { get; set; }
    }

    public class ApplyDecisionResult
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public JobAnalysisPreviewDto? Preview { get; set; }
    }

    public class FinalizeJobResult
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public JobPostings? Job { get; set; }
        public int SkillCount { get; set; }
    }

    public interface IJobAnalysisRepository
    {
        Task<JobAnalysisRequestContext?> GetRequestContextAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default);
        Task<JobAnalysisRuns?> GetRunAsync(Guid runId, CancellationToken ct = default);
        Task<JobAnalysisRuns?> FindByIdempotencyKeyAsync(Guid jobId, string key, CancellationToken ct = default);
        Task<JobAnalysisRuns?> FindReusableRunAsync(Guid jobId, int revision, string inputHash, CancellationToken ct = default);
        Task<bool> ActivateReusableRunAsync(Guid jobId, Guid runId, int expectedRevision, CancellationToken ct = default);
        Task<JobAnalysisRuns?> GetActiveProcessingRunAsync(Guid jobId, string inputHash, CancellationToken ct = default);
        Task<int> GetNextAttemptNumberAsync(Guid jobId, int revision, CancellationToken ct = default);
        Task<JobAnalysisRuns> AddPendingRunAsync(JobAnalysisRuns run, CancellationToken ct = default);
        Task<JobAnalysisRuns> CreatePendingRunAsync(JobAnalysisRuns run, CancellationToken ct = default);
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
        Task MarkSupersededAsync(Guid runId, CancellationToken ct = default);
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
            bool confirmNoStandardSkills,
            bool reviewRequired,
            CancellationToken ct = default);
    }
}
