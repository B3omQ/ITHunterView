using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class CertificationUpsertRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? IssuingOrganization { get; set; }

        public DateOnly? IssueDate { get; set; }

        /// <summary>Null = No expiry.</summary>
        public DateOnly? ExpirationDate { get; set; }

        [MaxLength(500)]
        public string? CredentialUrl { get; set; }
    }
}
