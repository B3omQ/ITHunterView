using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class InterviewSessionRepository : IInterviewSessionRepository
    {
        private readonly ITHunterviewContext _context;

        public InterviewSessionRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<InterviewSessions?> GetByIdAsync(Guid id)
        {
            return _context.InterviewSessions.FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task<List<InterviewSessions>> GetByCandidateIdAsync(Guid candidateId)
        {
            return _context.InterviewSessions
                .Where(s => s.CandidateId == candidateId)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();
        }

        public async Task AddAsync(InterviewSessions session)
        {
            await _context.InterviewSessions.AddAsync(session);
        }

        public Task UpdateAsync(InterviewSessions session)
        {
            _context.InterviewSessions.Update(session);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task DeleteAsync(InterviewSessions session)
        {
            _context.InterviewSessions.Remove(session);
            return Task.CompletedTask;
        }
    }
}
