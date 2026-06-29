using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;
using ITHunterview.Service.Interface.Persistence;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class JobApplicationRepository : IJobApplicationRepository
    {
        private readonly ITHunterviewContext _context;

        public JobApplicationRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ApplicantDto>> GetApplicantsByJobIdAsync(Guid jobId, int page, int pageSize)
        {
            var query = from application in _context.JobApplications
                        join profile in _context.CandidateProfiles on application.CandidateId equals profile.Id
                        join user in _context.Users on profile.UserId equals user.Id
                        join cv in _context.Cvs on application.CvId equals cv.Id into cvs
                        from c in cvs.DefaultIfEmpty()
                        where application.JobId == jobId
                        orderby application.CreatedAt descending
                        select new ApplicantDto
                        {
                            Id = application.Id,
                            CandidateId = profile.Id,
                            CandidateName = profile.FirstName + " " + profile.LastName,
                            Email = user.Email,
                            Phone = profile.Phone,
                            Status = application.Status,
                            ApplyDate = application.CreatedAt,
                            AvatarUrl = profile.AvatarUrl,
                            CvId = application.CvId,
                            CvUrl = c != null ? c.FileUrl : null,
                            CvFileName = c != null ? c.FileName : null
                        };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<ApplicantDto>
            {
                Items = items,
                TotalCount = totalCount,
                Total = totalCount,
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<JobApplications> CreateAsync(JobApplications entity)
        {
            await _context.JobApplications.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> HasCandidateAppliedAsync(Guid candidateId, Guid jobId)
        {
            return await _context.JobApplications.AnyAsync(x => x.CandidateId == candidateId && x.JobId == jobId);
        }

        public async Task<JobApplications?> GetByIdAsync(Guid id)
        {
            return await _context.JobApplications.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(JobApplications entity)
        {
            _context.JobApplications.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<JobApplicationDetailDto?> GetApplicationDetailAsync(Guid id)
        {
            var query = from application in _context.JobApplications
                        join profile in _context.CandidateProfiles on application.CandidateId equals profile.Id
                        join user in _context.Users on profile.UserId equals user.Id
                        where application.Id == id
                        select new { application, profile, user };

            var result = await query.FirstOrDefaultAsync();

            if (result == null) return null;

            var dto = new JobApplicationDetailDto
            {
                Id = result.application.Id,
                CandidateId = result.profile.Id,
                CandidateName = result.profile.FirstName + " " + result.profile.LastName,
                Email = result.user.Email,
                Phone = result.profile.Phone,
                Status = result.application.Status,
                ApplyDate = result.application.CreatedAt,
                AvatarUrl = result.profile.AvatarUrl,
                CoverLetter = string.IsNullOrWhiteSpace(result.application.CoverLetter) 
                    ? GenerateCoverLetterTemplate(result.profile, result.user) 
                    : result.application.CoverLetter,
                CvId = result.application.CvId
            };

            if (result.application.CvId.HasValue)
            {
                var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Id == result.application.CvId.Value);
                if (cv != null)
                {
                    dto.CvUrl = cv.FileUrl;
                    dto.CvFileName = cv.FileName;
                }
            }

            return dto;
        }

        public async Task<PagedResult<CandidateAppliedJobDto>> GetCandidateAppliedJobsAsync(Guid candidateId, int page, int pageSize)
        {
            var query = from application in _context.JobApplications
                        join job in _context.JobPostings on application.JobId equals job.Id
                        join company in _context.Companies on job.CompanyId equals company.Id
                        where application.CandidateId == candidateId
                        orderby application.CreatedAt descending
                        select new CandidateAppliedJobDto
                        {
                            Id = application.Id,
                            JobId = job.Id,
                            JobTitle = job.Title,
                            CompanyName = company.Name,
                            CompanyLogoUrl = company.LogoUrl,
                            Status = application.Status,
                            ApplyDate = application.CreatedAt
                        };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<CandidateAppliedJobDto>
            {
                Items = items,
                TotalCount = totalCount,
                Total = totalCount,
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private string GenerateCoverLetterTemplate(CandidateProfiles profile, User user)
        {
            var name = string.IsNullOrWhiteSpace(profile.FirstName) && string.IsNullOrWhiteSpace(profile.LastName) 
                ? "Candidate" 
                : $"{profile.FirstName} {profile.LastName}".Trim();
            
            var location = string.IsNullOrWhiteSpace(profile.Location) ? "Not specified" : profile.Location;
            var phone = string.IsNullOrWhiteSpace(profile.Phone) ? "Not provided" : profile.Phone;

            return $@"Dear Hiring Manager,

I am writing to express my interest in this position. Below is a brief summary of my profile:

- Name: {name}
- Email: {user.Email}
- Phone: {phone}
- Location: {location}

About Me:
{(string.IsNullOrWhiteSpace(profile.AboutMe) ? "I am an enthusiastic candidate looking for new opportunities to grow and contribute to your team." : profile.AboutMe)}

Please find my CV attached for more details regarding my skills and experiences. I look forward to the opportunity to discuss my application with you.

Sincerely,
{name}";
        }
    }
}
