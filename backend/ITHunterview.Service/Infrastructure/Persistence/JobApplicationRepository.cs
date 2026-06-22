using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
                        where application.JobId == jobId
                        orderby application.CreatedAt descending
                        select new ApplicantDto
                        {
                            Id = application.Id,
                            CandidateId = profile.Id,
                            CandidateName = profile.FirstName + " " + profile.LastName,
                            Email = user.Email,
                            Status = application.Status,
                            ApplyDate = application.CreatedAt,
                            AvatarUrl = profile.AvatarUrl,
                            CvId = application.CvId
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
    }
}
