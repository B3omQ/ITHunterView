using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Interface.Persistence;

public interface IOptimizeSessionRepository
{
    Task<OptimizeSession> CreateAsync(OptimizeSession session);
    Task<OptimizeSession?> GetByIdAsync(Guid id);
    Task UpdateAsync(OptimizeSession session);
}
