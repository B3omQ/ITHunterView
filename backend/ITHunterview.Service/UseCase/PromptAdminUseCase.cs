using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.PromptAdmin;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class PromptAdminUseCase : IPromptAdminUseCase
    {
        private readonly IPromptAdminRepository _promptRepository;

        public PromptAdminUseCase(IPromptAdminRepository promptRepository)
        {
            _promptRepository = promptRepository;
        }

        public async Task<PagedResult<PromptDto>> GetPagedPromptsAsync(int page, int size)
        {
            var (prompts, totalCount) = await _promptRepository.GetPagedPromptsAsync(page, size);

            var dtos = prompts.Select(p =>
            {
                var activeVersion = p.Versions.FirstOrDefault();
                return new PromptDto
                {
                    Id = p.Id,
                    PromptKey = p.PromptKey,
                    Description = p.Description,
                    ActiveVersionTag = activeVersion?.VersionTag,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                };
            }).ToList();

            return new PagedResult<PromptDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = size
            };
        }

        public async Task<PromptDto> GetPromptHistoryAsync(Guid promptId)
        {
            var prompt = await _promptRepository.GetPromptWithHistoryAsync(promptId);
            if (prompt == null)
            {
                throw new KeyNotFoundException("Prompt not found");
            }

            return MapToPromptDto(prompt);
        }

        public async Task<CvAnalysisPromptPairDto> GetCvAnalysisPromptPairAsync()
        {
            // Both repository calls use the same scoped DbContext. They must be
            // awaited sequentially because EF Core does not permit concurrent
            // operations on a single context instance.
            var systemPrompt = await _promptRepository.GetPromptWithHistoryByKeyAsync(
                CvAnalysisPromptContract.SystemPromptKey);
            var userPrompt = await _promptRepository.GetPromptWithHistoryByKeyAsync(
                CvAnalysisPromptContract.UserPromptKey);

            if (systemPrompt == null || userPrompt == null)
            {
                throw new KeyNotFoundException("CV analysis prompt pair is not configured");
            }

            return new CvAnalysisPromptPairDto
            {
                SystemPrompt = MapToPromptDto(systemPrompt),
                UserPrompt = MapToPromptDto(userPrompt)
            };
        }

        public async Task<JdAnalysisPromptPairDto> GetJdAnalysisPromptPairAsync()
        {
            var systemPrompt = await _promptRepository.GetPromptWithHistoryByKeyAsync(
                JdAnalysisPromptContract.SystemPromptKey);
            var userPrompt = await _promptRepository.GetPromptWithHistoryByKeyAsync(
                JdAnalysisPromptContract.UserPromptKey);

            if (systemPrompt == null || userPrompt == null)
            {
                throw new KeyNotFoundException("JD analysis prompt pair is not configured");
            }

            return new JdAnalysisPromptPairDto
            {
                SystemPrompt = MapToPromptDto(systemPrompt),
                UserPrompt = MapToPromptDto(userPrompt)
            };
        }

        public async Task<PromptVersionDto> GetPromptVersionAsync(Guid versionId)
        {
            var version = await _promptRepository.GetPromptVersionAsync(versionId);
            if (version == null)
            {
                throw new KeyNotFoundException("Prompt version not found");
            }

            return MapToVersionDto(version);
        }

        public async Task<PromptVersionDto> CreatePromptVersionAsync(Guid promptId, CreatePromptVersionDto dto, Guid adminId)
        {
            var prompt = await _promptRepository.GetPromptWithHistoryAsync(promptId);
            if (prompt == null)
            {
                throw new KeyNotFoundException("Prompt not found");
            }

            ValidateModelConfig(prompt.PromptKey, dto.ModelConfig);

            var content = dto.Content;
            if (prompt.PromptKey == JdMatchingPromptContract.PromptKey)
            {
                // Matching owns its output schema in application code. Store
                // only semantic instructions so an editor cannot silently
                // create a second, drifting provider contract.
                content = JdMatchingOutputSchema.NormalizeManagedContent(content).SemanticContent;
            }

            ValidatePlaceholders(prompt.PromptKey, content);

            if (IsManagedAnalysisPromptKey(prompt.PromptKey) && dto.MakeActive)
            {
                throw new ArgumentException("Analysis prompt versions must be activated through their system/user prompt-pair activation endpoint.");
            }

            var newVersion = new PromptVersions
            {
                Id = Guid.NewGuid(),
                PromptId = promptId,
                VersionTag = dto.VersionTag,
                Content = content,
                ModelConfig = dto.ModelConfig,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };

            var savedVersion = await _promptRepository.CreatePromptVersionAsync(newVersion, dto.MakeActive);

            return MapToVersionDto(savedVersion);
        }

        public async Task ActivatePromptVersionAsync(Guid promptId, Guid versionId, Guid adminId)
        {
            var prompt = await _promptRepository.GetPromptWithHistoryAsync(promptId);
            if (prompt == null)
            {
                throw new KeyNotFoundException("Prompt not found");
            }

            if (IsManagedAnalysisPromptKey(prompt.PromptKey))
            {
                throw new ArgumentException("Analysis prompt versions must be activated as a compatible system/user pair.");
            }

            // Validate the selected row before the repository starts its
            // deactivation transaction. This keeps a bad version from ever
            // replacing the currently active version.
            var version = await _promptRepository.GetPromptVersionAsync(versionId);
            if (version == null || version.PromptId != promptId)
            {
                throw new KeyNotFoundException("Prompt version not found or does not belong to the specified prompt.");
            }

            ValidateModelConfig(prompt.PromptKey, version.ModelConfig);
            var content = version.Content;
            if (prompt.PromptKey == JdMatchingPromptContract.PromptKey)
            {
                content = JdMatchingOutputSchema.NormalizeManagedContent(content).SemanticContent;
            }

            ValidatePlaceholders(prompt.PromptKey, content);

            await _promptRepository.ActivatePromptVersionAsync(promptId, versionId);
        }

        public async Task ActivateCvAnalysisPromptPairAsync(Guid systemVersionId, Guid userVersionId, Guid adminId)
        {
            var systemVersion = await _promptRepository.GetPromptVersionAsync(systemVersionId);
            var userVersion = await _promptRepository.GetPromptVersionAsync(userVersionId);

            if (systemVersion == null || userVersion == null)
            {
                throw new KeyNotFoundException("CV analysis prompt version not found");
            }

            if (systemVersion.Prompt?.PromptKey != CvAnalysisPromptContract.SystemPromptKey ||
                userVersion.Prompt?.PromptKey != CvAnalysisPromptContract.UserPromptKey)
            {
                throw new ArgumentException("The selected versions are not a CV analysis system/user prompt pair.");
            }

            var systemMetadata = ReadCvAnalysisMetadata(systemVersion.ModelConfig, CvAnalysisPromptContract.SystemRole);
            var userMetadata = ReadCvAnalysisMetadata(userVersion.ModelConfig, CvAnalysisPromptContract.UserRole);
            if (!string.Equals(systemMetadata.Contract, userMetadata.Contract, StringComparison.Ordinal))
            {
                throw new ArgumentException("CV analysis system and user prompts must have the same contract.");
            }

            await _promptRepository.ActivatePromptPairAsync(
                systemVersion.PromptId,
                systemVersion.Id,
                userVersion.PromptId,
                userVersion.Id);
        }

        public async Task ActivateJdAnalysisPromptPairAsync(Guid systemVersionId, Guid userVersionId, Guid adminId)
        {
            var systemVersion = await _promptRepository.GetPromptVersionAsync(systemVersionId);
            var userVersion = await _promptRepository.GetPromptVersionAsync(userVersionId);

            if (systemVersion == null || userVersion == null)
            {
                throw new KeyNotFoundException("JD analysis prompt version not found");
            }

            if (systemVersion.Prompt?.PromptKey != JdAnalysisPromptContract.SystemPromptKey ||
                userVersion.Prompt?.PromptKey != JdAnalysisPromptContract.UserPromptKey)
            {
                throw new ArgumentException("The selected versions are not a JD analysis system/user prompt pair.");
            }

            var systemMetadata = ReadJdAnalysisMetadata(systemVersion.ModelConfig, JdAnalysisPromptContract.SystemRole);
            var userMetadata = ReadJdAnalysisMetadata(userVersion.ModelConfig, JdAnalysisPromptContract.UserRole);
            if (!string.Equals(systemMetadata.Contract, userMetadata.Contract, StringComparison.Ordinal))
            {
                throw new ArgumentException("JD analysis system and user prompts must have the same contract.");
            }

            await _promptRepository.ActivatePromptPairAsync(
                systemVersion.PromptId,
                systemVersion.Id,
                userVersion.PromptId,
                userVersion.Id);
        }

        private void ValidatePlaceholders(string promptKey, string content)
        {
            var requiredPlaceholders = promptKey switch
            {
                JdAnalysisPromptContract.SystemPromptKey => Array.Empty<string>(),
                JdAnalysisPromptContract.UserPromptKey => new[] { JdAnalysisPromptContract.UserPlaceholder },
                "CV_ANALYSIS_SYSTEM" => Array.Empty<string>(),
                "CV_ANALYSIS_USER" => new[] { CvAnalysisPromptContract.UserPlaceholder },
                JdMatchingPromptContract.PromptKey => new[]
                {
                    JdMatchingPromptContract.CvPlaceholder,
                    JdMatchingPromptContract.RequirementsPlaceholder
                },
                "MOCK_INTERVIEW_START" => new[] { "[CV_TEXT]", "[JD_TEXT]" },
                "MOCK_INTERVIEW_NEXT" => new[] { "[CV_TEXT]", "[JD_TEXT]", "[INTERVIEW_CONTEXT]" },
                _ => Array.Empty<string>()
            };

            if (promptKey == JdMatchingPromptContract.PromptKey)
            {
                var invalidPlaceholders = requiredPlaceholders
                    .Where(p => JdMatchingPromptContract.FindOperationalPlaceholderIndex(content, p) < 0)
                    .ToList();
                if (invalidPlaceholders.Any())
                {
                    throw new ArgumentException(
                        $"{JdMatchingPromptContract.PromptKey} must contain exactly one operational input slot for each placeholder: {string.Join(", ", invalidPlaceholders)}");
                }

                return;
            }


            var missingPlaceholders = requiredPlaceholders.Where(p => !content.Contains(p)).ToList();

            if (missingPlaceholders.Any())
            {
                throw new ArgumentException($"Missing required placeholders in prompt content: {string.Join(", ", missingPlaceholders)}");
            }

            if (promptKey == CvAnalysisPromptContract.SystemPromptKey && content.Contains(CvAnalysisPromptContract.UserPlaceholder, StringComparison.Ordinal))
            {
                throw new ArgumentException($"{CvAnalysisPromptContract.SystemPromptKey} must not contain {CvAnalysisPromptContract.UserPlaceholder}.");
            }

            if (promptKey == CvAnalysisPromptContract.UserPromptKey &&
                CountOccurrences(content, CvAnalysisPromptContract.UserPlaceholder) != 1)
            {
                throw new ArgumentException($"{CvAnalysisPromptContract.UserPromptKey} must contain exactly one {CvAnalysisPromptContract.UserPlaceholder} placeholder.");
            }

            if (promptKey == JdAnalysisPromptContract.SystemPromptKey && content.Contains(JdAnalysisPromptContract.UserPlaceholder, StringComparison.Ordinal))
            {
                throw new ArgumentException($"{JdAnalysisPromptContract.SystemPromptKey} must not contain {JdAnalysisPromptContract.UserPlaceholder}.");
            }

            if (promptKey == JdAnalysisPromptContract.UserPromptKey &&
                CountOccurrences(content, JdAnalysisPromptContract.UserPlaceholder) != 1)
            {
                throw new ArgumentException($"{JdAnalysisPromptContract.UserPromptKey} must contain exactly one {JdAnalysisPromptContract.UserPlaceholder} placeholder.");
            }
        }

        private static int CountOccurrences(string value, string pattern)
        {
            var count = 0;
            var startIndex = 0;
            while ((startIndex = value.IndexOf(pattern, startIndex, StringComparison.Ordinal)) >= 0)
            {
                count++;
                startIndex += pattern.Length;
            }

            return count;
        }

        private static void ValidateModelConfig(string promptKey, string? modelConfig)
        {
            if (string.IsNullOrWhiteSpace(modelConfig))
            {
                if (IsManagedAnalysisPromptKey(promptKey))
                {
                    throw new ArgumentException("Analysis prompt ModelConfig is required.");
                }

                return;
            }

            try
            {
                using var document = JsonDocument.Parse(modelConfig);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException("ModelConfig must be a JSON object.");
                }

                if (CvAnalysisPromptContract.IsCvAnalysisPromptKey(promptKey))
                {
                    var expectedRole = promptKey == CvAnalysisPromptContract.SystemPromptKey
                        ? CvAnalysisPromptContract.SystemRole
                        : CvAnalysisPromptContract.UserRole;
                    ReadCvAnalysisMetadata(modelConfig, expectedRole);
                }

                if (JdAnalysisPromptContract.IsJdAnalysisPromptKey(promptKey))
                {
                    var expectedRole = promptKey == JdAnalysisPromptContract.SystemPromptKey
                        ? JdAnalysisPromptContract.SystemRole
                        : JdAnalysisPromptContract.UserRole;
                    ReadJdAnalysisMetadata(modelConfig, expectedRole);
                }
            }
            catch (JsonException)
            {
                throw new ArgumentException("ModelConfig must be a valid JSON string");
            }
        }

        private static CvAnalysisPromptMetadata ReadCvAnalysisMetadata(string? modelConfig, string expectedRole)
        {
            if (string.IsNullOrWhiteSpace(modelConfig))
            {
                throw new ArgumentException("CV analysis prompt ModelConfig is required.");
            }

            try
            {
                using var document = JsonDocument.Parse(modelConfig);
                var root = document.RootElement;
                var contract = GetRequiredString(root, "contract");
                var role = GetRequiredString(root, "role");

                if (!string.Equals(role, expectedRole, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"CV analysis prompt ModelConfig role must be '{expectedRole}'.");
                }

                return new CvAnalysisPromptMetadata(contract, role);
            }
            catch (JsonException)
            {
                throw new ArgumentException("CV analysis prompt ModelConfig must be valid JSON.");
            }
        }

        private static JdAnalysisPromptMetadata ReadJdAnalysisMetadata(string? modelConfig, string expectedRole)
        {
            if (string.IsNullOrWhiteSpace(modelConfig))
            {
                throw new ArgumentException("JD analysis prompt ModelConfig is required.");
            }

            try
            {
                using var document = JsonDocument.Parse(modelConfig);
                var root = document.RootElement;
                var contract = GetRequiredString(root, "contract");
                var role = GetRequiredString(root, "role");

                if (!string.Equals(role, expectedRole, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"JD analysis prompt ModelConfig role must be '{expectedRole}'.");
                }

                return new JdAnalysisPromptMetadata(contract, role);
            }
            catch (JsonException)
            {
                throw new ArgumentException("JD analysis prompt ModelConfig must be valid JSON.");
            }
        }

        private static bool IsManagedAnalysisPromptKey(string promptKey) =>
            CvAnalysisPromptContract.IsCvAnalysisPromptKey(promptKey) ||
            JdAnalysisPromptContract.IsJdAnalysisPromptKey(promptKey);

        private static string GetRequiredString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property) ||
                property.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(property.GetString()))
            {
                throw new ArgumentException($"CV analysis prompt ModelConfig requires a non-empty '{propertyName}' string.");
            }

            return property.GetString()!;
        }

        private sealed record CvAnalysisPromptMetadata(string Contract, string Role);
        private sealed record JdAnalysisPromptMetadata(string Contract, string Role);

        private PromptVersionDto MapToVersionDto(PromptVersions version)
        {
            return new PromptVersionDto
            {
                Id = version.Id,
                PromptId = version.PromptId,
                VersionTag = version.VersionTag,
                Content = version.Content,
                ModelConfig = version.ModelConfig,
                IsActive = version.IsActive,
                CreatedBy = version.CreatedBy,
                CreatedAt = version.CreatedAt
            };
        }

        private PromptDto MapToPromptDto(Prompts prompt)
        {
            var activeVersion = prompt.Versions.FirstOrDefault(v => v.IsActive);
            return new PromptDto
            {
                Id = prompt.Id,
                PromptKey = prompt.PromptKey,
                Description = prompt.Description,
                ActiveVersionTag = activeVersion?.VersionTag,
                CreatedAt = prompt.CreatedAt,
                UpdatedAt = prompt.UpdatedAt,
                Versions = prompt.Versions.Select(MapToVersionDto).ToList()
            };
        }
    }
}
