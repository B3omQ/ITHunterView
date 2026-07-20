using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Interview;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Interface.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/interview")]
    [Authorize(Policy = "CandidateOnly")] // Only candidates can practice mock interviews
    public class InterviewController : ControllerBase
    {
        private readonly IInterviewUseCase _interviewUseCase;
        private readonly ISpeechToTextService _speechToTextService;

        public InterviewController(IInterviewUseCase interviewUseCase, ISpeechToTextService speechToTextService)
        {
            _interviewUseCase = interviewUseCase;
            _speechToTextService = speechToTextService;
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<ResponseBase<List<InterviewSessionDto>>>> GetSessions()
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<List<InterviewSessionDto>>("Unauthorized."));
            }

            try
            {
                var sessions = await _interviewUseCase.GetCandidateSessionsAsync(userId);
                return Ok(new ResponseBase<List<InterviewSessionDto>>(sessions, "Interview sessions retrieved successfully."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetSessions failed: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ResponseBase<List<InterviewSessionDto>>($"Error: {ex.Message}"));
            }
        }

        [HttpGet("sessions/{sessionId:guid}")]
        public async Task<ActionResult<ResponseBase<InterviewSessionDetailDto>>> GetSessionDetail(Guid sessionId)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<InterviewSessionDetailDto>("Unauthorized."));
            }

            try
            {
                var detail = await _interviewUseCase.GetSessionDetailAsync(sessionId, userId);
                return Ok(new ResponseBase<InterviewSessionDetailDto>(detail, "Session detail retrieved successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseBase<InterviewSessionDetailDto>(ex.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetSessionDetail failed: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ResponseBase<InterviewSessionDetailDto>($"Error: {ex.Message}"));
            }
        }

        [HttpPost("sessions")]
        public async Task<ActionResult<ResponseBase<InterviewSessionDto>>> CreateSession([FromBody] CreateInterviewSessionDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new ResponseBase<InterviewSessionDto>("Request body is required."));
            }

            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<InterviewSessionDto>("Unauthorized."));
            }

            try
            {
                var session = await _interviewUseCase.CreateSessionAsync(userId, dto);
                return Ok(new ResponseBase<InterviewSessionDto>(session, "Interview session created successfully."));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CreateSession failed: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ResponseBase<InterviewSessionDto>($"Error starting interview: {ex.Message}"));
            }
        }

        [HttpPost("sessions/{sessionId:guid}/reply")]
        public async Task<ActionResult<ResponseBase<InterviewAnswerDto>>> SubmitReply(Guid sessionId, [FromBody] SubmitReplyDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
            {
                return BadRequest(new ResponseBase<InterviewAnswerDto>("Response message is required."));
            }

            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<InterviewAnswerDto>("Unauthorized."));
            }

            try
            {
                var newTurn = await _interviewUseCase.SubmitReplyAsync(sessionId, userId, dto);
                return Ok(new ResponseBase<InterviewAnswerDto>(newTurn, "Reply evaluated and next question loaded."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseBase<InterviewAnswerDto>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseBase<InterviewAnswerDto>(ex.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SubmitReply failed: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ResponseBase<InterviewAnswerDto>($"Error submitting reply: {ex.Message}"));
            }
        }

        [HttpPost("sessions/{sessionId:guid}/switch-model")]
        public async Task<ActionResult<ResponseBase<string>>> SwitchModel(Guid sessionId, [FromBody] SwitchModelDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.AiProvider))
            {
                return BadRequest(new ResponseBase<string>("AiProvider is required."));
            }

            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<string>("Unauthorized."));
            }

            try
            {
                await _interviewUseCase.SwitchModelAsync(sessionId, userId, dto);
                return Ok(new ResponseBase<string>(dto.AiProvider, $"Successfully switched session model provider to {dto.AiProvider}."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseBase<string>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<string>($"Error: {ex.Message}"));
            }
        }

        [HttpPost("sessions/{sessionId:guid}/complete")]
        public async Task<ActionResult<ResponseBase<string>>> CompleteSession(Guid sessionId)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<string>("Unauthorized."));
            }

            try
            {
                await _interviewUseCase.CompleteSessionAsync(sessionId, userId);
                return Ok(new ResponseBase<string>(sessionId.ToString(), "Interview completed successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseBase<string>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<string>($"Error: {ex.Message}"));
            }
        }

        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<ActionResult<ResponseBase<string>>> DeleteSession(Guid sessionId)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<string>("Unauthorized."));
            }

            try
            {
                await _interviewUseCase.DeleteSessionAsync(sessionId, userId);
                return Ok(new ResponseBase<string>(sessionId.ToString(), "Interview session deleted successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseBase<string>(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<string>($"Error: {ex.Message}"));
            }
        }

        [HttpPost("transcribe")]
        public async Task<ActionResult<ResponseBase<string>>> TranscribeAudio(IFormFile audio, [FromQuery] string? lang = "vi")
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new ResponseBase<string>("No audio file was uploaded."));
            }

            try
            {
                using var ms = new MemoryStream();
                await audio.CopyToAsync(ms);
                var audioBytes = ms.ToArray();

                var transcription = await _speechToTextService.TranscribeAudioAsync(audioBytes, audio.ContentType, lang);
                return Ok(new ResponseBase<string>(transcription, "Speech translated to text successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<string>($"Transcription error: {ex.Message}"));
            }
        }
    }
}
