using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class LearningPathRepository : ILearningPathRepository
    {
        private readonly ITHunterviewContext _context;

        public LearningPathRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<LearningPaths> GetByIdAsync(Guid id)
        {
            return await _context.LearningPaths
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<LearningPaths>> GetByCandidateIdAsync(Guid candidateId)
        {
            return await _context.LearningPaths
                .Where(x => x.CandidateId == candidateId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<LearningPaths> AddAsync(LearningPaths entity)
        {
            _context.LearningPaths.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
