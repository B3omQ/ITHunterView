using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("target_role_templates")]
    public class TargetRoleTemplate : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        // Relation
        public virtual ICollection<TargetRoleSkill> RequiredSkills { get; set; } = new List<TargetRoleSkill>();
    }
}
