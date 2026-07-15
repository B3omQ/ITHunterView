using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class CreateSfiaSkillDto
    {
        public string SkillCode { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AvailableLevels { get; set; } = string.Empty;
        public List<CreateSfiaSkillLevelDto> Levels { get; set; } = new();
    }

    public class UpdateSfiaSkillDto
    {
        public string SkillCode { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AvailableLevels { get; set; } = string.Empty;
        public List<CreateSfiaSkillLevelDto> Levels { get; set; } = new();
    }

    public class SfiaSkillResponseDto
    {
        public Guid Id { get; set; }
        public string SkillCode { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AvailableLevels { get; set; } = string.Empty;
        public List<SfiaSkillLevelDto> Levels { get; set; } = new();
    }
}
