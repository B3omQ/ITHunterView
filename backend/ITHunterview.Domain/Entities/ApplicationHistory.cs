using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("application_history")]
    public class ApplicationHistory
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("application_id")]
        public Guid ApplicationId { get; set; }

        [Column("from_status")]
        public string FromStatus { get; set; }

        [Column("to_status")]
        public string ToStatus { get; set; }

        [Column("changed_by")]
        public Guid? ChangedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
