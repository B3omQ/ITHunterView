using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Company
{
    public class CreateCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string HeadquartersAddress { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string CompanySize { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CompanyType { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;

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
