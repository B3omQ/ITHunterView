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
        public string? ProvinceCode { get; set; }
        public string? DetailedLocation { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? CompanyType { get; set; }
    }
}
