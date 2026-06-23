using ITHunterview.Service.DTOs.CandidateProfile;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateCertificationUseCase
    {
        Task<List<CertificationResponseDto>> GetCertificationsAsync(Guid userId);
        Task<CertificationResponseDto> CreateCertificationAsync(Guid userId, CertificationUpsertRequestDto request);
        Task<CertificationResponseDto> UpdateCertificationAsync(Guid userId, Guid id, CertificationUpsertRequestDto request);
        Task<bool> DeleteCertificationAsync(Guid userId, Guid id);
    }
}
