using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.InterviewQuestionBank;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/interview-questions")]
    [ApiController]
    [Authorize(Policy = "StaffOrAdmin")]
    public class InterviewQuestionBankController : ControllerBase
    {
        private readonly IInterviewQuestionBankUseCase _useCase;

        public InterviewQuestionBankController(IInterviewQuestionBankUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseBase<PagedResult<QuestionBankDto>>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? industry = null,
            [FromQuery] string? level = null)
        {
            var result = await _useCase.GetPagedAsync(page, pageSize, industry, level);
            return new ResponseBase<PagedResult<QuestionBankDto>>(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResponseBase<QuestionBankDto>>> GetById(Guid id)
        {
            var result = await _useCase.GetByIdAsync(id);
            return new ResponseBase<QuestionBankDto>(result);
        }

        [HttpPost]
        public async Task<ActionResult<QuestionBankDto>> Create([FromBody] CreateQuestionBankDto dto)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var result = await _useCase.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPost("import")]
        public async Task<ActionResult<int>> ImportFromExcel([FromForm] string industry, [FromForm] string level, IFormFile file)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            try
            {
                var result = await _useCase.ImportFromExcelAsync(industry, level, file, userId);
                return Ok(new { importedCount = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ResponseBase<QuestionBankDto>>> Update(Guid id, [FromBody] UpdateQuestionBankDto dto)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var result = await _useCase.UpdateAsync(id, dto, userId);
            return new ResponseBase<QuestionBankDto>(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ResponseBase<object>>> Delete(Guid id)
        {
            await _useCase.DeleteAsync(id);
            return new ResponseBase<object>(null, "Question deleted successfully.");
        }
    }
}
