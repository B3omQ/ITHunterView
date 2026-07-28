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

        /// <summary>Lấy trạng thái hoàn thiện profile bắt buộc.</summary>
        Task<ProfileCompletionStatusResponseDto> GetProfileCompletionStatusAsync(Guid userId);

        /// <summary>Cập nhật thông tin profile bắt buộc từ màn hình onboarding.</summary>
        Task<ProfileCompletionStatusResponseDto> CompleteOnboardingProfileAsync(Guid userId, OnboardingProfileRequestDto request);

        /// <summary>Nhận thưởng 1.500 coin tân binh khi hoàn thành 100% profile và xác thực email.</summary>
        Task<ProfileCompletionStatusResponseDto> ClaimNewbieRewardAsync(Guid userId);
    }
}
