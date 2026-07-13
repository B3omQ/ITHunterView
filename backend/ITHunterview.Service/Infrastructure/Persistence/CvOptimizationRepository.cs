using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CvOptimizationRepository : ICvOptimizationRepository
    {
        private readonly ITHunterviewContext _context;

        public CvOptimizationRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<CvOptimizations?> GetByIdAsync(Guid id)
        {
            return await _context.CvOptimizations
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<CvOptimizations>> GetByCandidateIdAsync(Guid candidateId)
        {
            return await _context.CvOptimizations
                .Where(c => c.CandidateId == candidateId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CvOptimizations>> GetByCvIdAsync(Guid cvId)
        {
            return await _context.CvOptimizations
                .Where(c => c.CvId == cvId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(CvOptimizations entity)
        {
            await _context.CvOptimizations.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CvOptimizations entity)
        {
            _context.CvOptimizations.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(CvOptimizations entity)
        {
            _context.CvOptimizations.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
