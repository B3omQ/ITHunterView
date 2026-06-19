using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Enums;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ITHunterviewContext _context;

        public SkillsController(ITHunterviewContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<List<SkillDto>>>> GetActiveSkills()
        {
            var skills = await (from s in _context.Skills
                                join c in _context.SkillCategories on s.CategoryId equals c.Id into sc
                                from category in sc.DefaultIfEmpty()
                                where s.Status == SkillStatus.ACTIVE
                                select new SkillDto
                                {
                                    Id = s.Id,
                                    Name = s.Name,
                                    CategoryId = s.CategoryId,
                                    CategoryName = category != null ? category.Name : string.Empty
                                }).ToListAsync();

            return Ok(new ResponseBase<List<SkillDto>>(skills));
        }
    }

    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
