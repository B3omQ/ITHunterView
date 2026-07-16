using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("learning_paths")]
    public class LearningPaths
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("candidate_id")]
        public Guid CandidateId { get; set; }

        [Column("session_id")]
        public Guid? SessionId { get; set; }

        [Column("path_data")]
        public string PathData { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("title")]
        [StringLength(255)]
        public string Title { get; set; } = "Learning Path";

        [Column("status")]
        [StringLength(50)]
        public string Status { get; set; } = "Not Started";
    }
}
