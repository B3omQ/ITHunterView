using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateEducationUseCase : ICandidateEducationUseCase
    {
        private readonly ICandidateEducationRepository _eduRepo;

        public CandidateEducationUseCase(ICandidateEducationRepository eduRepo)
        {
            _eduRepo = eduRepo;
        }

        public async Task<List<MajorResponseDto>> GetAllMajorsAsync()
        {
            var majors = await _eduRepo.GetAllMajorsAsync();
            return majors.Select(m => new MajorResponseDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code
            }).ToList();
        }

        public async Task<List<EducationResponseDto>> GetEducationsAsync(Guid userId)
        {
            var list = await _eduRepo.GetByUserIdAsync(userId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<EducationResponseDto> CreateEducationAsync(Guid userId, EducationUpsertRequestDto request)
        {
            await ValidateRequestAsync(request);

            var entity = new CandidateEducations
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Degree = request.Degree,
                MajorId = request.MajorId,
                InstitutionName = request.InstitutionName,
                Gpa = request.Gpa,
                MaxGpa = request.MaxGpa,
                StartDate = request.StartDate.HasValue ? request.StartDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                EndDate = request.EndDate.HasValue ? request.EndDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _eduRepo.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<EducationResponseDto> UpdateEducationAsync(Guid userId, Guid id, EducationUpsertRequestDto request)
        {
            var entity = await _eduRepo.GetByIdAndUserIdAsync(id, userId)
                ?? throw new KeyNotFoundException($"Education Id={id} không tồn tại.");

            await ValidateRequestAsync(request);

            entity.Degree = request.Degree;
            entity.MajorId = request.MajorId;
            entity.InstitutionName = request.InstitutionName;
            entity.Gpa = request.Gpa;
            entity.MaxGpa = request.MaxGpa;
            entity.StartDate = request.StartDate.HasValue ? request.StartDate.Value.ToDateTime(TimeOnly.MinValue) : null;
            entity.EndDate = request.EndDate.HasValue ? request.EndDate.Value.ToDateTime(TimeOnly.MinValue) : null;
            entity.Description = request.Description;
            entity.UpdatedAt = DateTime.UtcNow;

            await _eduRepo.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteEducationAsync(Guid userId, Guid id)
        {
            var deleted = await _eduRepo.DeleteAsync(id, userId);
            if (!deleted)
                throw new KeyNotFoundException($"Education Id={id} không tồn tại.");
            return true;
        }

        // ─── Private helpers ───────────────────────────────────────────────────

        private async Task ValidateRequestAsync(EducationUpsertRequestDto request)
        {
            if (request.StartDate.HasValue && request.EndDate.HasValue
                && request.EndDate < request.StartDate)
                throw new ArgumentException("Ngày tốt nghiệp phải sau ngày bắt đầu.");

            if (request.Gpa.HasValue && request.MaxGpa.HasValue && request.Gpa > request.MaxGpa)
                throw new ArgumentException("GPA không được vượt quá MaxGPA.");

            if (request.MajorId.HasValue)
            {
                var exists = await _eduRepo.MajorExistsAsync(request.MajorId.Value);
                if (!exists)
                    throw new ArgumentException($"Major Id={request.MajorId} không tồn tại.");
            }
        }

        private static EducationResponseDto MapToDto(CandidateEducations e)
        {
            return new EducationResponseDto
            {
                Id = e.Id,
                Degree = e.Degree,
                MajorId = e.MajorId,
                InstitutionName = e.InstitutionName,
                Gpa = e.Gpa,
                MaxGpa = e.MaxGpa,
                StartDate = e.StartDate.HasValue ? DateOnly.FromDateTime(e.StartDate.Value) : null,
                EndDate = e.EndDate.HasValue ? DateOnly.FromDateTime(e.EndDate.Value) : null,
                Description = e.Description
            };
        }
    }
}
