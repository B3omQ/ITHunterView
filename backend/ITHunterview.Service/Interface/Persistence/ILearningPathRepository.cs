using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface ILearningPathRepository
    {
        Task<LearningPaths> GetByIdAsync(Guid id);
        Task<List<LearningPaths>> GetByCandidateIdAsync(Guid candidateId);
        Task<LearningPaths> AddAsync(LearningPaths entity);
    }
}
