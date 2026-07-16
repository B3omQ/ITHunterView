using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence;

public class OptimizeSessionRepository : IOptimizeSessionRepository
{
    private readonly ITHunterviewContext _context;

    public OptimizeSessionRepository(ITHunterviewContext context)
    {
        _context = context;
    }

    public async Task<OptimizeSession> CreateAsync(OptimizeSession session)
    {
        await _context.OptimizeSessions.AddAsync(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<OptimizeSession?> GetByIdAsync(Guid id)
    {
        return await _context.OptimizeSessions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateAsync(OptimizeSession session)
    {
        _context.OptimizeSessions.Update(session);
        await _context.SaveChangesAsync();
    }
}
