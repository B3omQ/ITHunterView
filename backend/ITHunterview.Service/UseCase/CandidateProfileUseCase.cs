using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateProfileUseCase : ICandidateProfileUseCase
    {
        private readonly ICandidateProfileRepository _profileRepo;
        private readonly ICandidateExperienceRepository _expRepo;
        private readonly ICandidateEducationRepository _eduRepo;
        private readonly ICandidateCertificationRepository _certRepo;
        private readonly ICandidateSkillRepository _skillRepo;

        public CandidateProfileUseCase(
            ICandidateProfileRepository profileRepo,
            ICandidateExperienceRepository expRepo,
            ICandidateEducationRepository eduRepo,
            ICandidateCertificationRepository certRepo,
            ICandidateSkillRepository skillRepo)
        {
            _profileRepo = profileRepo;
            _expRepo = expRepo;
            _eduRepo = eduRepo;
            _certRepo = certRepo;
            _skillRepo = skillRepo;
        }

        // ─── Personal Info ─────────────────────────────────────────────────────

        public async Task<PersonalInfoResponseDto> GetPersonalInfoAsync(Guid userId)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            return MapToPersonalInfoDto(profile);
        }

        public async Task<PersonalInfoResponseDto> UpdatePersonalInfoAsync(Guid userId, PersonalInfoUpdateRequestDto request)
        {
            if (request.AboutMe != null && request.AboutMe.Length > 500)
                throw new ArgumentException("AboutMe không được vượt quá 500 ký tự.");

            var profile = await GetOrCreateProfileAsync(userId);

            profile.FirstName = request.FirstName;
            profile.LastName = request.LastName;
            profile.Phone = request.Phone;
            profile.Location = request.Location;
            profile.AboutMe = request.AboutMe;
            profile.PortfolioUrl = request.PortfolioUrl;
            profile.LinkedinUrl = request.LinkedInUrl;
            profile.GithubUrl = request.GithubUrl;

            // Track updated_at on the user record
            if (profile.User != null)
                profile.User.UpdatedAt = DateTime.UtcNow;

            await _profileRepo.SaveChangesAsync();

            return MapToPersonalInfoDto(profile);
        }

        // ─── Visibility ────────────────────────────────────────────────────────

        public async Task<bool> SetVisibilityAsync(Guid userId, bool isVisible)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            profile.IsVisibleToRecruiters = isVisible;
            await _profileRepo.SaveChangesAsync();

            return isVisible;
        }

        // ─── Avatar ────────────────────────────────────────────────────────────

        public async Task<AvatarUploadResponseDto> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType, long fileSize)
        {
            if (fileStream == null || fileSize == 0)
                throw new ArgumentException("File ảnh không hợp lệ.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(contentType.ToLower()))
                throw new ArgumentException("Chỉ chấp nhận ảnh định dạng JPEG, PNG, WebP hoặc GIF.");

            const long maxSizeBytes = 5 * 1024 * 1024; // 5 MB
            if (fileSize > maxSizeBytes)
                throw new ArgumentException("Ảnh không được vượt quá 5MB.");

            // TODO: Replace with actual file storage service (S3/Azure/GCP)
            var ext = Path.GetExtension(fileName);
            var avatarUrl = $"/uploads/avatars/{userId}{ext}";

            var profile = await GetOrCreateProfileAsync(userId);
            profile.AvatarUrl = avatarUrl;
            await _profileRepo.SaveChangesAsync();

            return new AvatarUploadResponseDto { AvatarUrl = avatarUrl };
        }

        // ─── Profile Summary ───────────────────────────────────────────────────

        public async Task<ProfileSummaryResponseDto> GetProfileSummaryAsync(Guid userId)
        {
            var profile = await GetOrCreateProfileAsync(userId);

            // Suy luận currentTitle từ experience is_current = true
            var experiences = await _expRepo.GetByUserIdAsync(userId);
            var currentExp = experiences.FirstOrDefault(e => e.IsCurrent)
                          ?? experiences.OrderByDescending(e => e.StartDate).FirstOrDefault();
            var currentTitle = currentExp?.Title;

            // Tính % completion (6 sections × ~16.67% mỗi section, làm tròn xuống bội 5)
            var skills = await _skillRepo.GetByUserIdAsync(userId);
            var educations = await _eduRepo.GetByUserIdAsync(userId);
            var certs = await _certRepo.GetByUserIdAsync(userId);

            var completedSections = 0;
            if (!string.IsNullOrWhiteSpace(profile.FirstName) || !string.IsNullOrWhiteSpace(profile.LastName))
                completedSections++;
            if (!string.IsNullOrWhiteSpace(profile.AboutMe))
                completedSections++;
            if (skills.Any())
                completedSections++;
            if (experiences.Any())
                completedSections++;
            if (educations.Any())
                completedSections++;
            if (certs.Any())
                completedSections++;

            const int totalSections = 6;
            var percentage = (int)Math.Round((double)completedSections / totalSections * 100);
            // Làm tròn xuống bội 5 gần nhất
            percentage = (percentage / 5) * 5;

            var remaining = totalSections - completedSections;
            var hint = remaining == 0
                ? "Your profile is complete!"
                : $"Add {remaining} more section{(remaining > 1 ? "s" : "")} to reach {Math.Min(percentage + 20, 100)}%";

            // lastSavedAt = max updated_at từ tất cả related records
            var timestamps = new List<DateTime?>();
            timestamps.Add(profile.User?.UpdatedAt);
            timestamps.AddRange(experiences.Select(e => (DateTime?)e.UpdatedAt));
            timestamps.AddRange(educations.Select(e => (DateTime?)e.UpdatedAt));
            var lastSavedAt = timestamps.Where(t => t.HasValue).Max(t => t);

            return new ProfileSummaryResponseDto
            {
                Id = profile.Id,
                FullName = string.Join(" ", new[] { profile.FirstName, profile.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
                AvatarUrl = profile.AvatarUrl,
                CurrentTitle = currentTitle,
                Location = profile.Location,
                IsVisibleToRecruiters = profile.IsVisibleToRecruiters,
                ProfileCompletionPercentage = percentage,
                CompletionHint = hint,
                LastSavedAt = lastSavedAt
            };
        }

        // ─── Private helpers ───────────────────────────────────────────────────

        private async Task<CandidateProfiles> GetOrCreateProfileAsync(Guid userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile != null)
                return profile;

            // Auto-create nếu chưa có (new user)
            var newProfile = new CandidateProfiles { UserId = userId };
            return await _profileRepo.CreateAsync(newProfile);
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
    }
}
