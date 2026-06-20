using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Company;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CompanyUseCase : ICompanyUseCase
    {
        private readonly ICompanyRepository _companyRepository;

        public CompanyUseCase(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, Guid userId)
        {
            var company = new Companies
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                TaxCode = dto.TaxCode,
                HeadquartersAddress = dto.HeadquartersAddress,
                Industry = dto.Industry,
                CompanySize = dto.CompanySize,
                Description = dto.Description,
                Website = dto.Website,
                LogoUrl = dto.LogoUrl,
                Status = CompanyStatus.PENDING,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCompany = await _companyRepository.CreateAsync(company);

            return MapToDto(createdCompany);
        }

        public async Task<CompanyDto> VerifyCompanyAsync(Guid companyId, VerifyCompanyDto dto, Guid userId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                throw new KeyNotFoundException("Company not found");
            }

            company.VerificationMethod = dto.VerificationMethod;
            company.VerificationDocumentUrl = dto.VerificationDocumentUrl;
            // Status remains PENDING for manual admin verification
            company.UpdatedBy = userId;
            company.UpdatedAt = DateTime.UtcNow;

            await _companyRepository.UpdateAsync(company);

            return MapToDto(company);
        }

        private CompanyDto MapToDto(Companies company)
        {
            return new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                TaxCode = company.TaxCode,
                HeadquartersAddress = company.HeadquartersAddress,
                Industry = company.Industry,
                CompanySize = company.CompanySize,
                Description = company.Description,
                Website = company.Website,
                LogoUrl = company.LogoUrl,
                VerificationMethod = company.VerificationMethod,
                VerificationDocumentUrl = company.VerificationDocumentUrl,
                Status = company.Status,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt
            };
        }
    }
}
