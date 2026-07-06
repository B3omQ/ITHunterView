using System;
using System.Collections.Generic;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Company
{
    public class CompanyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string HeadquartersAddress { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string CompanySize { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CompanyType { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public CompanyVerificationMethod VerificationMethod { get; set; }
        public string VerificationDocumentUrl { get; set; } = string.Empty;
        public CompanyStatus Status { get; set; }
        public string? PendingName { get; set; }
        public string? PendingTaxCode { get; set; }
        public string? PendingHeadquartersAddress { get; set; }
        public CompanyVerificationMethod? PendingVerificationMethod { get; set; }
        public string? PendingVerificationDocumentUrl { get; set; }
        public string? PendingCompanyType { get; set; }
        public string? RejectReason { get; set; }
        public bool HasPendingChange { get; set; }
        public string? TradeName { get; set; }
        public List<string>? TargetCustomers { get; set; }
        public string? CompanyEmail { get; set; }
        public string? ContactPhone { get; set; }
        public List<string>? CompanyImages { get; set; }
        public string? MainField { get; set; }
        public List<string>? OperatingMarkets { get; set; }
        public string? EmployeeBenefits { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByEmail { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }
}
