using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Dashboard;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ITHunterview.Service.UseCase;

public class StaffDashboardUseCase : IStaffDashboardUseCase
{
    private readonly IInterviewQuestionBankRepository _questionBankRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;

    public StaffDashboardUseCase(
        IInterviewQuestionBankRepository questionBankRepository,
        ICompanyRepository companyRepository,
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository)
    {
        _questionBankRepository = questionBankRepository;
        _companyRepository = companyRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
    }

    public async Task<StaffDashboardResponseDto> GetDashboardAsync(DashboardFilterRequest request)
    {
        var questionsQuery = _questionBankRepository.GetQueryable();
        var usersQuery = _userRepository.GetQueryable();
        var companiesQuery = _companyRepository.GetQueryable();
        var logsQuery = _auditLogRepository.GetQueryable();

        // Apply date filters only to entities that have CreatedAt
        if (request.FromDate.HasValue && !request.ToDate.HasValue)
        {
            usersQuery = usersQuery.Where(x => x.CreatedAt >= request.FromDate.Value);
            companiesQuery = companiesQuery.Where(x => x.CreatedAt >= request.FromDate.Value);
            logsQuery = logsQuery.Where(x => x.CreatedAt >= request.FromDate.Value);
        }
        else if (!request.FromDate.HasValue && request.ToDate.HasValue)
        {
            usersQuery = usersQuery.Where(x => x.CreatedAt <= request.ToDate.Value);
            companiesQuery = companiesQuery.Where(x => x.CreatedAt <= request.ToDate.Value);
            logsQuery = logsQuery.Where(x => x.CreatedAt <= request.ToDate.Value);
        }
        else if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            usersQuery = usersQuery.Where(x => x.CreatedAt >= request.FromDate.Value && x.CreatedAt <= request.ToDate.Value);
            companiesQuery = companiesQuery.Where(x => x.CreatedAt >= request.FromDate.Value && x.CreatedAt <= request.ToDate.Value);
            logsQuery = logsQuery.Where(x => x.CreatedAt >= request.FromDate.Value && x.CreatedAt <= request.ToDate.Value);
        }
        else
        {
            if (request.Year.HasValue)
            {
                usersQuery = usersQuery.Where(x => x.CreatedAt.Year == request.Year.Value);
                companiesQuery = companiesQuery.Where(x => x.CreatedAt.Year == request.Year.Value);
                logsQuery = logsQuery.Where(x => x.CreatedAt.Year == request.Year.Value);
            }
            if (request.Month.HasValue)
            {
                usersQuery = usersQuery.Where(x => x.CreatedAt.Month == request.Month.Value);
                companiesQuery = companiesQuery.Where(x => x.CreatedAt.Month == request.Month.Value);
                logsQuery = logsQuery.Where(x => x.CreatedAt.Month == request.Month.Value);
            }
        }

        var totalQuestions = await questionsQuery.CountAsync();
        
        // Mock new questions logic: count within the last 7 days of the filtered set or just a subset
        var newQuestions = await questionsQuery.CountAsync(); 
        
        var pendingCompanies = await companiesQuery.Where(x => x.Status.ToString() == "PENDING").CountAsync();
        
        // Mock audit warnings
        var auditWarnings = await logsQuery.CountAsync(); // Replace with actual warning criteria if enum exists

        // Questions by Level mock mapping (assuming Level is string or enum)
        var levels = new[] { "Intern", "Fresher", "Junior", "Middle", "Senior" };
        var questionsByLevel = levels.Select(l => new QuestionLevelDto { Level = l, Count = totalQuestions / 5 }).ToList();

        // Questions by Category mock
        var categories = new[] { "Frontend", "Backend", "DevOps", "Data", "Mobile" };
        var questionsByCategory = categories.Select(c => new QuestionCategoryDto { Name = c, Value = totalQuestions / 5 }).ToList();

        // Companies Verification chart
        var companyVerifications = new List<CompanyVerificationDto>
        {
            new CompanyVerificationDto { Week = "Week 1", New = 10, Verified = 5 },
            new CompanyVerificationDto { Week = "Week 2", New = 15, Verified = 8 }
        };

        return new StaffDashboardResponseDto
        {
            TotalQuestions = totalQuestions,
            NewQuestions = newQuestions,
            PendingCompanies = pendingCompanies,
            AuditWarnings = auditWarnings,
            QuestionsByCategory = questionsByCategory,
            QuestionsByLevel = questionsByLevel,
            CompanyVerifications = companyVerifications
        };
    }
}
