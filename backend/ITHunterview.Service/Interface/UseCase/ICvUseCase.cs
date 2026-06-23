using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICvUseCase
    {
        Task<CvResponseDto> CreateCvAsync(Guid userId, CreateCvRequestDto request);
        Task<IEnumerable<CvResponseDto>> GetMyCvsAsync(Guid userId);
        Task<CvResponseDto> GetCvByIdAsync(Guid id, Guid userId);
        Task DeleteCvAsync(Guid id, Guid userId);
    }
}
