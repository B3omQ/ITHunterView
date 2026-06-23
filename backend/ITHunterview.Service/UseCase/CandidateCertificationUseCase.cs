using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateCertificationUseCase : ICandidateCertificationUseCase
    {
        private readonly ICandidateCertificationRepository _certRepo;

        public CandidateCertificationUseCase(ICandidateCertificationRepository certRepo)
        {
            _certRepo = certRepo;
        }

        public async Task<List<CertificationResponseDto>> GetCertificationsAsync(Guid userId)
        {
            var list = await _certRepo.GetByUserIdAsync(userId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<CertificationResponseDto> CreateCertificationAsync(Guid userId, CertificationUpsertRequestDto request)
        {
            ValidateRequest(request);

            var entity = new CandidateCertifications
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name,
                IssuingOrganization = request.IssuingOrganization,
                IssueDate = request.IssueDate.HasValue ? request.IssueDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                ExpirationDate = request.ExpirationDate.HasValue ? request.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                CredentialUrl = request.CredentialUrl,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _certRepo.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<CertificationResponseDto> UpdateCertificationAsync(Guid userId, Guid id, CertificationUpsertRequestDto request)
        {
            var entity = await _certRepo.GetByIdAndUserIdAsync(id, userId)
                ?? throw new KeyNotFoundException($"Certification Id={id} không tồn tại.");

            ValidateRequest(request);

            entity.Name = request.Name;
            entity.IssuingOrganization = request.IssuingOrganization;
            entity.IssueDate = request.IssueDate.HasValue ? request.IssueDate.Value.ToDateTime(TimeOnly.MinValue) : null;
            entity.ExpirationDate = request.ExpirationDate.HasValue ? request.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue) : null;
            entity.CredentialUrl = request.CredentialUrl;

            await _certRepo.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteCertificationAsync(Guid userId, Guid id)
        {
            var deleted = await _certRepo.DeleteAsync(id, userId);
            if (!deleted)
                throw new KeyNotFoundException($"Certification Id={id} không tồn tại.");
            return true;
        }

        // ─── Private helpers ───────────────────────────────────────────────────

        private static void ValidateRequest(CertificationUpsertRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Tên chứng chỉ không được để trống.");

            if (request.IssueDate.HasValue && request.ExpirationDate.HasValue
                && request.ExpirationDate < request.IssueDate)
                throw new ArgumentException("Ngày hết hạn phải sau ngày cấp.");
        }

        private static CertificationResponseDto MapToDto(CandidateCertifications c)
        {
            return new CertificationResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                IssuingOrganization = c.IssuingOrganization,
                IssueDate = c.IssueDate.HasValue ? DateOnly.FromDateTime(c.IssueDate.Value) : null,
                ExpirationDate = c.ExpirationDate.HasValue ? DateOnly.FromDateTime(c.ExpirationDate.Value) : null,
                CredentialUrl = c.CredentialUrl
            };
        }
    }
}
