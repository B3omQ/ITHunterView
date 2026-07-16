using System;
using System.Collections.Generic;
using ITHunterview.Service.DTOs.LearningPath;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class CreateTargetRoleTemplateDto
    {
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<CreateTargetRoleSkillDto> RequiredSkills { get; set; } = new List<CreateTargetRoleSkillDto>();
    }

    public class UpdateTargetRoleTemplateDto
    {
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<CreateTargetRoleSkillDto> RequiredSkills { get; set; } = new List<CreateTargetRoleSkillDto>();
    }

    public class CreateTargetRoleSkillDto
    {
        public Guid SfiaSkillId { get; set; }
        public int TargetLevel { get; set; }
    }

    public class PagedTargetRoleResponseDto
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public List<TargetRoleResponseDto> Items { get; set; } = new List<TargetRoleResponseDto>();
    }
}
