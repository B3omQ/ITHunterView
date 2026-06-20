using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Company
{
    public class VerifyCompanyDto
    {
        public CompanyVerificationMethod VerificationMethod { get; set; }
        public string VerificationDocumentUrl { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string HeadquartersAddress { get; set; } = string.Empty;
    }
}
