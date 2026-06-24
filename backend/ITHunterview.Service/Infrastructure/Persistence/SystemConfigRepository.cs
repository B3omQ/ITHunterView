using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly ITHunterviewContext _context;

        public SystemConfigRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<SystemConfigs?> GetByKeyAsync(string key)
        {
            return await _context.SystemConfigs.FirstOrDefaultAsync(x => x.ConfigKey == key);
        }

        public async Task SaveAsync(SystemConfigs config)
        {
            var existing = await _context.SystemConfigs.FirstOrDefaultAsync(x => x.ConfigKey == config.ConfigKey);
            if (existing == null)
            {
                _context.SystemConfigs.Add(config);
            }
            else
            {
                existing.ConfigValue = config.ConfigValue;
                existing.Description = config.Description;
                existing.UpdatedBy = config.UpdatedBy;
                _context.SystemConfigs.Update(existing);
            }
            await _context.SaveChangesAsync();
        }
    }
}
