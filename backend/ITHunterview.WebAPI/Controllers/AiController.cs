using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Ai;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Interface.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiConfigUseCase _aiConfigUseCase;
        private readonly IAiService _aiService;

        public AiController(IAiConfigUseCase aiConfigUseCase, IAiService aiService)
        {
            _aiConfigUseCase = aiConfigUseCase;
            _aiService = aiService;
        }

        [HttpGet("configs")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<ActionResult<ResponseBase<AiConfigResponseDto>>> GetConfigs()
        {
            try
            {
                var configs = await _aiConfigUseCase.GetAiConfigAsync();
                return Ok(new ResponseBase<AiConfigResponseDto>(configs, "AI Configurations retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<AiConfigResponseDto>($"Error retrieving configs: {ex.Message}"));
            }
        }

        [HttpPost("configs/update")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<ActionResult<ResponseBase<string>>> UpdateAiConfig([FromBody] UpdateAiConfigRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ProviderName))
            {
                return BadRequest(new ResponseBase<string>("ProviderName is required."));
            }

            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<string>("Unauthorized."));
            }

            try
            {
                await _aiConfigUseCase.UpdateAiConfigAsync(userId, dto);
                return Ok(new ResponseBase<string>(dto.ProviderName, $"AI configurations successfully updated."));
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new ResponseBase<string>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<string>($"Error updating AI config: {ex.Message}"));
            }
        }

        [HttpPost("test-connection")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<ActionResult<ResponseBase<TestConnectionResponseDto>>> TestConnection([FromBody] TestConnectionRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ProviderName))
            {
                return BadRequest(new ResponseBase<TestConnectionResponseDto>("ProviderName is required."));
            }

            try
            {
                var result = await _aiConfigUseCase.TestConnectionAsync(dto.ProviderName, dto.Prompt ?? "Hello");
                if (result.Success)
                {
                    return Ok(new ResponseBase<TestConnectionResponseDto>(result, "Test connection succeeded."));
                }
                return BadRequest(new ResponseBase<TestConnectionResponseDto>(result, "Test connection failed."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<TestConnectionResponseDto>($"Unexpected error: {ex.Message}"));
            }
        }

        [HttpPost("generate")]
        [Authorize(Policy = "AllRoles")]
        public async Task<ActionResult<ResponseBase<string>>> GenerateText([FromBody] GenerateRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Prompt))
            {
                return BadRequest(new ResponseBase<string>("Prompt is required."));
            }

            try
            {
                var responseText = await _aiService.GenerateTextAsync(dto.Prompt, dto.SystemPrompt);
                return Ok(new ResponseBase<string>(responseText, "Text generated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<string>($"Error generating text: {ex.Message}"));
            }
        }
    }

    public class GenerateRequestDto
    {
        public string Prompt { get; set; }
        public string SystemPrompt { get; set; }
    }
}
