using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class InterviewAnswerRepository : IInterviewAnswerRepository
    {
        private readonly ITHunterviewContext _context;

        public InterviewAnswerRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<List<InterviewAnswers>> GetBySessionIdAsync(Guid sessionId)
        {
            return _context.InterviewAnswers
                .Where(a => a.SessionId == sessionId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(InterviewAnswers answer)
        {
            await _context.InterviewAnswers.AddAsync(answer);
        }

        public Task UpdateAsync(InterviewAnswers answer)
        {
            _context.InterviewAnswers.Update(answer);
            return Task.CompletedTask;
        }

        public Task<InterviewAnswers?> GetActiveTurnAsync(Guid sessionId)
        {
            return _context.InterviewAnswers
                .Where(a => a.SessionId == sessionId && a.CandidateTranscript == null)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task DeleteRangeAsync(List<InterviewAnswers> answers)
        {
            _context.InterviewAnswers.RemoveRange(answers);
            return Task.CompletedTask;
        }
    }
}
