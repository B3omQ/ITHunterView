using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICandidateEducationRepository
    {
        Task<List<CandidateEducations>> GetByUserIdAsync(Guid userId);
        Task<CandidateEducations?> GetByIdAndUserIdAsync(Guid id, Guid userId);
        Task<CandidateEducations> CreateAsync(CandidateEducations education);
        Task SaveChangesAsync();
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<bool> MajorExistsAsync(int majorId);
        Task<List<Majors>> GetAllMajorsAsync();
    }
}
