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

            ValidatePlaceholders(prompt.PromptKey, dto.Content);

            if (CvAnalysisPromptContract.IsCvAnalysisPromptKey(prompt.PromptKey) && dto.MakeActive)
            {
                throw new ArgumentException("CV analysis prompt versions must be activated through the CV prompt-pair activation endpoint.");
            }

            var newVersion = new PromptVersions
            {
                Id = Guid.NewGuid(),
                PromptId = promptId,
                VersionTag = dto.VersionTag,
                Content = dto.Content,
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

            if (CvAnalysisPromptContract.IsCvAnalysisPromptKey(prompt.PromptKey))
            {
                throw new ArgumentException("CV analysis prompt versions must be activated as a compatible system/user pair.");
            }

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

        private void ValidatePlaceholders(string promptKey, string content)
        {
            var requiredPlaceholders = promptKey switch
            {
                "JD_ANALYSIS_V2_SYSTEM" => Array.Empty<string>(),
                "JD_ANALYSIS_V2_USER" => new[] { "[JOB_INPUT_JSON]" },
                "CV_ANALYSIS_SYSTEM" => Array.Empty<string>(),
                "CV_ANALYSIS_USER" => new[] { CvAnalysisPromptContract.UserPlaceholder },
                "JD_MATCHING_PROMPT" => new[] { "[CV_TEXT]", "[PARSED_JD_REQUIREMENTS]" },
                "MOCK_INTERVIEW_START" => new[] { "[CV_TEXT]", "[JD_TEXT]" },
                "MOCK_INTERVIEW_NEXT" => new[] { "[CV_TEXT]", "[JD_TEXT]", "[INTERVIEW_CONTEXT]" },
                _ => Array.Empty<string>()
            };


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
                if (CvAnalysisPromptContract.IsCvAnalysisPromptKey(promptKey))
                {
                    throw new ArgumentException("CV analysis prompt ModelConfig is required.");
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
