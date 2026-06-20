using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class ExperienceUpsertRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        public Guid? CompanyId { get; set; }

        [MaxLength(255)]
        public string? Location { get; set; }

        public EmploymentType? EmploymentType { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public bool IsCurrent { get; set; }

        public string? Description { get; set; }
    }
}
