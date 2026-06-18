using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("candidate_profiles")]
    public class CandidateProfiles
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("phone")]
        public string Phone { get; set; }

        [Column("location")]
        public string Location { get; set; }

        [Column("about_me")]
        public string AboutMe { get; set; }

        [Column("avatar_url")]
        public string AvatarUrl { get; set; }

        [Column("github_url")]
        public string GithubUrl { get; set; }

        [Column("linkedIn_url")]
        public string LinkedinUrl { get; set; }

        [Column("portfolio_url")]
        public string PortfolioUrl { get; set; }

        [Column("is_visible_to_recruiters")]
        public bool IsVisibleToRecruiters { get; set; }

    }
}
