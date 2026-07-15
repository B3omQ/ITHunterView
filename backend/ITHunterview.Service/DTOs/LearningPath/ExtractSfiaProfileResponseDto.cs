using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class ExtractSfiaProfileResponseDto
    {
        public Guid TargetRoleTemplateId { get; set; }
        public List<CandidateSfiaSkillDto> CurrentSkills { get; set; } = new List<CandidateSfiaSkillDto>();
    }
}
