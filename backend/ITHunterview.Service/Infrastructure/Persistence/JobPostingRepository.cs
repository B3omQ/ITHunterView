using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class JobPostingRepository : IJobPostingRepository
    {
        private readonly ITHunterviewContext _context;

        public JobPostingRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<JobPostings?> GetByIdAsync(Guid id)
        {
            return await _context.JobPostings
                .Include(j => j.JobSkills)
                    .ThenInclude(js => js.Skill)
                .FirstOrDefaultAsync(j => j.Id == id && j.DeletedAt == null);
        }

        public async Task<(List<JobPostings> Items, int TotalCount)> GetPagedAsync(
            string? search, 
            JobStatus? status, 
            int page, 
            int pageSize)
        {
            var query = _context.JobPostings.Where(j => j.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(j => j.Title.ToLower().Contains(lowerSearch) || j.JobCode.ToLower().Contains(lowerSearch));
            }

            if (status.HasValue)
            {
                query = query.Where(j => j.Status == status.Value);
            }

            // Order by CreatedAt descending to show newest first
            query = query.OrderByDescending(j => j.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(JobPostings job)
        {
            _context.JobPostings.Add(job);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(JobPostings job)
        {
            _context.JobPostings.Update(job);
            await _context.SaveChangesAsync();
        }

        public async Task SyncJobSkillsAsync(Guid jobId, List<JobSkillRequirements> newSkills)
        {
            var existingSkills = await _context.JobSkillRequirements.Where(js => js.JobId == jobId).ToListAsync();
            _context.JobSkillRequirements.RemoveRange(existingSkills);
            _context.JobSkillRequirements.AddRange(newSkills);
            await _context.SaveChangesAsync();
        }

        public async Task<Guid?> GetRecruiterCompanyIdAsync(Guid recruiterId)
        {
            var profile = await _context.RecruiterProfiles.FirstOrDefaultAsync(rp => rp.UserId == recruiterId);
            return profile?.CompanyId;
        }
    }
}
