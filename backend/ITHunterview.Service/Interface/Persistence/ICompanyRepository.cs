using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICompanyRepository
    {
        Task<Companies> CreateAsync(Companies company);
        Task UpdateAsync(Companies company);
        Task<Companies?> GetByIdAsync(Guid id);
        Task<Companies?> GetByUserIdAsync(Guid userId);
        Task LinkCompanyToRecruiterAsync(Guid companyId, Guid userId);
    }
}
