using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Company;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICompanyUseCase
    {
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, Guid userId);
        Task<CompanyDto> VerifyCompanyAsync(Guid companyId, VerifyCompanyDto dto, Guid userId);
    }
}
