using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("candidate_educations")]
    public class CandidateEducations
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("degree")]
        public string Degree { get; set; }

        [Column("major_id")]
        public int? MajorId { get; set; }

        [Column("institution_name")]
        public string InstitutionName { get; set; }

        [Column("gpa")]
        public decimal? Gpa { get; set; }

        [Column("max_gpa")]
        public decimal? MaxGpa { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

    }
}
