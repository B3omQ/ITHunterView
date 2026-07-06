using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICandidateSkillRepository
    {
        /// <summary>Danh sách skills của user (kèm tên skill từ master data).</summary>
        Task<List<UserSkills>> GetByUserIdAsync(Guid userId);

        /// <summary>Kiểm tra user đã có skill này chưa.</summary>
        Task<UserSkills?> GetUserSkillAsync(Guid userId, int skillId);

        /// <summary>Thêm skill vào profile.</summary>
        Task<UserSkills> AddAsync(UserSkills userSkill);

        /// <summary>Xóa skill khỏi profile.</summary>
        Task<bool> RemoveAsync(Guid userId, int skillId);

        /// <summary>Tìm kiếm master skills theo keyword.</summary>
        Task<List<Skills>> SearchMasterSkillsAsync(string keyword, List<int> excludeIds, int limit = 20);

        /// <summary>Kiểm tra skill tồn tại trong master data.</summary>
        Task<Skills?> GetMasterSkillByIdAsync(int skillId);

        /// <summary>Lấy toàn bộ active skills cho client cache.</summary>
        Task<List<Skills>> GetAllActiveMasterSkillsAsync();
    }
}
