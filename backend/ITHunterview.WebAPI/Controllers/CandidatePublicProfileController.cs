using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/recruiter/candidates")]
    [Authorize(Roles = "recruiter")]
    public class CandidatePublicProfileController : ControllerBase
    {
        private readonly ICandidatePublicProfileUseCase _publicProfileUseCase;

        public CandidatePublicProfileController(ICandidatePublicProfileUseCase publicProfileUseCase)
        {
            _publicProfileUseCase = publicProfileUseCase;
        }

        [HttpGet("{id:guid}/profile")]
        public async Task<ActionResult<ResponseBase<CandidateFullProfileDto>>> GetPublicProfile(Guid id)
        {
            try
            {
                var result = await _publicProfileUseCase.GetPublicProfileAsync(id);
                return Ok(new ResponseBase<CandidateFullProfileDto>(result, "Candidate profile retrieved"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseBase<CandidateFullProfileDto>(null, ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseBase<CandidateFullProfileDto>(null, ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseBase<CandidateFullProfileDto>(null, ex.Message));
            }
        }
    }
}
