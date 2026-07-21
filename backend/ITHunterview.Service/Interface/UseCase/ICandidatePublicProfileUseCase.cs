using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.CandidateProfile;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidatePublicProfileUseCase
    {
        Task<CandidateFullProfileDto> GetPublicProfileAsync(Guid candidateId);
    }
}
