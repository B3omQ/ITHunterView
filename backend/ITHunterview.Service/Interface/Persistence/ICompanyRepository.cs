using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICompanyRepository
    {
        Task<Companies> CreateAsync(Companies company);
        Task UpdateAsync(Companies company);
        Task<Companies?> GetByIdAsync(Guid id);
        Task<Companies?> GetByUserIdAsync(Guid userId);
        Task LinkCompanyToRecruiterAsync(Guid companyId, Guid userId);
        Task<(List<Companies> items, int total)> GetPagedCompaniesAsync(int page, int pageSize, string? search, CompanyStatus? status);
    }
}
