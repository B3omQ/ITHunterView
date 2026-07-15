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
    }

    public class UpdateSfiaSkillDto
    {
        public string SkillCode { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SfiaSkillResponseDto
    {
        public Guid Id { get; set; }
        public string SkillCode { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class PagedSfiaSkillResponseDto
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public List<SfiaSkillResponseDto> Items { get; set; } = new List<SfiaSkillResponseDto>();
    }
}
