using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public sealed class PromptSnapshotDto
    {
        public Guid PromptId { get; set; }
        public Guid VersionId { get; set; }
        public string PromptKey { get; set; } = string.Empty;
        public string VersionTag { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ModelConfig { get; set; } = "{}";
    }

    public sealed class PromptPairSnapshotDto
    {
        public PromptSnapshotDto System { get; set; } = new();
        public PromptSnapshotDto User { get; set; } = new();
        public string Contract { get; set; } = string.Empty;
    }

    public interface IPromptManagementService
    {
        Task<string> GetActivePromptContentAsync(string promptKey);
        Task<string> GetActivePromptContentWithVariablesAsync(string promptKey, Dictionary<string, string> variables);
        Task<PromptSnapshotDto> GetActivePromptSnapshotAsync(string promptKey, CancellationToken ct = default);
        Task<PromptPairSnapshotDto> GetActivePromptPairSnapshotAsync(string systemPromptKey, string userPromptKey, CancellationToken ct = default);
        Task<PromptSnapshotDto> GetPromptSnapshotByVersionIdAsync(Guid versionId, CancellationToken ct = default);
    }
}
