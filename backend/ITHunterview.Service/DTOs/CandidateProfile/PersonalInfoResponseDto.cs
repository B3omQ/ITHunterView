namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class PersonalInfoResponseDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? AboutMe { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? GithubUrl { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
