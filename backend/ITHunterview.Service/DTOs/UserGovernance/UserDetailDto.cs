using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.UserGovernance
{
    public class UserDetailDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeactiveAt { get; set; }
        
        public CandidateProfileDetailDto? CandidateProfile { get; set; }
        public RecruiterProfileDetailDto? RecruiterProfile { get; set; }
    }

    public class CandidateProfileDetailDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AboutMe { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string GithubUrl { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        public string PortfolioUrl { get; set; } = string.Empty;
    }

    public class RecruiterProfileDetailDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public CompanyDetailDto? Company { get; set; }
    }

    public class CompanyDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string HeadquartersAddress { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }
}
