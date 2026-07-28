using System;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICvJobMatchingUseCase
    {
        Task MatchCvWithAllJobsAsync(Guid cvId, Guid userId);
        Task MatchJobWithAllCvsAsync(Guid jobId, Guid userId);

        // API Polling: Đẩy request vào background và trả về Job ID
        Task<Guid> SubmitMatchingJobAsync(Guid userId, ITHunterview.Service.DTOs.Cv.Matching.MatchingRequestDto request);
        
        // Background task
        Task ProcessMatchingJobAsync(Guid jobId, Guid userId, ITHunterview.Service.DTOs.Cv.Matching.MatchingRequestDto request);

        // Lấy kết quả
        Task<ITHunterview.Service.DTOs.Cv.Matching.MatchingResultDto?> GetMatchingResultAsync(Guid jobId, Guid userId);
        Task<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>> GetMatchHistoryAsync(Guid userId, int page, int pageSize, Guid? cvId = null);
        Task<ITHunterview.Service.DTOs.Common.PagedResult<ITHunterview.Service.DTOs.Cv.Matching.MatchHistoryDto>> GetJobMatchHistoryAsync(Guid jobId, Guid recruiterId, int page, int pageSize);
        Task DeleteMatchHistoryAsync(Guid jobId, Guid userId);
        Task<ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateResponseDto> UnlockCandidateCvAsync(Guid recruiterId, ITHunterview.Service.DTOs.Cv.Matching.UnlockCandidateRequestDto dto);
    }
}
