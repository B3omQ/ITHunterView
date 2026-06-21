using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class SkillDto
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public SkillStatus Status { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
