using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.PromptAdmin;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IPromptAdminUseCase
    {
        Task<PagedResult<PromptDto>> GetPagedPromptsAsync(int page, int size);
        Task<PromptDto> GetPromptHistoryAsync(Guid promptId);
        Task<PromptVersionDto> GetPromptVersionAsync(Guid versionId);
        Task<PromptVersionDto> CreatePromptVersionAsync(Guid promptId, CreatePromptVersionDto dto, Guid adminId);
        Task ActivatePromptVersionAsync(Guid promptId, Guid versionId, Guid adminId);
    }
}
