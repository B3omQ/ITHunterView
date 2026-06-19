using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobCategoriesController : ControllerBase
    {
        private readonly ITHunterviewContext _context;

        public JobCategoriesController(ITHunterviewContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<CategoryDto>>>> GetCategories()
        {
            var categories = await _context.JobCategories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentId = c.ParentId
                }).ToListAsync();

            return Ok(new ResponseBase<List<CategoryDto>>(categories));
        }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }
}
