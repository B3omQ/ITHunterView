using System;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class MajorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public System.Collections.Generic.List<MajorDto> Children { get; set; } = new System.Collections.Generic.List<MajorDto>();
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
