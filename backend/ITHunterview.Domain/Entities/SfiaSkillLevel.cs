using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("sfia_skill_levels")]
    public class SfiaSkillLevel
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("sfia_skill_id")]
        public Guid SfiaSkillId { get; set; }

        [Column("level")]
        public int Level { get; set; }

        [Column("description", TypeName = "text")]
        public string Description { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey("SfiaSkillId")]
        public virtual SfiaSkill SfiaSkill { get; set; } = null!;
    }
}
