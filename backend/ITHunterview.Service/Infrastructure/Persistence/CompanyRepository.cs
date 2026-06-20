using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly ITHunterviewContext _context;

        public CompanyRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<Companies> CreateAsync(Companies company)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            return company;
        }

        public async Task UpdateAsync(Companies company)
        {
            _context.Companies.Update(company);
            await _context.SaveChangesAsync();
        }

        public Task<Companies?> GetByIdAsync(Guid id)
        {
            return _context.Companies.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Companies?> GetByUserIdAsync(Guid userId)
        {
            var profile = await _context.RecruiterProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || profile.CompanyId == null) return null;
            return await _context.Companies.FirstOrDefaultAsync(c => c.Id == profile.CompanyId);
        }

        public async Task LinkCompanyToRecruiterAsync(Guid companyId, Guid userId)
        {
            var profile = await _context.RecruiterProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null)
            {
                profile.CompanyId = companyId;
                await _context.SaveChangesAsync();
            }
            else
            {
                profile = new RecruiterProfiles
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CompanyId = companyId
                };
                _context.RecruiterProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }
        }
    }
}
