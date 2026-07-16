using System;
using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class SfiaSkillLevelDto
    {
        public Guid Id { get; set; }
        public int Level { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class CreateSfiaSkillLevelDto
    {
        [Required]
        [Range(1, 7)]
        public int Level { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
