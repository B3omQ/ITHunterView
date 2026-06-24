namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class ProfileSummaryResponseDto
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Location { get; set; }
        public bool IsVisibleToRecruiters { get; set; }
        public DateTime? LastSavedAt { get; set; }
    }
}
