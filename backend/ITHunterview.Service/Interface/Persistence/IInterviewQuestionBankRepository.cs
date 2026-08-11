using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;

namespace ITHunterview.Service.Interface.Persistence
{
    public interface IInterviewQuestionBankRepository
    {
        IQueryable<InterviewQuestionBank> GetQueryable();
        Task<PagedResult<InterviewQuestionBank>> GetPagedAsync(int pageIndex, int pageSize, string? industry, string? level);
        Task<InterviewQuestionBank?> GetByIdAsync(Guid id);
        Task AddAsync(InterviewQuestionBank entity);
        Task AddRangeAsync(IEnumerable<InterviewQuestionBank> entities);
        Task UpdateAsync(InterviewQuestionBank entity);
        Task DeleteAsync(InterviewQuestionBank entity);
    }
}
