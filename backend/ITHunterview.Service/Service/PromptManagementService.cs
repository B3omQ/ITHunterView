using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service
{
    public class PromptManagementService : IPromptManagementService
    {
        private readonly ITHunterviewContext _context;
        private readonly ILogger<PromptManagementService> _logger;

        public PromptManagementService(ITHunterviewContext context, ILogger<PromptManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetActivePromptContentAsync(string promptKey)
        {
            var activeVersion = await _context.PromptVersions
                .AsNoTracking()
                .Include(pv => pv.Prompt)
                .Where(pv => pv.Prompt.PromptKey == promptKey && pv.IsActive)
                .FirstOrDefaultAsync();

            if (activeVersion == null)
            {
                _logger.LogWarning($"No active prompt found for key: {promptKey}");
                return string.Empty;
            }

            return activeVersion.Content;
        }

        public async Task<string> GetActivePromptContentWithVariablesAsync(string promptKey, Dictionary<string, string> variables)
        {
            var content = await GetActivePromptContentAsync(promptKey);

            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            foreach (var variable in variables)
            {
                var placeholder = $"[{variable.Key}]";
                if (!content.Contains(placeholder))
                {
                    _logger.LogWarning($"Template for {promptKey} is missing the placeholder {placeholder}. The data might be ignored by the LLM.");
                }
                content = content.Replace(placeholder, variable.Value);
            }

            return content;
        }

        public async Task<PromptSnapshotDto> GetActivePromptSnapshotAsync(string promptKey, CancellationToken ct = default)
        {
            var activeVersions = await _context.PromptVersions
                .AsNoTracking()
                .Include(pv => pv.Prompt)
                .Where(pv => pv.Prompt.PromptKey == promptKey && pv.IsActive)
                .ToListAsync(ct);

            if (activeVersions.Count == 0)
            {
                _logger.LogError($"PROMPT_NOT_CONFIGURED: Active prompt key '{promptKey}' not found.");
                throw new InvalidOperationException($"PROMPT_NOT_CONFIGURED: Active prompt key '{promptKey}' not found.");
            }

            if (activeVersions.Count > 1)
            {
                var ids = string.Join(", ", activeVersions.Select(v => v.Id));
                _logger.LogError($"PROMPT_CONFIGURATION_INVALID: Multiple active versions found for key '{promptKey}': {ids}");
                throw new InvalidOperationException($"PROMPT_CONFIGURATION_INVALID: Multiple active versions found for key '{promptKey}'.");
            }

            var v = activeVersions[0];
            return new PromptSnapshotDto
            {
                PromptId = v.PromptId,
                VersionId = v.Id,
                PromptKey = v.Prompt.PromptKey,
                VersionTag = v.VersionTag,
                Content = v.Content,
                ModelConfig = v.ModelConfig
            };
        }

        public async Task<PromptPairSnapshotDto> GetActivePromptPairSnapshotAsync(
            string systemPromptKey,
            string userPromptKey,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(systemPromptKey))
            {
                throw new ArgumentException("System prompt key is required.", nameof(systemPromptKey));
            }

            if (string.IsNullOrWhiteSpace(userPromptKey))
            {
                throw new ArgumentException("User prompt key is required.", nameof(userPromptKey));
            }

            if (string.Equals(systemPromptKey, userPromptKey, StringComparison.Ordinal))
            {
                throw new ArgumentException("System and user prompt keys must be different.");
            }

            var activeVersions = await _context.PromptVersions
                .AsNoTracking()
                .Include(pv => pv.Prompt)
                .Where(pv => pv.IsActive &&
                    (pv.Prompt.PromptKey == systemPromptKey || pv.Prompt.PromptKey == userPromptKey))
                .ToListAsync(ct);

            var system = GetSingleActiveSnapshot(activeVersions, systemPromptKey);
            var user = GetSingleActiveSnapshot(activeVersions, userPromptKey);

            var systemMetadata = ReadPromptMetadata(system);
            var userMetadata = ReadPromptMetadata(user);

            if (!string.Equals(systemMetadata.Role, "system", StringComparison.Ordinal) ||
                !string.Equals(userMetadata.Role, "user", StringComparison.Ordinal))
            {
                _logger.LogError(
                    "PROMPT_CONFIGURATION_INVALID: prompt pair {SystemPromptKey}/{UserPromptKey} has invalid roles.",
                    systemPromptKey,
                    userPromptKey);
                throw new InvalidOperationException("PROMPT_CONFIGURATION_INVALID: Prompt pair roles are invalid.");
            }

            if (string.IsNullOrWhiteSpace(systemMetadata.Contract) ||
                !string.Equals(systemMetadata.Contract, userMetadata.Contract, StringComparison.Ordinal))
            {
                _logger.LogError(
                    "PROMPT_CONTRACT_MISMATCH: prompt pair {SystemPromptKey}/{UserPromptKey} has incompatible contracts.",
                    systemPromptKey,
                    userPromptKey);
                throw new InvalidOperationException("PROMPT_CONTRACT_MISMATCH: Prompt pair contracts are incompatible.");
            }

            return new PromptPairSnapshotDto
            {
                System = system,
                User = user,
                Contract = systemMetadata.Contract
            };
        }

        public async Task<PromptSnapshotDto> GetPromptSnapshotByVersionIdAsync(Guid versionId, CancellationToken ct = default)
        {
            var version = await _context.PromptVersions
                .AsNoTracking()
                .Include(pv => pv.Prompt)
                .FirstOrDefaultAsync(pv => pv.Id == versionId, ct);

            if (version == null)
            {
                _logger.LogError($"PROMPT_VERSION_NOT_FOUND: Prompt version '{versionId}' not found.");
                throw new InvalidOperationException($"PROMPT_VERSION_NOT_FOUND: Prompt version '{versionId}' not found.");
            }

            return new PromptSnapshotDto
            {
                PromptId = version.PromptId,
                VersionId = version.Id,
                PromptKey = version.Prompt.PromptKey,
                VersionTag = version.VersionTag,
                Content = version.Content,
                ModelConfig = version.ModelConfig
            };
        }

        private PromptSnapshotDto GetSingleActiveSnapshot(IEnumerable<PromptVersions> activeVersions, string promptKey)
        {
            var matches = activeVersions
                .Where(v => v.Prompt.PromptKey == promptKey)
                .ToList();

            if (matches.Count == 0)
            {
                _logger.LogError("PROMPT_NOT_CONFIGURED: Active prompt key '{PromptKey}' not found.", promptKey);
                throw new InvalidOperationException($"PROMPT_NOT_CONFIGURED: Active prompt key '{promptKey}' not found.");
            }

            if (matches.Count > 1)
            {
                _logger.LogError("PROMPT_CONFIGURATION_INVALID: Multiple active versions found for key '{PromptKey}'.", promptKey);
                throw new InvalidOperationException($"PROMPT_CONFIGURATION_INVALID: Multiple active versions found for key '{promptKey}'.");
            }

            var version = matches[0];
            return new PromptSnapshotDto
            {
                PromptId = version.PromptId,
                VersionId = version.Id,
                PromptKey = version.Prompt.PromptKey,
                VersionTag = version.VersionTag,
                Content = version.Content,
                ModelConfig = version.ModelConfig ?? "{}"
            };
        }

        private static PromptMetadata ReadPromptMetadata(PromptSnapshotDto snapshot)
        {
            try
            {
                using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(snapshot.ModelConfig) ? "{}" : snapshot.ModelConfig);
                var root = document.RootElement;

                return new PromptMetadata(
                    GetStringProperty(root, "contract"),
                    GetStringProperty(root, "role"));
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"PROMPT_CONFIGURATION_INVALID: ModelConfig for '{snapshot.PromptKey}' is not valid JSON.");
            }
        }

        private static string GetStringProperty(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : string.Empty;
        }

        private sealed record PromptMetadata(string Contract, string Role);
    }
}
