using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ITHunterviewContext _context;

        public RoleRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<Roles?> GetByNameAsync(string roleName)
            => _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);

        public Task<Roles?> GetByIdAsync(int id)
            => _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
    }
}
