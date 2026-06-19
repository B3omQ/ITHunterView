using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("job_skill_requirements")]
    public class JobSkillRequirements
    {
        [Column("job_id")]
        public Guid JobId { get; set; }

        [Column("skill_id")]
        public int SkillId { get; set; }

        [Column("is_mandatory")]
        public bool IsMandatory { get; set; }

        // Navigation properties
        public Skills Skill { get; set; }
        public JobPostings JobPosting { get; set; }
    }
}
