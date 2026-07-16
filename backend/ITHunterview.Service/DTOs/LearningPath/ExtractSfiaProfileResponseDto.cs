using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class ExtractSfiaProfileResponseDto
    {
        public Guid? TargetRoleTemplateId { get; set; }
        public List<CandidateSfiaSkillDto> CurrentSkills { get; set; } = new List<CandidateSfiaSkillDto>();
        public AiGeneratedRoleDto? NewRole { get; set; }
    }

    public class AiGeneratedRoleDto
    {
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<AiGeneratedRoleSkillDto> RequiredSkills { get; set; } = new List<AiGeneratedRoleSkillDto>();
    }

    public class AiGeneratedRoleSkillDto
    {
        public string SkillCode { get; set; } = string.Empty;
        public int TargetLevel { get; set; }
    }
}
