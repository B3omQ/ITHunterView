using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("user_activity_logs")]
    public class UserActivityLogs
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("actor_role")]
        public string ActorRole { get; set; }

        [Column("action_category")]
        public ActivityLogCategory ActionCategory { get; set; }

        [Column("actor_email")]
        public string ActorEmail { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("status")]
        public ActivityLogStatus Status { get; set; }

        [Column("ip_address")]
        public string IpAddress { get; set; }

        [Column("user_agent")]
        public string UserAgent { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
