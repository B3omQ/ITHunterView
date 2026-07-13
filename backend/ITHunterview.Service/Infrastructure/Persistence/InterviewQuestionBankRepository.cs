using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.Persistence;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class InterviewQuestionBankRepository : IInterviewQuestionBankRepository
    {
        private readonly ITHunterviewContext _context;

        public InterviewQuestionBankRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<InterviewQuestionBank>> GetPagedAsync(int pageIndex, int pageSize, string? industry, string? level)
        {
            var query = _context.InterviewQuestionBank.AsQueryable();

            if (!string.IsNullOrEmpty(industry))
            {
                query = query.Where(x => x.Industry == industry);
            }

            if (!string.IsNullOrEmpty(level))
            {
                query = query.Where(x => x.Level == level);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<InterviewQuestionBank>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pageIndex,
                PageSize = pageSize
            };
        }

        public Task<InterviewQuestionBank?> GetByIdAsync(Guid id)
        {
            return _context.InterviewQuestionBank.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(InterviewQuestionBank entity)
        {
            _context.InterviewQuestionBank.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<InterviewQuestionBank> entities)
        {
            await _context.InterviewQuestionBank.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(InterviewQuestionBank entity)
        {
            _context.InterviewQuestionBank.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(InterviewQuestionBank entity)
        {
            _context.InterviewQuestionBank.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
