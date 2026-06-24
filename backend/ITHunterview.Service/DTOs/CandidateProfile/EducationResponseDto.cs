namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class EducationResponseDto
    {
        public Guid Id { get; set; }
        public string? Degree { get; set; }
        public int? MajorId { get; set; }
        public string? InstitutionName { get; set; }
        public decimal? Gpa { get; set; }
        public decimal? MaxGpa { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
