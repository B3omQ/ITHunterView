using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("skill_aliases")]
    public class SkillAliases
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("skill_id")]
        public int SkillId { get; set; }

        [Column("alias_name")]
        [MaxLength(255)]
        public string AliasName { get; set; } = string.Empty;

        [Column("normalized_alias_name")]
        [MaxLength(255)]
        public string NormalizedAliasName { get; set; } = string.Empty;

        [ForeignKey("SkillId")]
        public virtual Skills Skill { get; set; } = null!;
    }
}
