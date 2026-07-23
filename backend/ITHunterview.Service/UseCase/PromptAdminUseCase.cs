using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

            // Validation 1: ModelConfig must be valid JSON if provided
            if (!string.IsNullOrWhiteSpace(dto.ModelConfig))
            {
                try
                {
                    JsonDocument.Parse(dto.ModelConfig);
                }
                catch (JsonException)
                {
                    throw new ArgumentException("ModelConfig must be a valid JSON string");
                }
            }

            // Validation 2: Check required placeholders based on PromptKey
            ValidatePlaceholders(prompt.PromptKey, dto.Content);

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
            // Just call repository to activate, throwing KeyNotFound if invalid
            await _promptRepository.ActivatePromptVersionAsync(promptId, versionId);
        }

        private void ValidatePlaceholders(string promptKey, string content)
        {
            var requiredPlaceholders = promptKey switch
            {
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
        }

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
    }
}
