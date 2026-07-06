using ITHunterview.Service.DTOs.CandidateProfile;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateProfileUseCase
    {
        /// <summary>Lấy thông tin Personal Info (Tab 1).</summary>
        Task<PersonalInfoResponseDto> GetPersonalInfoAsync(Guid userId);

        /// <summary>Cập nhật Basic Info.</summary>
        Task<PersonalInfoResponseDto> UpdateBasicInfoAsync(Guid userId, BasicInfoUpdateRequestDto request);

        /// <summary>Cập nhật About Me.</summary>
        Task<PersonalInfoResponseDto> UpdateAboutMeAsync(Guid userId, AboutMeUpdateRequestDto request);

        /// <summary>Cập nhật Social Links.</summary>
        Task<PersonalInfoResponseDto> UpdateSocialLinksAsync(Guid userId, SocialLinksUpdateRequestDto request);

        /// <summary>Bật/tắt visibility với recruiter.</summary>
        Task<bool> SetVisibilityAsync(Guid userId, bool isVisible);

        /// <summary>Upload ảnh đại diện. Controller chịu trách nhiệm validate IFormFile và truyền xuống dưới dạng stream.</summary>
        Task<AvatarUploadResponseDto> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType, long fileSize);

        /// <summary>Lấy dữ liệu Profile Summary (Header, computed).</summary>
        Task<ProfileSummaryResponseDto> GetProfileSummaryAsync(Guid userId);
    }
}
