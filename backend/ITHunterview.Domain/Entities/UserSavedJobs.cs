using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("user_saved_jobs")]
    public class UserSavedJobs
    {
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("job_id")]
        public Guid JobId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
