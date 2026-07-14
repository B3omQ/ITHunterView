using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IPromptAdminRepository
    {
        Task<(IEnumerable<Prompts> Prompts, int TotalCount)> GetPagedPromptsAsync(int page, int size);
        Task<Prompts?> GetPromptWithHistoryAsync(Guid promptId);
        Task<PromptVersions?> GetPromptVersionAsync(Guid versionId);
        Task<PromptVersions> CreatePromptVersionAsync(PromptVersions newVersion, bool makeActive);
        Task ActivatePromptVersionAsync(Guid promptId, Guid versionId);
    }
}
