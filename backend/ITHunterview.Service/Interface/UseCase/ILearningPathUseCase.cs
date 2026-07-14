using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.LearningPath;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ILearningPathUseCase
    {
        Task<LearningPathResponseDto> GenerateLearningPathAsync(Guid candidateId, GeneratePathRequestDto request);
        Task<LearningPathResponseDto> GenerateFromCvJdAsync(Guid candidateId, GenerateFromCvJdRequestDto request);
        Task<LearningPathResponseDto> GenerateFromInterviewAsync(Guid candidateId, GenerateFromInterviewRequestDto request);
        Task<List<LearningPathResponseDto>> GetMyLearningPathsAsync(Guid candidateId);
        Task<LearningPathResponseDto> GetLearningPathByIdAsync(Guid candidateId, Guid id);
        Task DeleteLearningPathAsync(Guid candidateId, Guid id);
        Task<LearningPathResponseDto> ToggleModuleCompletionAsync(Guid candidateId, Guid pathId, int moduleIndex);
        Task<HistoryContextPreviewDto> PreviewHistoryContextAsync(Guid candidateId, string type, Guid? sourceId);
    }
}

