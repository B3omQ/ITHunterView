using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.MasterData;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateEducationUseCase
    {
        Task<List<MajorResponseDto>> GetAllMajorsAsync();
        Task<List<EducationResponseDto>> GetEducationsAsync(Guid userId);
        Task<EducationResponseDto> CreateEducationAsync(Guid userId, EducationUpsertRequestDto request);
        Task<EducationResponseDto> UpdateEducationAsync(Guid userId, Guid id, EducationUpsertRequestDto request);
        Task<bool> DeleteEducationAsync(Guid userId, Guid id);
    }
}
