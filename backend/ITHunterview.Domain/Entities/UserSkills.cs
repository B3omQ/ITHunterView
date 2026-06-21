using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("user_skills")]
    public class UserSkills
    {
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("skill_id")]
        public int SkillId { get; set; }

        [Column("proficiency_level")]
        public int? ProficiencyLevel { get; set; }

        // Navigation
        public Skills Skill { get; set; } = null!;
    }
}
