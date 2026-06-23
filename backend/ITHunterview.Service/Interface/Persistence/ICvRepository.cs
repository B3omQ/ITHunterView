using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICvRepository
    {
        Task<Cvs> CreateAsync(Cvs cv);
        Task<IEnumerable<Cvs>> GetByUserIdAsync(Guid userId);
        Task<Cvs?> GetByIdAsync(Guid id);
        Task UpdateAsync(Cvs cv);
        Task DeleteAsync(Cvs cv);
        Task<bool> HasPrimaryCvAsync(Guid userId);
        Task ResetPrimaryCvAsync(Guid userId);
    }
}
