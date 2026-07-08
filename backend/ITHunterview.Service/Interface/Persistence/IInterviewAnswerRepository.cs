using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IInterviewAnswerRepository
    {
        Task<List<InterviewAnswers>> GetBySessionIdAsync(Guid sessionId);
        Task AddAsync(InterviewAnswers answer);
        Task UpdateAsync(InterviewAnswers answer);
        Task<InterviewAnswers?> GetActiveTurnAsync(Guid sessionId);
        Task SaveChangesAsync();
        Task DeleteRangeAsync(List<InterviewAnswers> answers);
    }
}
