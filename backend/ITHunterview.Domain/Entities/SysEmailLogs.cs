using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("sys_email_logs")]
    public class SysEmailLogs
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("reference_type")]
        public string ReferenceType { get; set; }

        [Column("reference_id")]
        public Guid? ReferenceId { get; set; }

        [Column("to_email")]
        public string ToEmail { get; set; }

        [Column("status")]
        public EmailLogStatus Status { get; set; }

        [Column("error_message")]
        public string ErrorMessage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
