using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICandidateProfileRepository
    {
        /// <summary>Lấy profile theo userId, trả null nếu không tồn tại.</summary>
        Task<CandidateProfiles?> GetByUserIdAsync(Guid userId);

        /// <summary>Lấy profile theo Id (CandidateId).</summary>
        Task<CandidateProfiles?> GetByIdAsync(Guid id);

        /// <summary>Tạo profile mới.</summary>
        Task<CandidateProfiles> CreateAsync(CandidateProfiles profile);

        /// <summary>Lưu thay đổi sau khi mutate entity.</summary>
        Task SaveChangesAsync();
    }
}
