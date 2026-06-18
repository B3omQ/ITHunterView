using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("job_reviews")]
    public class JobReviews
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("job_id")]
        public Guid JobId { get; set; }

        [Column("admin_id")]
        public Guid? AdminId { get; set; }

        [Column("status")]
        public ReviewStatus Status { get; set; }

        [Column("reject_reason")]
        public string RejectReason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
