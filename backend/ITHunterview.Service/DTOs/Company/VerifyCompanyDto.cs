using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Company
{
    public class VerifyCompanyDto
    {
        public CompanyVerificationMethod VerificationMethod { get; set; }
        public string VerificationDocumentUrl { get; set; } = string.Empty;
    }
}
