using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class JobCategoryRepository : IJobCategoryRepository
    {
        private readonly ITHunterviewContext _context;

        public JobCategoryRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public Task<List<JobCategories>> GetCategoriesAsync()
        {
            return _context.JobCategories.ToListAsync();
        }
    }
}
