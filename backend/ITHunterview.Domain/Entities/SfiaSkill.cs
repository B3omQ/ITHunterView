using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("sfia_skills")]
    public class SfiaSkill : BaseEntity
    {
        [Required]
        [MaxLength(10)]
        [Column("skill_code")]
        public string SkillCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("skill_name")]
        public string SkillName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("subcategory")]
        public string Subcategory { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        // Relation
        public virtual ICollection<TargetRoleSkill> TargetRoleSkills { get; set; } = new List<TargetRoleSkill>();
    }
}
