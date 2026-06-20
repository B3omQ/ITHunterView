namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class ExperienceResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public Guid? CompanyId { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Description { get; set; }
    }
}
