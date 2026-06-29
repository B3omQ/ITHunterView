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
            var jobsQuery = _context.JobPostings.AsQueryable();

            if (query.Status.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.Status == query.Status.Value);
            }
            else
            {
                // Default to PUBLISHED for backwards compatibility if not provided
                jobsQuery = jobsQuery.Where(j => j.Status == JobStatus.PUBLISHED);
            }

            if (query.MinSalary.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.MinSalary >= query.MinSalary.Value || j.MaxSalary >= query.MinSalary.Value);
            }

            if (query.MaxSalary.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.MinSalary <= query.MaxSalary.Value || j.MaxSalary <= query.MaxSalary.Value);
            }

            if (query.PostedWithinDays.HasValue)
            {
                var thresholdDate = DateTime.UtcNow.AddDays(-query.PostedWithinDays.Value);
                jobsQuery = jobsQuery.Where(j => j.PublishedAt >= thresholdDate);
            }

            if (!string.IsNullOrEmpty(query.ProvinceCode))
            {
                var lowerProvinceCode = query.ProvinceCode.ToLower();
                jobsQuery = jobsQuery.Where(j => j.ProvinceCode.ToLower().Contains(lowerProvinceCode));
            }

            if (query.Levels != null && query.Levels.Any())
            {
                jobsQuery = jobsQuery.Where(j => j.Level != null && query.Levels.Contains(j.Level));
            }

            if (query.JobExpertises != null && query.JobExpertises.Any())
            {
                jobsQuery = jobsQuery.Where(j => j.JobExpertise != null && query.JobExpertises.Contains(j.JobExpertise));
            }

            if (query.WorkingModels != null && query.WorkingModels.Any())
            {
                jobsQuery = jobsQuery.Where(j => j.WorkingModel != null && query.WorkingModels.Contains(j.WorkingModel));
            }

            if (query.JobDomains != null && query.JobDomains.Any())
            {
                jobsQuery = jobsQuery.Where(j => j.JobDomain != null && j.JobDomain.Any(d => query.JobDomains.Contains(d)));
            }



            var queryable = from job in jobsQuery
                            join company in _context.Companies on job.CompanyId equals company.Id
                            select new { job, company };

            if (!string.IsNullOrEmpty(query.CompanyName))
            {
                var lowerCompany = query.CompanyName.ToLower();
                queryable = queryable.Where(x => x.company.Name != null && x.company.Name.ToLower().Contains(lowerCompany));
            }

            if (query.CompanyIndustries != null && query.CompanyIndustries.Any())
            {
                queryable = queryable.Where(x => x.company.Industry != null && query.CompanyIndustries.Contains(x.company.Industry));
            }

            if (query.CompanyTypes != null && query.CompanyTypes.Any())
            {
                queryable = queryable.Where(x => x.company.CompanyType != null && query.CompanyTypes.Contains(x.company.CompanyType));
            }

            if (!string.IsNullOrEmpty(query.Skill))
            {
                var lowerSkill = query.Skill.ToLower();
                queryable = from q in queryable
                            join jsr in _context.JobSkillRequirements on q.job.Id equals jsr.JobId
                            join s in _context.Skills on jsr.SkillId equals s.Id
                            where s.Name.ToLower().Contains(lowerSkill)
                            select q;
            }

            if (!string.IsNullOrEmpty(query.Keyword))
            {
                var lowerKeyword = query.Keyword.ToLower();
                queryable = queryable.Where(x => 
                    (x.job.Title != null && x.job.Title.ToLower().Contains(lowerKeyword)) || 
                    (x.company.Name != null && x.company.Name.ToLower().Contains(lowerKeyword)) ||
                    _context.JobSkillRequirements.Any(jsr => jsr.JobId == x.job.Id && 
                        _context.Skills.Any(s => s.Id == jsr.SkillId && s.Name.ToLower().Contains(lowerKeyword)))
                );
            }

            var totalItems = await queryable.CountAsync();

            var pagedResults = await queryable
                .OrderByDescending(x => x.job.PublishedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var pagedJobIds = pagedResults.Select(x => x.job.Id).ToList();

            var jobSkills = await (from jsr in _context.JobSkillRequirements
                                   join s in _context.Skills on jsr.SkillId equals s.Id
                                   where pagedJobIds.Contains(jsr.JobId)
                                   select new { jsr.JobId, s.Name })
                                  .ToListAsync();
            
            var skillLookup = jobSkills.GroupBy(x => x.JobId)
                                       .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

            var jobCards = pagedResults.Select(x => new JobCardDto
            {
                Id = x.job.Id,
                Title = x.job.Title,
                CompanyName = x.company.Name,
                LogoUrl = x.company.LogoUrl,
                MinSalary = x.job.MinSalary,
                MaxSalary = x.job.MaxSalary,
                Currency = x.job.Currency,
                ProvinceCode = x.job.ProvinceCode,
                DetailedLocation = x.job.DetailedLocation,
                Level = x.job.Level,
                WorkingModel = x.job.WorkingModel,
                JobExpertise = x.job.JobExpertise,
                JobDomain = x.job.JobDomain,
                PublishedAt = x.job.PublishedAt,
                IsSaved = false,
                Skills = skillLookup.ContainsKey(x.job.Id) ? skillLookup[x.job.Id] : new List<string>()
            }).ToList();

            if (userId.HasValue && pagedJobIds.Any())
            {
                var savedJobIds = await _context.UserSavedJobs
                    .Where(usj => usj.UserId == userId.Value && pagedJobIds.Contains(usj.JobId))
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
                ProvinceCode = jobWithCompany.job.ProvinceCode,
                DetailedLocation = jobWithCompany.job.DetailedLocation,
                Level = jobWithCompany.job.Level,
                WorkingModel = jobWithCompany.job.WorkingModel,
                JobExpertise = jobWithCompany.job.JobExpertise,
                JobDomain = jobWithCompany.job.JobDomain,
                PublishedAt = jobWithCompany.job.PublishedAt,
                IsSaved = isSaved,
                Skills = skills
            };
        }
    }
}
