using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ISkillUseCase
    {
        Task<ResponseBase<PagedResult<SkillDto>>> GetPagedSkillsAsync(int page, int pageSize, string? search, int? categoryId, SkillStatus? status);
        Task<ResponseBase<List<SkillCategoryDto>>> GetAllCategoriesAsync();
        Task<ResponseBase<SkillDto>> CreateSkillAsync(CreateSkillDto dto, Guid userId);
        Task<ResponseBase<SkillDto>> UpdateSkillAsync(int id, UpdateSkillDto dto, Guid userId);
        Task<ResponseBase<SkillDto>> UpdateSkillStatusAsync(int id, UpdateSkillStatusDto dto, Guid userId);
        Task<ResponseBase> DeleteSkillAsync(int id);
    }
}
