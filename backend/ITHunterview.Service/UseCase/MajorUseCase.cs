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

            var major = new Majors
            {
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim().ToUpper(),
                NormalizedName = Utils.StringNormalizationHelper.NormalizeITTerm(dto.Name),
                CreatedBy = userId
            };

            await _majorRepository.AddAsync(major);

            var resultDto = new MajorDto
            {
                Id = major.Id,
                Name = major.Name,
                Code = major.Code,
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

            major.Name = dto.Name.Trim();
            major.Code = dto.Code.Trim().ToUpper();
            major.NormalizedName = Utils.StringNormalizationHelper.NormalizeITTerm(dto.Name);
            major.UpdatedBy = userId;

            await _majorRepository.UpdateAsync(major);

            var resultDto = new MajorDto
            {
                Id = major.Id,
                Name = major.Name,
                Code = major.Code,
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

            var resultDto = new MajorDto
            {
                Id = major.Id,
                Name = major.Name,
                Code = major.Code,
                CreatedBy = major.CreatedBy,
                UpdatedBy = major.UpdatedBy
            };

            return new ResponseBase<MajorDto>(resultDto, "Major restored successfully.");
        }
    }
}
