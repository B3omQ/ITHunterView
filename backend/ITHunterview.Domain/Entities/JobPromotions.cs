using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("job_promotions")]
    public class JobPromotions
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("job_id")]
        public Guid JobId { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("status")]
        public PromotionStatus Status { get; set; }

    }
}
