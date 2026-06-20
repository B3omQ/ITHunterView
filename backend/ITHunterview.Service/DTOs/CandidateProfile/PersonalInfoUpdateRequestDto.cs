using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class PersonalInfoUpdateRequestDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Location { get; set; }

        [MaxLength(500)]
        public string? AboutMe { get; set; }

        [MaxLength(500)]
        public string? PortfolioUrl { get; set; }

        [MaxLength(500)]
        public string? LinkedInUrl { get; set; }

        [MaxLength(500)]
        public string? GithubUrl { get; set; }
    }
}
