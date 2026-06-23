using ITHunterview.Service.DTOs.CandidateProfile;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateProfileUseCase
    {
        /// <summary>Lấy thông tin Personal Info (Tab 1).</summary>
        Task<PersonalInfoResponseDto> GetPersonalInfoAsync(Guid userId);

        /// <summary>Cập nhật Personal Info.</summary>
        Task<PersonalInfoResponseDto> UpdatePersonalInfoAsync(Guid userId, PersonalInfoUpdateRequestDto request);

        /// <summary>Bật/tắt visibility với recruiter.</summary>
        Task<bool> SetVisibilityAsync(Guid userId, bool isVisible);

        /// <summary>Upload ảnh đại diện. Controller chịu trách nhiệm validate IFormFile và truyền xuống dưới dạng stream.</summary>
        Task<AvatarUploadResponseDto> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType, long fileSize);

        /// <summary>Lấy dữ liệu Profile Summary (Header, computed).</summary>
        Task<ProfileSummaryResponseDto> GetProfileSummaryAsync(Guid userId);
    }
}
