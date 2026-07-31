using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class PromptAdminRepository : IPromptAdminRepository
    {
        private readonly ITHunterviewContext _context;

        public PromptAdminRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Prompts> Prompts, int TotalCount)> GetPagedPromptsAsync(int page, int size)
        {
            var query = _context.Prompts
                .Include(p => p.Versions.Where(v => v.IsActive))
                .OrderBy(p => p.PromptKey)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

            return (items, totalCount);
        }

        public async Task<Prompts?> GetPromptWithHistoryAsync(Guid promptId)
        {
            return await _context.Prompts
                .Include(p => p.Versions.OrderByDescending(v => v.CreatedAt))
                .FirstOrDefaultAsync(p => p.Id == promptId);
        }

        public async Task<Prompts?> GetPromptWithHistoryByKeyAsync(string promptKey)
        {
            return await _context.Prompts
                .Include(p => p.Versions)
                .FirstOrDefaultAsync(p => p.PromptKey == promptKey);
        }

        public async Task<PromptVersions?> GetPromptVersionAsync(Guid versionId)
        {
            return await _context.PromptVersions
                .Include(pv => pv.Prompt)
                .FirstOrDefaultAsync(pv => pv.Id == versionId);
        }

        public async Task<PromptVersions> CreatePromptVersionAsync(PromptVersions newVersion, bool makeActive)
        {
            // If makeActive is true, we need to deactivate other versions in a transaction
            if (makeActive)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Deactivate all existing versions
                    await _context.PromptVersions
                        .Where(pv => pv.PromptId == newVersion.PromptId && pv.IsActive)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

                    newVersion.IsActive = true;
                    _context.PromptVersions.Add(newVersion);
                    
                    // Update Prompts UpdatedAt
                    await _context.Prompts
                        .Where(p => p.Id == newVersion.PromptId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                newVersion.IsActive = false;
                _context.PromptVersions.Add(newVersion);
                
                // Update Prompts UpdatedAt
                var prompt = await _context.Prompts.FindAsync(newVersion.PromptId);
                if (prompt != null)
                {
                    prompt.UpdatedAt = DateTime.UtcNow;
                    _context.Prompts.Update(prompt);
                }

                await _context.SaveChangesAsync();
            }

            return newVersion;
        }

        public async Task ActivatePromptVersionAsync(Guid promptId, Guid versionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Deactivate all active versions for this prompt
                await _context.PromptVersions
                    .Where(pv => pv.PromptId == promptId && pv.IsActive)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

                // Activate the specified version
                var rowsAffected = await _context.PromptVersions
                    .Where(pv => pv.Id == versionId && pv.PromptId == promptId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true));
                    
                if (rowsAffected == 0)
                {
                    throw new KeyNotFoundException("Prompt version not found or does not belong to the specified prompt.");
                }

                // Update Prompts UpdatedAt
                await _context.Prompts
                    .Where(p => p.Id == promptId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ActivatePromptPairAsync(Guid systemPromptId, Guid systemVersionId, Guid userPromptId, Guid userVersionId)
        {
            if (systemPromptId == userPromptId)
            {
                throw new ArgumentException("System and user prompts must be different.");
            }

            if (systemVersionId == userVersionId)
            {
                throw new ArgumentException("System and user prompt versions must be different.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var selectedVersions = await _context.PromptVersions
                    .AsNoTracking()
                    .Where(v => v.Id == systemVersionId || v.Id == userVersionId)
                    .Select(v => new { v.Id, v.PromptId })
                    .ToListAsync();

                var hasExpectedSystemVersion = selectedVersions.Any(v => v.Id == systemVersionId && v.PromptId == systemPromptId);
                var hasExpectedUserVersion = selectedVersions.Any(v => v.Id == userVersionId && v.PromptId == userPromptId);
                if (!hasExpectedSystemVersion || !hasExpectedUserVersion)
                {
                    throw new KeyNotFoundException("Prompt pair version not found or does not belong to the expected prompt.");
                }

                var promptIds = new[] { systemPromptId, userPromptId };
                await _context.PromptVersions
                    .Where(v => promptIds.Contains(v.PromptId) && v.IsActive)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsActive, false));

                var selectedVersionIds = new[] { systemVersionId, userVersionId };
                var rowsAffected = await _context.PromptVersions
                    .Where(v => selectedVersionIds.Contains(v.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsActive, true));

                if (rowsAffected != 2)
                {
                    throw new InvalidOperationException("Could not activate both prompt versions.");
                }

                await _context.Prompts
                    .Where(p => promptIds.Contains(p.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
