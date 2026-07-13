using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ICvOptimizationRepository
    {
        Task<CvOptimizations?> GetByIdAsync(Guid id);
        Task<List<CvOptimizations>> GetByCandidateIdAsync(Guid candidateId);
        Task<List<CvOptimizations>> GetByCvIdAsync(Guid cvId);
        Task AddAsync(CvOptimizations entity);
        Task UpdateAsync(CvOptimizations entity);
        Task DeleteAsync(CvOptimizations entity);
    }
}
