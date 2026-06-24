using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Company;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CompanyUseCase : ICompanyUseCase
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IUserRepository _userRepository;

        public CompanyUseCase(ICompanyRepository companyRepository, IUserRepository userRepository)
        {
            _companyRepository = companyRepository;
            _userRepository = userRepository;
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, Guid userId)
        {
            var company = new Companies
            {
                Id = Guid.NewGuid(),
                Name = dto.Name ?? "",
                TaxCode = dto.TaxCode ?? "",
                HeadquartersAddress = dto.HeadquartersAddress ?? "",
                Industry = dto.Industry ?? "",
                CompanySize = dto.CompanySize ?? "",
                Description = dto.Description ?? "",
                Website = dto.Website ?? "",
                LogoUrl = dto.LogoUrl ?? "",
                VerificationDocumentUrl = "",
                Status = CompanyStatus.DRAFT,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCompany = await _companyRepository.CreateAsync(company);
            await _companyRepository.LinkCompanyToRecruiterAsync(createdCompany.Id, userId);

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
            company.TaxCode = dto.TaxCode;
            company.Name = dto.CompanyName;
            company.HeadquartersAddress = dto.HeadquartersAddress;
            company.Status = CompanyStatus.PENDING;
            company.UpdatedBy = userId;
            company.UpdatedAt = DateTime.UtcNow;

            await _companyRepository.UpdateAsync(company);

            return MapToDto(company);
        }

        public async Task<CompanyDto?> GetMyCompanyAsync(Guid userId)
        {
            var company = await _companyRepository.GetByUserIdAsync(userId);
            if (company == null) return null;

            return MapToDto(company);
        }

        public async Task<CompanyDto> UpdateCompanyStatusAsync(Guid companyId, UpdateCompanyStatusDto dto, Guid userId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                throw new KeyNotFoundException("Company not found");
            }

            company.Status = dto.Status;
            company.UpdatedBy = userId;
            company.UpdatedAt = DateTime.UtcNow;

            await _companyRepository.UpdateAsync(company);

            var resDto = MapToDto(company);
            if (company.CreatedBy.HasValue)
            {
                var user = await _userRepository.GetUserWithRoleAsync(company.CreatedBy.Value);
                if (user != null)
                {
                    resDto.CreatedByEmail = user.Email;
                    
                    string? candidateName = null;
                    if (user.CandidateProfile != null)
                    {
                        candidateName = $"{user.CandidateProfile.FirstName} {user.CandidateProfile.LastName}".Trim();
                    }

                    resDto.CreatedByName = !string.IsNullOrEmpty(user.RecruiterProfile?.FullName)
                        ? user.RecruiterProfile.FullName
                        : (!string.IsNullOrEmpty(candidateName) ? candidateName : user.Email);
                }
            }
            else
            {
                resDto.CreatedByEmail = "system@ithunterview.com";
                resDto.CreatedByName = "System";
            }
            return resDto;
        }

        public async Task<ResponseBase<PagedResult<CompanyDto>>> GetPagedCompaniesAsync(int page, int pageSize, string? search, CompanyStatus? status)
        {
            var (items, total) = await _companyRepository.GetPagedCompaniesAsync(page, pageSize, search, status);

            var companyDtos = new List<CompanyDto>();
            foreach (var company in items)
            {
                var dto = MapToDto(company);
                if (company.CreatedBy.HasValue)
                {
                    var user = await _userRepository.GetUserWithRoleAsync(company.CreatedBy.Value);
                    if (user != null)
                    {
                        dto.CreatedByEmail = user.Email;
                        
                        string? candidateName = null;
                        if (user.CandidateProfile != null)
                        {
                            candidateName = $"{user.CandidateProfile.FirstName} {user.CandidateProfile.LastName}".Trim();
                        }

                        dto.CreatedByName = !string.IsNullOrEmpty(user.RecruiterProfile?.FullName)
                            ? user.RecruiterProfile.FullName
                            : (!string.IsNullOrEmpty(candidateName) ? candidateName : user.Email);
                    }
                }
                else
                {
                    dto.CreatedByEmail = "system@ithunterview.com";
                    dto.CreatedByName = "System";
                }
                companyDtos.Add(dto);
            }

            var result = new PagedResult<CompanyDto>
            {
                Items = companyDtos,
                Total = total,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<CompanyDto>>(result);
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
