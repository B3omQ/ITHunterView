using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Company
{
    public class UpdateCompanyDto
    {
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? CompanySize { get; set; }
        public string? Description { get; set; }
        public string? CompanyType { get; set; }
        public string? Industry { get; set; }

        // New fields
        public string? TradeName { get; set; }
        public List<string>? TargetCustomers { get; set; }
        public string? CompanyEmail { get; set; }
        public string? ContactPhone { get; set; }
        public List<string>? CompanyImages { get; set; }
        public string? MainField { get; set; }
        public List<string>? OperatingMarkets { get; set; }
        public string? EmployeeBenefits { get; set; }
    }
}
