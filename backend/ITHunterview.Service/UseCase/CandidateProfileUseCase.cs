using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Interface.Service;

namespace ITHunterview.Service.UseCase
{
    public class CandidateProfileUseCase : ICandidateProfileUseCase
    {
        private readonly ICandidateProfileRepository _profileRepo;
        private readonly ICandidateExperienceRepository _expRepo;
        private readonly ICandidateEducationRepository _eduRepo;
        private readonly ICandidateCertificationRepository _certRepo;
        private readonly ICandidateSkillRepository _skillRepo;
        private readonly IFileUploadService _fileUploadService;
        private readonly ICvRepository _cvRepository;
        private readonly ICvUseCase _cvUseCase;
        private readonly IUserRepository _userRepo;
        private readonly IWalletUseCase _walletUseCase;

        public CandidateProfileUseCase(
            ICandidateProfileRepository profileRepo,
            ICandidateExperienceRepository expRepo,
            ICandidateEducationRepository eduRepo,
            ICandidateCertificationRepository certRepo,
            ICandidateSkillRepository skillRepo,
            IFileUploadService fileUploadService,
            ICvRepository cvRepository,
            ICvUseCase cvUseCase,
            IUserRepository userRepo,
            IWalletUseCase walletUseCase)
        {
            _profileRepo = profileRepo;
            _expRepo = expRepo;
            _eduRepo = eduRepo;
            _certRepo = certRepo;
            _skillRepo = skillRepo;
            _fileUploadService = fileUploadService;
            _cvRepository = cvRepository;
            _cvUseCase = cvUseCase;
            _userRepo = userRepo;
            _walletUseCase = walletUseCase;
        }

        // ─── Personal Info ─────────────────────────────────────────────────────

        public async Task<PersonalInfoResponseDto> GetPersonalInfoAsync(Guid userId)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            return MapToPersonalInfoDto(profile);
        }

        public async Task<PersonalInfoResponseDto> UpdateBasicInfoAsync(Guid userId, BasicInfoUpdateRequestDto request)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            profile.FirstName = request.FirstName;
            profile.LastName = request.LastName;
            profile.Phone = request.Phone;
            profile.Location = request.Location;

            if (profile.User != null)
                profile.User.UpdatedAt = DateTime.UtcNow;

            await _profileRepo.SaveChangesAsync();

            return MapToPersonalInfoDto(profile);
        }

        public async Task<PersonalInfoResponseDto> UpdateAboutMeAsync(Guid userId, AboutMeUpdateRequestDto request)
        {
            if (request.AboutMe != null && request.AboutMe.Length > 500)
                throw new ArgumentException("AboutMe không được vượt quá 500 ký tự.");

            var profile = await GetOrCreateProfileAsync(userId);

            profile.AboutMe = request.AboutMe;

            if (profile.User != null)
                profile.User.UpdatedAt = DateTime.UtcNow;

            await _profileRepo.SaveChangesAsync();

            return MapToPersonalInfoDto(profile);
        }

        public async Task<PersonalInfoResponseDto> UpdateSocialLinksAsync(Guid userId, SocialLinksUpdateRequestDto request)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            profile.PortfolioUrl = request.PortfolioUrl;
            profile.LinkedinUrl = request.LinkedInUrl;
            profile.GithubUrl = request.GithubUrl;

            if (profile.User != null)
                profile.User.UpdatedAt = DateTime.UtcNow;

            await _profileRepo.SaveChangesAsync();

            return MapToPersonalInfoDto(profile);
        }

        // ─── Visibility ────────────────────────────────────────────────────────

        public async Task<bool> SetVisibilityAsync(Guid userId, bool isVisible)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            if (isVisible)
            {
                bool hasPrimary = await _cvRepository.HasPrimaryCvAsync(userId);
                if (!hasPrimary)
                {
                    throw new InvalidOperationException("Bạn cần phải thiết lập CV Chính (Primary CV) trước khi hiển thị hồ sơ.");
                }

                var cvs = await _cvRepository.GetByUserIdAsync(userId);
                var primaryCv = cvs.FirstOrDefault(c => c.IsPrimary);
                if (primaryCv != null && primaryCv.ParseStatus == "PENDING")
                {
                    bool isLocked = await _cvRepository.TryLockCvForParsingAsync(primaryCv.Id);
                    if (isLocked)
                    {
                        // Fire and forget
                        _ = Task.Run(() => _cvUseCase.ParseCvBackgroundAsync(primaryCv.Id, primaryCv.RawText, primaryCv.FileUrl));
                    }
                }
            }

            profile.IsVisibleToRecruiters = isVisible;
            await _profileRepo.SaveChangesAsync();

            return isVisible;
        }

        // ─── Avatar ────────────────────────────────────────────────────────────

        public async Task<AvatarUploadResponseDto> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType, long fileSize)
        {
            if (fileStream == null || fileSize == 0)
                throw new ArgumentException("File ảnh không hợp lệ.");

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(contentType.ToLower()))
                throw new ArgumentException("Chỉ chấp nhận ảnh định dạng JPG, JPEG, PNG, hoặc WebP.");

            const long maxSizeBytes = 3 * 1024 * 1024; // 3 MB
            if (fileSize > maxSizeBytes)
                throw new ArgumentException("Ảnh không được vượt quá 3MB.");

            var profile = await GetOrCreateProfileAsync(userId);

            // Xóa ảnh cũ trên Cloudinary (nếu có)
            if (!string.IsNullOrEmpty(profile.AvatarUrl))
            {
                var oldPublicId = ExtractPublicIdFromUrl(profile.AvatarUrl);
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    await _fileUploadService.DeleteFileAsync(oldPublicId);
                }
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var publicId = $"{userId}_{timestamp}";

            var avatarUrl = await _fileUploadService.UploadAvatarAsync(
                fileStream, 
                fileName, 
                "job_seeking_system/avatars", 
                publicId);

            profile.AvatarUrl = avatarUrl;
            await _profileRepo.SaveChangesAsync();

            return new AvatarUploadResponseDto { AvatarUrl = avatarUrl };
        }

        // ─── Profile Summary ───────────────────────────────────────────────────

        public async Task<ProfileSummaryResponseDto> GetProfileSummaryAsync(Guid userId)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            return new ProfileSummaryResponseDto
            {
                Id = profile.Id,
                FullName = string.Join(" ", new[] { profile.FirstName, profile.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
                AvatarUrl = profile.AvatarUrl,
                Location = profile.Location,
                IsVisibleToRecruiters = profile.IsVisibleToRecruiters
            };
        }

        // ─── Onboarding & Completion Gate ──────────────────────────────────────

        public async Task<ProfileCompletionStatusResponseDto> GetProfileCompletionStatusAsync(Guid userId)
        {
            var profile = await GetOrCreateProfileAsync(userId);
            var evaluation = EvaluateProfileCompletion(profile);

            if (evaluation.IsComplete && !profile.IsProfileComplete)
            {
                profile.IsProfileComplete = true;
                profile.ProfileCompletedAt = DateTime.UtcNow;
                await _profileRepo.SaveChangesAsync();
            }

            await CheckAndAwardNewbieBonusAsync(profile, evaluation);

            return evaluation;
        }

        public async Task<ProfileCompletionStatusResponseDto> CompleteOnboardingProfileAsync(Guid userId, OnboardingProfileRequestDto request)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                profile.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName))
                profile.LastName = request.LastName;
            if (!string.IsNullOrWhiteSpace(request.Phone))
                profile.Phone = request.Phone;
            if (!string.IsNullOrWhiteSpace(request.Location))
                profile.Location = request.Location;

            if (profile.User != null)
                profile.User.UpdatedAt = DateTime.UtcNow;

            var evaluation = EvaluateProfileCompletion(profile);
            
            profile.IsProfileComplete = evaluation.IsComplete;
            if (evaluation.IsComplete && profile.ProfileCompletedAt == null)
            {
                profile.ProfileCompletedAt = DateTime.UtcNow;
            }
            else if (!evaluation.IsComplete)
            {
                profile.ProfileCompletedAt = null;
            }

            await _profileRepo.SaveChangesAsync();

            await CheckAndAwardNewbieBonusAsync(profile, evaluation);

            return evaluation;
        }

        public async Task<ProfileCompletionStatusResponseDto> ClaimNewbieRewardAsync(Guid userId)
        {
            var profile = await GetOrCreateProfileAsync(userId);
            var status = EvaluateProfileCompletion(profile);

            if (!status.IsEmailVerified)
            {
                throw new ArgumentException("Bạn cần xác thực email trước khi nhận phần thưởng này.");
            }
            if (!status.IsComplete)
            {
                throw new ArgumentException("Bạn cần hoàn thiện 100% hồ sơ trước khi nhận phần thưởng này.");
            }
            if (profile.IsNewbieRewardClaimed)
            {
                throw new ArgumentException("Bạn đã nhận phần thưởng tân binh 1.500 coin trước đó rồi.");
            }

            await CheckAndAwardNewbieBonusAsync(profile, status);
            return status;
        }

        // ─── Private helpers ───────────────────────────────────────────────────

        private static string? ExtractPublicIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.Contains("cloudinary.com")) return null;
            
            var parts = url.Split("upload/");
            if (parts.Length < 2) return null;
            
            var afterUpload = parts[1]; // e.g. v1234567/job_seeking_system/avatars/abc.jpg
            var versionSlashIndex = afterUpload.IndexOf('/');
            if (versionSlashIndex == -1) return null;
            
            var publicIdWithExt = afterUpload.Substring(versionSlashIndex + 1);
            var lastDotIndex = publicIdWithExt.LastIndexOf('.');
            
            if (lastDotIndex != -1)
            {
                return publicIdWithExt.Substring(0, lastDotIndex);
            }
            return publicIdWithExt;
        }

        private async Task<CandidateProfiles> GetOrCreateProfileAsync(Guid userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile == null)
            {
                // Auto-create nếu chưa có (new user)
                var newProfile = new CandidateProfiles { UserId = userId };
                await _profileRepo.CreateAsync(newProfile);
                profile = await _profileRepo.GetByUserIdAsync(userId) ?? newProfile;
            }

            if (profile.User == null)
            {
                profile.User = (await _userRepo.GetUserByIdAsync(userId))!;
            }

            return profile;
        }

        private static PersonalInfoResponseDto MapToPersonalInfoDto(CandidateProfiles profile)
        {
            return new PersonalInfoResponseDto
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.User?.Email,
                Phone = profile.Phone,
                Location = profile.Location,
                AboutMe = profile.AboutMe,
                PortfolioUrl = profile.PortfolioUrl,
                LinkedInUrl = profile.LinkedinUrl,
                GithubUrl = profile.GithubUrl,
                UpdatedAt = profile.User?.UpdatedAt
            };
        }

        private ProfileCompletionStatusResponseDto EvaluateProfileCompletion(CandidateProfiles profile)
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(profile.FirstName)) missingFields.Add("firstName");
            if (string.IsNullOrWhiteSpace(profile.LastName)) missingFields.Add("lastName");
            if (string.IsNullOrWhiteSpace(profile.Phone)) missingFields.Add("phone");
            if (string.IsNullOrWhiteSpace(profile.Location)) missingFields.Add("location");

            int totalRequiredFields = 4;
            int filledFields = totalRequiredFields - missingFields.Count;
            int percentage = (int)Math.Round((double)filledFields / totalRequiredFields * 100);

            return new ProfileCompletionStatusResponseDto
            {
                IsComplete = missingFields.Count == 0,
                MissingFields = missingFields,
                CompletionPercentage = percentage,
                IsEmailVerified = profile.User?.Status == Domain.Enums.UserStatus.ACTIVE,
                IsNewbieRewardClaimed = profile.IsNewbieRewardClaimed
            };
        }

        private async Task CheckAndAwardNewbieBonusAsync(CandidateProfiles profile, ProfileCompletionStatusResponseDto status)
        {
            if (status.IsComplete && status.IsEmailVerified && !profile.IsNewbieRewardClaimed)
            {
                profile.IsNewbieRewardClaimed = true;
                await _profileRepo.SaveChangesAsync();
                await _walletUseCase.AddBonusCoinsAsync(profile.UserId, 1500, "Thưởng tân binh hoàn thành 100% hồ sơ và xác thực email (1,500 Coin)");
                status.IsNewbieRewardClaimed = true;
            }
        }
    }
}
