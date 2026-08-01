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
        Task<Prompts?> GetPromptWithHistoryByKeyAsync(string promptKey);
        Task<PromptVersions?> GetPromptVersionAsync(Guid versionId);
        Task<PromptVersions> CreatePromptVersionAsync(PromptVersions newVersion, bool makeActive);
        Task ActivatePromptVersionAsync(Guid promptId, Guid versionId);
        Task ActivatePromptPairAsync(Guid systemPromptId, Guid systemVersionId, Guid userPromptId, Guid userVersionId);
    }
}
