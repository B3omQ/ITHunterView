using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("coin_features")]
    public class CoinFeatures
    {
        [Key]
        [Column("feature_key")]
        [Required]
        [MaxLength(50)]
        public string FeatureKey { get; set; } = string.Empty;

        [Column("coin_cost")]
        public int CoinCost { get; set; }

        [Column("description")]
        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
