using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IInterviewSessionRepository
    {
        Task<InterviewSessions?> GetByIdAsync(Guid id);
        Task<List<InterviewSessions>> GetByCandidateIdAsync(Guid candidateId);
        Task AddAsync(InterviewSessions session);
        Task UpdateAsync(InterviewSessions session);
        Task SaveChangesAsync();
        Task DeleteAsync(InterviewSessions session);
    }
}
