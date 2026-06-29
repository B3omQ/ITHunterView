using System;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class UserSavedJobRepository : IUserSavedJobRepository
    {
        private readonly ITHunterviewContext _context;

        public UserSavedJobRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<PaginatedDataResponse<SavedJobDto>> GetSavedJobsAsync(Guid userId, int page, int pageSize)
        {
            var savedJobsQuery = from usj in _context.UserSavedJobs
                                 join job in _context.JobPostings on usj.JobId equals job.Id
                                 join company in _context.Companies on job.CompanyId equals company.Id
                                 where usj.UserId == userId
                                 select new { usj, job, company };

            var totalItems = await savedJobsQuery.CountAsync();

            var pagedResults = await savedJobsQuery
                .OrderByDescending(x => x.usj.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var savedJobs = pagedResults.Select(x => new SavedJobDto
            {
                JobId = x.job.Id,
                Title = x.job.Title,
                CompanyName = x.company.Name,
                LogoUrl = x.company.LogoUrl,
                ProvinceCode = x.job.ProvinceCode,
                DetailedLocation = x.job.DetailedLocation,
                SalaryText = $"{x.job.MinSalary} - {x.job.MaxSalary} {x.job.Currency}",
                SavedAt = x.usj.CreatedAt
            }).ToList();

            var response = new PaginatedDataResponse<SavedJobDto>
            {
                Data = savedJobs,
                Meta = new PaginationMeta
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                }
            };

            return response;
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid jobId)
        {
            return await _context.UserSavedJobs
                .AnyAsync(usj => usj.UserId == userId && usj.JobId == jobId);
        }

        public async Task AddAsync(UserSavedJobs userSavedJob)
        {
            _context.UserSavedJobs.Add(userSavedJob);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid userId, Guid jobId)
        {
            var savedJob = await _context.UserSavedJobs
                .FirstOrDefaultAsync(usj => usj.UserId == userId && usj.JobId == jobId);

            if (savedJob != null)
            {
                _context.UserSavedJobs.Remove(savedJob);
                await _context.SaveChangesAsync();
            }
        }
    }
}
