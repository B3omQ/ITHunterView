using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.LearningPath;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ILearningPathUseCase
    {
        Task<LearningPathResponseDto> GenerateLearningPathAsync(Guid candidateId, GeneratePathRequestDto request);
        Task<LearningPathResponseDto> GenerateFromHistoryAsync(Guid candidateId, GenerateFromHistoryRequestDto request);
        Task<List<LearningPathResponseDto>> GetMyLearningPathsAsync(Guid candidateId);
        Task<LearningPathResponseDto> GetLearningPathByIdAsync(Guid candidateId, Guid id);
        Task DeleteLearningPathAsync(Guid candidateId, Guid id);
    }
}

