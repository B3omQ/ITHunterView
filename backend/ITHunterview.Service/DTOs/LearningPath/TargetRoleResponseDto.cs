using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class TargetRoleResponseDto
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public List<TargetRoleSkillDto> RequiredSkills { get; set; } = new List<TargetRoleSkillDto>();
    }

    public class TargetRoleSkillDto
    {
        public string SkillCode { get; set; }
        public string SkillName { get; set; }
        public int TargetLevel { get; set; }
    }
}
