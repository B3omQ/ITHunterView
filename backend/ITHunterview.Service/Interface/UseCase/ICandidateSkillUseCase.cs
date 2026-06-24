using ITHunterview.Service.DTOs.CandidateProfile;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateSkillUseCase
    {
        /// <summary>Danh sách skills của user.</summary>
        Task<(List<SkillResponseDto> Skills, int TotalCount)> GetSkillsAsync(Guid userId);

        /// <summary>Tìm kiếm master skills (autocomplete), loại trừ skills user đã có nếu cần.</summary>
        Task<List<SkillSearchResponseDto>> SearchSkillsAsync(string keyword, bool excludeOwned, Guid userId);

        /// <summary>Thêm skill vào profile.</summary>
        Task<SkillResponseDto> AddSkillAsync(Guid userId, SkillAddRequestDto request);

        /// <summary>Xóa skill khỏi profile.</summary>
        Task<bool> RemoveSkillAsync(Guid userId, int skillId);
    }
}
