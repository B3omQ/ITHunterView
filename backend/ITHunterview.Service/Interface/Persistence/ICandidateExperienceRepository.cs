using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICandidateExperienceRepository
    {
        Task<List<CandidateExperiences>> GetByUserIdAsync(Guid userId);
        Task<CandidateExperiences?> GetByIdAndUserIdAsync(Guid id, Guid userId);
        Task<CandidateExperiences> CreateAsync(CandidateExperiences experience);
        Task SaveChangesAsync();
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<bool> CompanyExistsAsync(Guid companyId);
    }
}
