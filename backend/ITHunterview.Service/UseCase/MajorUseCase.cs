using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class MajorUseCase : IMajorUseCase
    {
        private readonly IMajorRepository _majorRepository;
        private const int MaxDepth = 3;

        public MajorUseCase(IMajorRepository majorRepository)
        {
            _majorRepository = majorRepository;
        }

        public async Task<ResponseBase<PagedResult<MajorDto>>> GetPagedMajorsAsync(int page, int pageSize, string? search)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var (items, total) = await _majorRepository.GetPagedMajorsAsync(page, pageSize, search);

            var dtos = items.Select(m => new MajorDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                ParentId = m.ParentId,
                ParentName = m.Parent?.Name,
                CreatedBy = m.CreatedBy,
                UpdatedBy = m.UpdatedBy
            }).ToList();

            var result = new PagedResult<MajorDto>
            {
                Items = dtos,
                Total = total,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<MajorDto>>(result);
        }

        public async Task<ResponseBase<MajorDto>> CreateMajorAsync(CreateMajorDto dto, Guid userId)
        {
            var isNameDuplicate = await _majorRepository.ExistsByNameAsync(dto.Name);
            if (isNameDuplicate)
            {
                return new ResponseBase<MajorDto>("Major name already exists.");
            }

            var isCodeDuplicate = await _majorRepository.ExistsByCodeAsync(dto.Code);
            if (isCodeDuplicate)
            {
                return new ResponseBase<MajorDto>("Major code already exists.");
            }

            if (dto.ParentId.HasValue)
            {
                var parent = await _majorRepository.GetByIdAsync(dto.ParentId.Value);
                if (parent == null)
                {
                    return new ResponseBase<MajorDto>("Parent major does not exist.");
                }

                var allActiveMajors = await _majorRepository.GetAllActiveMajorsAsync();
                var depth = CalculateDepth(dto.ParentId.Value, allActiveMajors) + 1;
                if (depth > MaxDepth)
                {
                    return new ResponseBase<MajorDto>($"Exceeds maximum hierarchy depth of {MaxDepth}.");
                }
            }

            var major = new Majors
            {
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim().ToUpper(),
                ParentId = dto.ParentId,
                NormalizedName = Utils.StringNormalizationHelper.NormalizeITTerm(dto.Name),
                CreatedBy = userId
            };

            await _majorRepository.AddAsync(major);

            // Fetch created major with parent details to populate DTO
            var createdMajor = await _majorRepository.GetByIdAsync(major.Id);
            string? parentName = null;
            if (createdMajor?.ParentId.HasValue == true)
            {
                var parentEntity = await _majorRepository.GetByIdAsync(createdMajor.ParentId.Value);
                parentName = parentEntity?.Name;
            }

            var resultDto = new MajorDto
            {
                Id = major.Id,
                Name = major.Name,
                Code = major.Code,
                ParentId = major.ParentId,
                ParentName = parentName,
                CreatedBy = major.CreatedBy
            };

            return new ResponseBase<MajorDto>(resultDto, "Major created successfully.");
        }

        public async Task<ResponseBase<MajorDto>> UpdateMajorAsync(int id, UpdateMajorDto dto, Guid userId)
        {
            var major = await _majorRepository.GetByIdAsync(id);
            if (major == null)
            {
                return new ResponseBase<MajorDto>("Major does not exist.");
            }

            var isNameDuplicate = await _majorRepository.ExistsByNameAsync(dto.Name, id);
            if (isNameDuplicate)
            {
                return new ResponseBase<MajorDto>("Major name already exists.");
            }

            var isCodeDuplicate = await _majorRepository.ExistsByCodeAsync(dto.Code, id);
            if (isCodeDuplicate)
            {
                return new ResponseBase<MajorDto>("Major code already exists.");
            }

            if (dto.ParentId.HasValue)
            {
                if (dto.ParentId.Value == id)
                {
                    return new ResponseBase<MajorDto>("A major cannot be its own parent.");
                }

                var parent = await _majorRepository.GetByIdAsync(dto.ParentId.Value);
                if (parent == null)
                {
                    return new ResponseBase<MajorDto>("Parent major does not exist.");
                }

                var allActiveMajors = await _majorRepository.GetAllActiveMajorsAsync();

                // Check circular reference
                if (IsDescendant(id, dto.ParentId.Value, allActiveMajors))
                {
                    return new ResponseBase<MajorDto>("Circular reference detected. Cannot assign a descendant as a parent.");
                }

                // Check depth limit
                var depth = CalculateDepth(dto.ParentId.Value, allActiveMajors) + 1 + CalculateSubtreeHeight(id, allActiveMajors);
                if (depth > MaxDepth)
                {
                    return new ResponseBase<MajorDto>($"Exceeds maximum hierarchy depth of {MaxDepth}.");
                }
            }

            major.Name = dto.Name.Trim();
            major.Code = dto.Code.Trim().ToUpper();
            major.ParentId = dto.ParentId;
            major.NormalizedName = Utils.StringNormalizationHelper.NormalizeITTerm(dto.Name);
            major.UpdatedBy = userId;

            await _majorRepository.UpdateAsync(major);

            string? parentName = null;
            if (major.ParentId.HasValue)
            {
                var parentEntity = await _majorRepository.GetByIdAsync(major.ParentId.Value);
                parentName = parentEntity?.Name;
            }

            var resultDto = new MajorDto
            {
                Id = major.Id,
                Name = major.Name,
                Code = major.Code,
                ParentId = major.ParentId,
                ParentName = parentName,
                CreatedBy = major.CreatedBy,
                UpdatedBy = major.UpdatedBy
            };

            return new ResponseBase<MajorDto>(resultDto, "Major updated successfully.");
        }

        public async Task<ResponseBase> DeleteMajorAsync(int id, Guid userId)
        {
            var major = await _majorRepository.GetByIdAsync(id);
            if (major == null)
            {
                return ResponseBase.Fail("Major does not exist.");
            }

            var hasChildren = await _majorRepository.HasChildrenAsync(id);
            if (hasChildren)
            {
                return ResponseBase.Fail("Cannot delete this major because it contains sub-majors (children).");
            }

            var isInUse = await _majorRepository.IsMajorInUseAsync(id);
            if (isInUse)
            {
                return ResponseBase.Fail("Cannot delete this major because there are candidates enrolled in it.");
            }

            await _majorRepository.DeleteAsync(major, userId);
            return ResponseBase.Ok("Major deleted successfully.");
        }

        public async Task<ResponseBase<MajorDto>> RestoreMajorAsync(int id, Guid userId)
        {
            var major = await _majorRepository.GetDeletedByIdAsync(id);
            if (major == null)
            {
                return new ResponseBase<MajorDto>("The deleted major does not exist or has not been deleted.");
            }

            // Check if name/code restored will conflict with active records
            var isNameDuplicate = await _majorRepository.ExistsByNameAsync(major.Name);
            if (isNameDuplicate)
            {
                return new ResponseBase<MajorDto>("Cannot restore because this major name already exists in another active record.");
            }

            var isCodeDuplicate = await _majorRepository.ExistsByCodeAsync(major.Code);
            if (isCodeDuplicate)
            {
                return new ResponseBase<MajorDto>("Cannot restore because this major code already exists in another active record.");
            }

            major.DeletedAt = null;
            major.UpdatedBy = userId;
            await _majorRepository.UpdateAsync(major);

            string? parentName = null;
            if (major.ParentId.HasValue)
            {
                var parentEntity = await _majorRepository.GetByIdAsync(major.ParentId.Value);
                parentName = parentEntity?.Name;
            }

            var resultDto = new MajorDto
            {
                Id = major.Id,
                Name = major.Name,
                Code = major.Code,
                ParentId = major.ParentId,
                ParentName = parentName,
                CreatedBy = major.CreatedBy,
                UpdatedBy = major.UpdatedBy
            };

            return new ResponseBase<MajorDto>(resultDto, "Major restored successfully.");
        }

        public async Task<ResponseBase<List<MajorDto>>> GetMajorTreeAsync()
        {
            var allMajors = await _majorRepository.GetAllActiveMajorsAsync();

            var dtoMap = allMajors.Select(m => new MajorDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                ParentId = m.ParentId,
                ParentName = m.Parent?.Name,
                CreatedBy = m.CreatedBy,
                UpdatedBy = m.UpdatedBy
            }).ToDictionary(d => d.Id);

            var roots = new List<MajorDto>();

            foreach (var major in allMajors)
            {
                var dto = dtoMap[major.Id];
                if (major.ParentId == null)
                {
                    roots.Add(dto);
                }
                else if (dtoMap.TryGetValue(major.ParentId.Value, out var parentDto))
                {
                    parentDto.Children.Add(dto);
                }
            }

            // Sort roots and children by name alphabetically
            roots = roots.OrderBy(r => r.Name).ToList();
            foreach (var dto in dtoMap.Values)
            {
                dto.Children = dto.Children.OrderBy(c => c.Name).ToList();
            }

            return new ResponseBase<List<MajorDto>>(roots);
        }

        #region Helper Methods
        private int CalculateDepth(int? parentId, List<Majors> allActiveMajors)
        {
            if (parentId == null) return 0;
            var parent = allActiveMajors.FirstOrDefault(m => m.Id == parentId);
            if (parent == null) return 0;
            return CalculateDepth(parent.ParentId, allActiveMajors) + 1;
        }

        private int CalculateSubtreeHeight(int nodeId, List<Majors> allActiveMajors)
        {
            var children = allActiveMajors.Where(m => m.ParentId == nodeId).ToList();
            if (!children.Any()) return 0;
            int maxHeight = 0;
            foreach (var child in children)
            {
                var height = CalculateSubtreeHeight(child.Id, allActiveMajors) + 1;
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
            return maxHeight;
        }

        private bool IsDescendant(int parentId, int childId, List<Majors> allActiveMajors)
        {
            if (parentId == childId) return true;
            var child = allActiveMajors.FirstOrDefault(m => m.Id == childId);
            if (child == null || child.ParentId == null) return false;
            return IsDescendant(parentId, child.ParentId.Value, allActiveMajors);
        }
        #endregion
    }
}
