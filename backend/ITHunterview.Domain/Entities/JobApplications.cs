using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("job_applications")]
    public class JobApplications
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("candidate_id")]
        public Guid CandidateId { get; set; }

        [Column("job_id")]
        public Guid JobId { get; set; }

        [Column("cv_id")]
        public Guid? CvId { get; set; }

        [Column("cover_letter")]
        public string CoverLetter { get; set; }

        [Column("status")]
        public ApplicationStatus Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

    }
}
