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
            var trimmedName = dto.Name.Trim();
            var trimmedCode = dto.Code.Trim();

            var isNameDuplicate = await _majorRepository.ExistsByNameAsync(trimmedName);
            if (isNameDuplicate)
            {
                return new ResponseBase<MajorDto>("Major name already exists.");
            }

            var isCodeDuplicate = await _majorRepository.ExistsByCodeAsync(trimmedCode);
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

            if (dto.ParentId != major.ParentId)
            {
                return new ResponseBase<MajorDto>("Changing parent major after creation is not allowed.");
            }

            var trimmedName = dto.Name.Trim();
            var trimmedCode = dto.Code.Trim();

            var isNameDuplicate = await _majorRepository.ExistsByNameAsync(trimmedName, id);
            if (isNameDuplicate)
            {
                return new ResponseBase<MajorDto>("Major name already exists.");
            }

            var isCodeDuplicate = await _majorRepository.ExistsByCodeAsync(trimmedCode, id);
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

        public async Task<ResponseBase<PagedResult<MajorDto>>> GetMajorTreeAsync(int page, int pageSize, string? search)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var allMajors = await _majorRepository.GetAllActiveMajorsAsync();

            var dtoMap = allMajors.Select(m => new MajorDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                ParentId = m.ParentId,
                ParentName = m.Parent?.Name,
                CreatedBy = m.CreatedBy,
                UpdatedBy = m.UpdatedBy,
                Children = new List<MajorDto>()
            }).ToDictionary(d => d.Id);

            var allRoots = new List<MajorDto>();

            foreach (var major in allMajors)
            {
                var dto = dtoMap[major.Id];
                if (major.ParentId == null)
                {
                    allRoots.Add(dto);
                }
                else if (dtoMap.TryGetValue(major.ParentId.Value, out var parentDto))
                {
                    parentDto.Children.Add(dto);
                }
            }

            // Sort children by name alphabetically
            foreach (var dto in dtoMap.Values)
            {
                dto.Children = dto.Children.OrderBy(c => c.Name).ToList();
            }

            List<MajorDto> filteredRoots;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                var matchedMajorIds = allMajors
                    .Where(m => m.Name.ToLower().Contains(searchLower) || m.Code.ToLower().Contains(searchLower))
                    .Select(m => m.Id)
                    .ToHashSet();

                var rootIds = new HashSet<int>();
                foreach (var major in allMajors)
                {
                    if (matchedMajorIds.Contains(major.Id))
                    {
                        var current = major;
                        while (current.ParentId != null)
                        {
                            var parent = allMajors.FirstOrDefault(m => m.Id == current.ParentId.Value);
                            if (parent == null) break;
                            current = parent;
                        }
                        rootIds.Add(current.Id);
                    }
                }

                filteredRoots = allRoots.Where(r => rootIds.Contains(r.Id)).OrderBy(r => r.Name).ToList();
            }
            else
            {
                filteredRoots = allRoots.OrderBy(r => r.Name).ToList();
            }

            var total = filteredRoots.Count;
            var pagedRoots = filteredRoots.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PagedResult<MajorDto>
            {
                Items = pagedRoots,
                Total = total,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<MajorDto>>(result);
        }

        #region Helper Methods
        private int CalculateDepth(int? parentId, List<Majors> allActiveMajors)
        {
            return CalculateDepthWithVisited(parentId, allActiveMajors, new HashSet<int>());
        }

        private int CalculateDepthWithVisited(int? parentId, List<Majors> allActiveMajors, HashSet<int> visited)
        {
            if (parentId == null) return 0;
            if (visited.Contains(parentId.Value))
            {
                return MaxDepth + 1; // Return value exceeding MaxDepth to fail validation safely instead of throwing StackOverflowException
            }
            visited.Add(parentId.Value);
            var parent = allActiveMajors.FirstOrDefault(m => m.Id == parentId);
            if (parent == null) return 0;
            return CalculateDepthWithVisited(parent.ParentId, allActiveMajors, visited) + 1;
        }

        private int CalculateSubtreeHeight(int nodeId, List<Majors> allActiveMajors)
        {
            return CalculateSubtreeHeightWithVisited(nodeId, allActiveMajors, new HashSet<int>());
        }

        private int CalculateSubtreeHeightWithVisited(int nodeId, List<Majors> allActiveMajors, HashSet<int> visited)
        {
            if (visited.Contains(nodeId))
            {
                return 0; // Prevent cycle
            }
            visited.Add(nodeId);
            var children = allActiveMajors.Where(m => m.ParentId == nodeId).ToList();
            if (!children.Any()) return 0;
            int maxHeight = 0;
            foreach (var child in children)
            {
                var height = CalculateSubtreeHeightWithVisited(child.Id, allActiveMajors, new HashSet<int>(visited)) + 1;
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
            return maxHeight;
        }

        private bool IsDescendant(int parentId, int childId, List<Majors> allActiveMajors)
        {
            return IsDescendantWithVisited(parentId, childId, allActiveMajors, new HashSet<int>());
        }

        private bool IsDescendantWithVisited(int parentId, int childId, List<Majors> allActiveMajors, HashSet<int> visited)
        {
            if (parentId == childId) return true;
            if (visited.Contains(childId)) return false; // Prevent cycle
            visited.Add(childId);
            var child = allActiveMajors.FirstOrDefault(m => m.Id == childId);
            if (child == null || child.ParentId == null) return false;
            return IsDescendantWithVisited(parentId, child.ParentId.Value, allActiveMajors, visited);
        }
        #endregion
    }
}
