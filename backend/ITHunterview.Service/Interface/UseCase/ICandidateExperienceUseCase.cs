using ITHunterview.Service.DTOs.CandidateProfile;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateExperienceUseCase
    {
        Task<List<ExperienceResponseDto>> GetExperiencesAsync(Guid userId);
        Task<ExperienceResponseDto> CreateExperienceAsync(Guid userId, ExperienceUpsertRequestDto request);
        Task<ExperienceResponseDto> UpdateExperienceAsync(Guid userId, Guid id, ExperienceUpsertRequestDto request);
        Task<bool> DeleteExperienceAsync(Guid userId, Guid id);
    }
}
