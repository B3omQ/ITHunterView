namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class CertificationResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IssuingOrganization { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public string? CredentialUrl { get; set; }
    }
}
