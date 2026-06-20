using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class EducationUpsertRequestDto
    {
        [MaxLength(100)]
        public string? Degree { get; set; }

        public int? MajorId { get; set; }

        [MaxLength(255)]
        public string? InstitutionName { get; set; }

        [Range(0.0, 10.0)]
        public decimal? Gpa { get; set; }

        [Range(0.0, 10.0)]
        public decimal? MaxGpa { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string? Description { get; set; }
    }
}
