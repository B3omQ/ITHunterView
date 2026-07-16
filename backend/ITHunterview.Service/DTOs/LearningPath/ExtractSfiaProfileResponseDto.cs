using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class ExtractSfiaProfileResponseDto
    {
        public string CustomRoleName { get; set; } = string.Empty;
        public string CustomRoleDescription { get; set; } = string.Empty;
        public List<ExtractedSkillProfileDto> Skills { get; set; } = new List<ExtractedSkillProfileDto>();
    }

    public class ExtractedSkillProfileDto
    {
        public string SkillCode { get; set; } = string.Empty;
        public int TargetLevel { get; set; }
        public int CurrentLevel { get; set; }
        public string Justification { get; set; } = string.Empty;
    }
}
