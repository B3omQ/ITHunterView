using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class SkillUseCase : ISkillUseCase
    {
        private readonly ISkillRepository _skillRepository;
        private readonly ISkillCategoryRepository _skillCategoryRepository;

        public SkillUseCase(ISkillRepository skillRepository, ISkillCategoryRepository skillCategoryRepository)
        {
            _skillRepository = skillRepository;
            _skillCategoryRepository = skillCategoryRepository;
        }

        public async Task<ResponseBase<PagedResult<SkillDto>>> GetPagedSkillsAsync(
            int page, int pageSize, string? search, int? categoryId, SkillStatus? status)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var (items, total) = await _skillRepository.GetPagedSkillsAsync(page, pageSize, search, categoryId, status);
            
            var categories = await _skillCategoryRepository.GetAllCategoriesAsync();
            var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

            var skillDtos = items.Select(s => new SkillDto
            {
                Id = s.Id,
                CategoryId = s.CategoryId,
                CategoryName = s.CategoryId.HasValue && categoryDict.ContainsKey(s.CategoryId.Value) 
                    ? categoryDict[s.CategoryId.Value] 
                    : string.Empty,
                Name = s.Name,
                Status = s.Status,
                CreatedBy = s.CreatedBy,
                UpdatedBy = s.UpdatedBy
            }).ToList();

            var result = new PagedResult<SkillDto>
            {
                Items = skillDtos,
                Total = total,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<SkillDto>>(result);
        }

        public async Task<ResponseBase<List<SkillCategoryDto>>> GetAllCategoriesAsync()
        {
            var categories = await _skillCategoryRepository.GetAllCategoriesAsync();
            var dtos = categories.Select(c => new SkillCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            return new ResponseBase<List<SkillCategoryDto>>(dtos);
        }

        public async Task<ResponseBase<SkillDto>> CreateSkillAsync(CreateSkillDto dto, Guid userId)
        {
            var isCategoryValid = await _skillCategoryRepository.CategoryExistsAsync(dto.CategoryId);
            if (!isCategoryValid)
            {
                return new ResponseBase<SkillDto>("Skill category does not exist.");
            }

            var isDuplicate = await _skillRepository.ExistsByNameAsync(dto.Name);
            if (isDuplicate)
            {
                return new ResponseBase<SkillDto>("Skill name already exists in the system.");
            }

            var skill = new Skills
            {
                Name = dto.Name.Trim(),
                CategoryId = dto.CategoryId,
                Status = dto.Status,
                NormalizedName = Utils.StringNormalizationHelper.NormalizeITTerm(dto.Name),
                CreatedBy = userId
            };

            await _skillRepository.AddAsync(skill);

            var categories = await _skillCategoryRepository.GetAllCategoriesAsync();
            var categoryName = categories.FirstOrDefault(c => c.Id == skill.CategoryId)?.Name ?? string.Empty;

            var resultDto = new SkillDto
            {
                Id = skill.Id,
                CategoryId = skill.CategoryId,
                CategoryName = categoryName,
                Name = skill.Name,
                Status = skill.Status,
                CreatedBy = skill.CreatedBy
            };

            return new ResponseBase<SkillDto>(resultDto, "Skill created successfully.");
        }

        public async Task<ResponseBase<SkillDto>> UpdateSkillAsync(int id, UpdateSkillDto dto, Guid userId)
        {
            var skill = await _skillRepository.GetByIdAsync(id);
            if (skill == null)
            {
                return new ResponseBase<SkillDto>("Skill does not exist.");
            }

            var isCategoryValid = await _skillCategoryRepository.CategoryExistsAsync(dto.CategoryId);
            if (!isCategoryValid)
            {
                return new ResponseBase<SkillDto>("Skill category does not exist.");
            }

            var isDuplicate = await _skillRepository.ExistsByNameAsync(dto.Name, id);
            if (isDuplicate)
            {
                return new ResponseBase<SkillDto>("Skill name already exists in the system.");
            }

            skill.Name = dto.Name.Trim();
            skill.CategoryId = dto.CategoryId;
            skill.Status = dto.Status;
            skill.NormalizedName = Utils.StringNormalizationHelper.NormalizeITTerm(dto.Name);
            skill.UpdatedBy = userId;

            await _skillRepository.UpdateAsync(skill);

            var categories = await _skillCategoryRepository.GetAllCategoriesAsync();
            var categoryName = categories.FirstOrDefault(c => c.Id == skill.CategoryId)?.Name ?? string.Empty;

            var resultDto = new SkillDto
            {
                Id = skill.Id,
                CategoryId = skill.CategoryId,
                CategoryName = categoryName,
                Name = skill.Name,
                Status = skill.Status,
                CreatedBy = skill.CreatedBy,
                UpdatedBy = skill.UpdatedBy
            };

            return new ResponseBase<SkillDto>(resultDto, "Skill updated successfully.");
        }

        public async Task<ResponseBase<SkillDto>> UpdateSkillStatusAsync(int id, UpdateSkillStatusDto dto, Guid userId)
        {
            var skill = await _skillRepository.GetByIdAsync(id);
            if (skill == null)
            {
                return new ResponseBase<SkillDto>("Skill does not exist.");
            }

            if (dto.Status == SkillStatus.DEACTIVE && !dto.Force)
            {
                var userCount = await _skillRepository.CountUserSkillsAsync(id);
                var jobCount = await _skillRepository.CountJobRequirementsAsync(id);
                if (userCount > 0 || jobCount > 0)
                {
                    return new ResponseBase<SkillDto>($"Skill is currently used by {userCount} candidates and {jobCount} jobs. Are you sure you want to deactivate it?");
                }
            }

            skill.Status = dto.Status;
            skill.UpdatedBy = userId;

            await _skillRepository.UpdateAsync(skill);

            var categories = await _skillCategoryRepository.GetAllCategoriesAsync();
            var categoryName = categories.FirstOrDefault(c => c.Id == skill.CategoryId)?.Name ?? string.Empty;

            var resultDto = new SkillDto
            {
                Id = skill.Id,
                CategoryId = skill.CategoryId,
                CategoryName = categoryName,
                Name = skill.Name,
                Status = skill.Status,
                CreatedBy = skill.CreatedBy,
                UpdatedBy = skill.UpdatedBy
            };

            string message = "Skill status updated successfully.";
            return new ResponseBase<SkillDto>(resultDto, message);
        }

        public async Task<ResponseBase> DeleteSkillAsync(int id)
        {
            var skill = await _skillRepository.GetByIdAsync(id);
            if (skill == null)
            {
                return ResponseBase.Fail("Skill does not exist.");
            }

            var isInUse = await _skillRepository.IsSkillInUseAsync(id);
            if (isInUse)
            {
                return ResponseBase.Fail("Cannot delete this skill because there are candidates or jobs using it. Please change its status to DEACTIVE instead.");
            }

            await _skillRepository.DeleteAsync(skill);
            return ResponseBase.Ok("Skill deleted successfully.");
        }
    }
}
