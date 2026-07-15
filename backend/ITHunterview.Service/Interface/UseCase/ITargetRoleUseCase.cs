using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.DTOs.MasterData;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ITargetRoleUseCase
    {
        Task<PagedTargetRoleResponseDto> GetPagedRolesAsync(int page, int pageSize, string? search);
        Task<ResponseBase<TargetRoleResponseDto>> CreateRoleAsync(CreateTargetRoleTemplateDto dto);
        Task<ResponseBase<TargetRoleResponseDto>> UpdateRoleAsync(Guid id, UpdateTargetRoleTemplateDto dto);
        Task<ResponseBase<bool>> DeleteRoleAsync(Guid id);
        
        // Also a helper to get all SfiaSkills so the admin can pick them in the dropdown
        Task<List<SfiaSkillDto>> GetAllSfiaSkillsAsync();
    }

    public class SfiaSkillDto
    {
        public Guid Id { get; set; }
        public string SkillCode { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
