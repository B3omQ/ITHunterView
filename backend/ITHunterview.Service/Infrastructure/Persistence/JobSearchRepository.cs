using System;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobSearch;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class JobSearchRepository : IJobSearchRepository
    {
        private readonly ITHunterviewContext _context;

        public JobSearchRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<PaginatedDataResponse<JobCardDto>> SearchJobsAsync(JobSearchQueryDto query, Guid? userId = null)
        {
            var jobsQuery = _context.JobPostings
                .Where(j => j.Status == JobStatus.PUBLISHED);

            if (!string.IsNullOrEmpty(query.Location))
            {
                var lowerLocation = query.Location.ToLower();
                jobsQuery = jobsQuery.Where(j => j.Location.ToLower().Contains(lowerLocation));
            }

            if (query.JobType.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.JobType == query.JobType.Value);
            }

            if (query.CategoryId.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.CategoryId == query.CategoryId.Value);
            }

            var queryable = from job in jobsQuery
                            join company in _context.Companies on job.CompanyId equals company.Id
                            select new { job, company };

            if (!string.IsNullOrEmpty(query.Keyword))
            {
                var lowerKeyword = query.Keyword.ToLower();
                queryable = queryable.Where(x => 
                    (x.job.Title != null && x.job.Title.ToLower().Contains(lowerKeyword)) || 
                    (x.company.Name != null && x.company.Name.ToLower().Contains(lowerKeyword)));
            }

            var totalItems = await queryable.CountAsync();

            var pagedResults = await queryable
                .OrderByDescending(x => x.job.PublishedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var jobCards = pagedResults.Select(x => new JobCardDto
            {
                Id = x.job.Id,
                Title = x.job.Title,
                CompanyName = x.company.Name,
                LogoUrl = x.company.LogoUrl,
                MinSalary = x.job.MinSalary,
                MaxSalary = x.job.MaxSalary,
                Currency = x.job.Currency,
                Location = x.job.Location,
                JobType = x.job.JobType.ToString().ToLower(), // as requested like full_time
                PublishedAt = x.job.PublishedAt,
                IsSaved = false 
            }).ToList();

            if (userId.HasValue && jobCards.Any())
            {
                var jobIds = jobCards.Select(j => j.Id).ToList();
                var savedJobIds = await _context.UserSavedJobs
                    .Where(usj => usj.UserId == userId.Value && jobIds.Contains(usj.JobId))
                    .Select(usj => usj.JobId)
                    .ToListAsync();

                foreach (var job in jobCards)
                {
                    if (savedJobIds.Contains(job.Id))
                    {
                        job.IsSaved = true;
                    }
                }
            }

            var response = new PaginatedDataResponse<JobCardDto>
            {
                Data = jobCards,
                Meta = new PaginationMeta
                {
                    CurrentPage = query.Page,
                    PageSize = query.PageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize)
                }
            };

            return response;
        }

        public async Task<JobDetailViewDto?> GetJobDetailAsync(Guid jobId, Guid? userId = null)
        {
            var jobWithCompany = await (from job in _context.JobPostings
                                        join company in _context.Companies on job.CompanyId equals company.Id
                                        where job.Id == jobId && job.Status == JobStatus.PUBLISHED
                                        select new { job, company }).FirstOrDefaultAsync();

            if (jobWithCompany == null)
            {
                return null;
            }

            var skillIds = await _context.JobSkillRequirements
                .Where(jsr => jsr.JobId == jobId)
                .Select(jsr => jsr.SkillId)
                .ToListAsync();

            var skills = await _context.Skills
                .Where(s => skillIds.Contains(s.Id))
                .Select(s => s.Name)
                .ToListAsync();

            bool isSaved = false;
            if (userId.HasValue)
            {
                isSaved = await _context.UserSavedJobs
                    .AnyAsync(usj => usj.UserId == userId.Value && usj.JobId == jobId);
            }

            return new JobDetailViewDto
            {
                Id = jobWithCompany.job.Id,
                Title = jobWithCompany.job.Title,
                CompanyName = jobWithCompany.company.Name,
                CompanyId = jobWithCompany.company.Id,
                LogoUrl = jobWithCompany.company.LogoUrl,
                Description = jobWithCompany.job.Description,
                Responsibilities = jobWithCompany.job.Responsibilities,
                Requirements = jobWithCompany.job.Requirements,
                Benefits = jobWithCompany.job.Benefits,
                MinSalary = jobWithCompany.job.MinSalary,
                MaxSalary = jobWithCompany.job.MaxSalary,
                Currency = jobWithCompany.job.Currency,
                Location = jobWithCompany.job.Location,
                JobType = jobWithCompany.job.JobType.ToString().ToLower(),
                PublishedAt = jobWithCompany.job.PublishedAt,
                IsSaved = isSaved,
                Skills = skills
            };
        }
    }
}
