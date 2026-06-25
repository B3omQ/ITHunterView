using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateExperienceUseCase : ICandidateExperienceUseCase
    {
        private readonly ICandidateExperienceRepository _expRepo;

        public CandidateExperienceUseCase(ICandidateExperienceRepository expRepo)
        {
            _expRepo = expRepo;
        }

        public async Task<List<ExperienceResponseDto>> GetExperiencesAsync(Guid userId)
        {
            var list = await _expRepo.GetByUserIdAsync(userId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<ExperienceResponseDto> CreateExperienceAsync(Guid userId, ExperienceUpsertRequestDto request)
        {
            await ValidateRequestAsync(request);

            var entity = new CandidateExperiences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                CompanyName = request.CompanyName,
                CompanyId = request.CompanyId,
                Location = request.Location,
                EmploymentType = request.EmploymentType ?? Domain.Enums.EmploymentType.FULL_TIME,
                StartDate = request.StartDate.HasValue ? DateTime.SpecifyKind(request.StartDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null,
                EndDate = request.EndDate.HasValue ? DateTime.SpecifyKind(request.EndDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null,
                IsCurrent = request.IsCurrent,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _expRepo.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<ExperienceResponseDto> UpdateExperienceAsync(Guid userId, Guid id, ExperienceUpsertRequestDto request)
        {
            var entity = await _expRepo.GetByIdAndUserIdAsync(id, userId)
                ?? throw new KeyNotFoundException($"Experience Id={id} không tồn tại.");

            await ValidateRequestAsync(request);

            entity.Title = request.Title;
            entity.CompanyName = request.CompanyName;
            entity.CompanyId = request.CompanyId;
            entity.Location = request.Location;
            entity.EmploymentType = request.EmploymentType ?? entity.EmploymentType;
            entity.StartDate = request.StartDate.HasValue ? DateTime.SpecifyKind(request.StartDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
            entity.EndDate = request.EndDate.HasValue ? DateTime.SpecifyKind(request.EndDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
            entity.IsCurrent = request.IsCurrent;
            entity.Description = request.Description;
            entity.UpdatedAt = DateTime.UtcNow;

            await _expRepo.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteExperienceAsync(Guid userId, Guid id)
        {
            var deleted = await _expRepo.DeleteAsync(id, userId);
            if (!deleted)
                throw new KeyNotFoundException($"Experience Id={id} không tồn tại.");
            return true;
        }

        // ─── Private helpers ───────────────────────────────────────────────────

        private async Task ValidateRequestAsync(ExperienceUpsertRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Tiêu đề vị trí không được để trống.");

            if (request.IsCurrent && request.EndDate.HasValue)
                throw new ArgumentException("Công việc hiện tại không được có ngày kết thúc.");

            if (!request.IsCurrent && request.StartDate.HasValue && request.EndDate.HasValue
                && request.EndDate < request.StartDate)
                throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");

            // Validate companyId nếu được gửi lên
            if (request.CompanyId.HasValue)
            {
                var exists = await _expRepo.CompanyExistsAsync(request.CompanyId.Value);
                if (!exists)
                    throw new ArgumentException($"Company Id={request.CompanyId} không tồn tại trong hệ thống.");
            }
        }

        private static ExperienceResponseDto MapToDto(CandidateExperiences e)
        {
            return new ExperienceResponseDto
            {
                Id = e.Id,
                Title = e.Title,
                CompanyName = e.CompanyName,
                CompanyId = e.CompanyId,
                Location = e.Location,
                EmploymentType = e.EmploymentType.ToString(),
                StartDate = e.StartDate.HasValue ? DateOnly.FromDateTime(e.StartDate.Value) : null,
                EndDate = e.EndDate.HasValue ? DateOnly.FromDateTime(e.EndDate.Value) : null,
                IsCurrent = e.IsCurrent,
                Description = e.Description
            };
        }
    }
}
