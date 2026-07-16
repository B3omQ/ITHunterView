using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.LearningPath;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ILearningPathUseCase
    {
        Task<LearningPathResponseDto> GenerateLearningPathAsync(Guid candidateId, GeneratePathRequestDto request);
        Task<ExtractSfiaProfileResponseDto> ExtractFromCvJdAsync(Guid candidateId, Guid matchScoreId);
        Task<ExtractSfiaProfileResponseDto> ExtractFromInterviewAsync(Guid candidateId, Guid sessionId);
        Task<List<LearningPathResponseDto>> GetMyLearningPathsAsync(Guid candidateId);
        Task<LearningPathResponseDto> GetLearningPathByIdAsync(Guid candidateId, Guid id);
        Task DeleteLearningPathAsync(Guid candidateId, Guid id);
        Task<LearningPathResponseDto> ToggleTaskCompletionAsync(Guid candidateId, Guid pathId, int moduleIndex, int taskIndex);
        Task<HistoryContextPreviewDto> PreviewHistoryContextAsync(Guid candidateId, string type, Guid? sourceId);
        Task<List<TargetRoleResponseDto>> GetTargetRolesAsync();
    }
}

