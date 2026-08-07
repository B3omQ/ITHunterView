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

    public async Task<(List<OptimizeSession> Items, int TotalCount)> GetHistoryByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.OptimizeSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task DeleteAsync(Guid sessionId)
    {
        var session = await _context.OptimizeSessions.FindAsync(sessionId);
        if (session != null)
        {
            _context.OptimizeSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
}
