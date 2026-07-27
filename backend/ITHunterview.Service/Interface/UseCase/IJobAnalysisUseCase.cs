using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IJobAnalysisUseCase
    {
        Task<JobAnalysisStatusDto> RequestAnalysisAsync(Guid jobId, Guid recruiterId, AnalyzeJobRequestDto dto, CancellationToken ct = default);
        Task<JobAnalysisStatusDto> RetryAnalysisAsync(Guid jobId, Guid runId, Guid recruiterId, AnalyzeJobRequestDto dto, CancellationToken ct = default);
        Task<JobAnalysisPreviewDto?> GetPreviewAsync(Guid jobId, Guid recruiterId, CancellationToken ct = default);
        Task<JobAnalysisPreviewDto> UpdateDecisionsAsync(Guid jobId, Guid runId, Guid recruiterId, UpdateJobSkillDecisionsDto dto, CancellationToken ct = default);
        Task<FinalizeJobResponseDto> FinalizeAsync(Guid jobId, Guid recruiterId, FinalizeJobRequestDto dto, CancellationToken ct = default);
    }
}
