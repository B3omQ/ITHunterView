using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Dashboard;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ITHunterview.Service.UseCase;

    public class RecruiterDashboardUseCase : IRecruiterDashboardUseCase
    {
        private readonly IJobPostingRepository _jobPostingRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IUserRepository _userRepository;

        public RecruiterDashboardUseCase(
            IJobPostingRepository jobPostingRepository,
            IJobApplicationRepository jobApplicationRepository,
            IUserRepository userRepository)
        {
            _jobPostingRepository = jobPostingRepository;
            _jobApplicationRepository = jobApplicationRepository;
            _userRepository = userRepository;
        }

        public async Task<RecruiterDashboardResponseDto> GetDashboardAsync(DashboardFilterRequest request, Guid recruiterId)
        {
            var user = await _userRepository.GetUserWithRoleAsync(recruiterId);
            if (user == null || user.RecruiterProfile?.CompanyId == null)
            {
                throw new UnauthorizedAccessException("Recruiter is not associated with any company.");
            }

            var companyId = user.RecruiterProfile.CompanyId.Value;

        var jobsQuery = _jobPostingRepository.GetQueryable().Where(x => x.CompanyId == companyId);
        var jobIds = await jobsQuery.Select(x => x.Id).ToListAsync();
        var appsQuery = _jobApplicationRepository.GetQueryable().Where(x => jobIds.Contains(x.JobId));

        // Apply filters to appsQuery
        if (request.FromDate.HasValue && !request.ToDate.HasValue)
        {
            appsQuery = appsQuery.Where(x => x.CreatedAt >= request.FromDate.Value);
        }
        else if (!request.FromDate.HasValue && request.ToDate.HasValue)
        {
            appsQuery = appsQuery.Where(x => x.CreatedAt <= request.ToDate.Value);
        }
        else if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            appsQuery = appsQuery.Where(x => x.CreatedAt >= request.FromDate.Value && x.CreatedAt <= request.ToDate.Value);
        }
        else
        {
            if (request.Year.HasValue)
            {
                appsQuery = appsQuery.Where(x => x.CreatedAt.Year == request.Year.Value);
            }
            if (request.Month.HasValue)
            {
                appsQuery = appsQuery.Where(x => x.CreatedAt.Month == request.Month.Value);
            }
        }

        var activeJobs = await jobsQuery.Where(x => x.Status == JobStatus.PUBLISHED).CountAsync();
        var totalApplications = await appsQuery.CountAsync();

        var appsByDay = await appsQuery
            .GroupBy(x => x.CreatedAt.Day)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync();
            
        var dailyApplications = appsByDay.Select(x => new DailyApplicationDto
        {
            Day = x.Day.ToString(),
            Apps = x.Count
        }).ToList();

        var appsByStatus = await appsQuery
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var applicationStatus = appsByStatus.Select(x => new ApplicationStatusDto
        {
            Name = x.Status.ToString(),
            Value = x.Count
        }).ToList();

        var appsByJob = await appsQuery
            .GroupBy(x => x.JobId)
            .Select(g => new { JobId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var topJobsList = await jobsQuery.ToListAsync();

        var topJobs = appsByJob.Select(x => new TopJobDto
        {
            Title = topJobsList.FirstOrDefault(j => j.Id == x.JobId)?.Title ?? "Unknown Job",
            Applicants = x.Count
        }).ToList();

        return new RecruiterDashboardResponseDto
        {
            ActiveJobs = activeJobs,
            TotalApplications = totalApplications,
            DailyApplications = dailyApplications,
            ApplicationStatus = applicationStatus,
            TopJobs = topJobs
        };
    }
}
