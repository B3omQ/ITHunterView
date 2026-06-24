using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class SocialLinksUpdateRequestDto
    {
        [MaxLength(500)]
        public string? PortfolioUrl { get; set; }

        [MaxLength(500)]
        public string? LinkedInUrl { get; set; }

        [MaxLength(500)]
        public string? GithubUrl { get; set; }
    }
}
