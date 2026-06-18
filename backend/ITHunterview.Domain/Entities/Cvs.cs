using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("cvs")]
    public class Cvs
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("file_url")]
        public string FileUrl { get; set; }

        [Column("file_name")]
        public string FileName { get; set; }

        [Column("file_size")]
        public int? FileSize { get; set; }

        [Column("file_type")]
        public string FileType { get; set; }

        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        [Column("parsed_data")]
        public string ParsedData { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

    }
}
