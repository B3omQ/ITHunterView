using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.CvOptimizer;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICvOptimizerUseCase
    {
        Task<CvOptimizationResponseDto> OptimizeCvAsync(Guid candidateId, OptimizeCvRequestDto request);
        Task<List<CvOptimizationResponseDto>> GetMyOptimizationHistoryAsync(Guid candidateId);
        Task<CvOptimizationResponseDto> GetOptimizationByIdAsync(Guid candidateId, Guid id);
    }
}
