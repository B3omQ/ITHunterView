using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Company;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICompanyUseCase
    {
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, Guid userId);
        Task<CompanyDto> VerifyCompanyAsync(Guid companyId, VerifyCompanyDto dto, Guid userId);
        Task<CompanyDto?> GetMyCompanyAsync(Guid userId);
        Task<CompanyDto> UpdateCompanyStatusAsync(Guid companyId, UpdateCompanyStatusDto dto, Guid userId);
        Task<ResponseBase<PagedResult<CompanyDto>>> GetPagedCompaniesAsync(int page, int pageSize, string? search, CompanyStatus? status);
    }
}
