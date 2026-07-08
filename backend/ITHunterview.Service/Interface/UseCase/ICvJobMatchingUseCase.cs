using System;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICvJobMatchingUseCase
    {
        Task MatchCvWithAllJobsAsync(Guid cvId);
        Task MatchJobWithAllCvsAsync(Guid jobId);

        // API Polling: Đẩy request vào background và trả về Job ID
        Task<Guid> SubmitMatchingJobAsync(Guid userId, ITHunterview.Service.DTOs.Cv.Matching.MatchingRequestDto request);
        
        // Background task
        Task ProcessMatchingJobAsync(Guid jobId, Guid userId, ITHunterview.Service.DTOs.Cv.Matching.MatchingRequestDto request);

        // Lấy kết quả
        Task<ITHunterview.Service.DTOs.Cv.Matching.MatchingResultDto?> GetMatchingResultAsync(Guid jobId, Guid userId);
    }
}
