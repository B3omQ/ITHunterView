using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICandidateCertificationRepository
    {
        Task<List<CandidateCertifications>> GetByUserIdAsync(Guid userId);
        Task<CandidateCertifications?> GetByIdAndUserIdAsync(Guid id, Guid userId);
        Task<CandidateCertifications> CreateAsync(CandidateCertifications certification);
        Task SaveChangesAsync();
        Task<bool> DeleteAsync(Guid id, Guid userId);
    }
}
