using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("target_role_skills")]
    public class TargetRoleSkill : BaseEntity
    {
        [Required]
        [Column("role_template_id")]
        public Guid RoleTemplateId { get; set; }

        [Required]
        [Column("sfia_skill_id")]
        public Guid SfiaSkillId { get; set; }

        [Required]
        [Column("target_level")]
        public int TargetLevel { get; set; }

        // Navigation properties
        [ForeignKey("RoleTemplateId")]
        public virtual TargetRoleTemplate RoleTemplate { get; set; }

        [ForeignKey("SfiaSkillId")]
        public virtual SfiaSkill SfiaSkill { get; set; }
    }
}
