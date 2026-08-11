using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Optimize;

namespace ITHunterview.Service.Interface.UseCase;

public interface IOptimizeUseCase
{
    Task<CvOptimizationResultDto> CreateSessionAndAnalyzeAsync(Guid userId, string? cvUrl, Guid? cvId);
    Task<CvOptimizationResultDto> GetSessionResultAsync(Guid sessionId);
    Task<PagedResult<OptimizeHistoryItemDto>> GetUserHistoryAsync(Guid userId, int page, int pageSize);
    Task DeleteSessionAsync(Guid userId, Guid sessionId);
    Task<string?> GeneratePreviewAsync(Guid sessionId);
    Task<string> GenerateFinalFileAsync(Guid sessionId);
}
