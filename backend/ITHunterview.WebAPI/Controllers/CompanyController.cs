using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Company;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.WebAPI.Controllers
{
    [Route("api/companies")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyUseCase _companyUseCase;

        public CompanyController(ICompanyUseCase companyUseCase)
        {
            _companyUseCase = companyUseCase;
        }

        [HttpPost]
        [Authorize(Roles = "recruiter")]
        public async Task<ActionResult<ResponseBase<CompanyDto>>> Create([FromBody] CreateCompanyDto dto)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<CompanyDto>("Unauthorized"));
            }

            var company = await _companyUseCase.CreateCompanyAsync(dto, userId);
            return Ok(new ResponseBase<CompanyDto>(company, "Company created successfully"));
        }

        [HttpPost("{id:guid}/verify")]
        [Authorize(Roles = "recruiter")]
        public async Task<ActionResult<ResponseBase<CompanyDto>>> Verify(Guid id, [FromBody] VerifyCompanyDto dto)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<CompanyDto>("Unauthorized"));
            }

            var company = await _companyUseCase.VerifyCompanyAsync(id, dto, userId);
            return Ok(new ResponseBase<CompanyDto>(company, "Company verification info updated successfully"));
        }

        [HttpGet("me")]
        [Authorize(Roles = "recruiter")]
        public async Task<ActionResult<ResponseBase<CompanyDto>>> GetMyCompany()
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<CompanyDto>("Unauthorized"));
            }

            var company = await _companyUseCase.GetMyCompanyAsync(userId);
            if (company == null)
            {
                return NotFound(new ResponseBase<CompanyDto>("Company not found"));
            }

            return Ok(new ResponseBase<CompanyDto>(company, "Company retrieved successfully"));
        }

        [HttpGet]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetPagedCompanies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var result = await _companyUseCase.GetPagedCompaniesAsync(page, pageSize, search, status);
            return Ok(result);
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<ActionResult<ResponseBase<CompanyDto>>> UpdateStatus(Guid id, [FromBody] UpdateCompanyStatusDto dto)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<CompanyDto>("Unauthorized"));
            }

            var company = await _companyUseCase.UpdateCompanyStatusAsync(id, dto, userId);
            return Ok(new ResponseBase<CompanyDto>(company, "Company status updated successfully"));
        }

        [HttpPost("{id:guid}/update-request")]
        [Authorize(Roles = "recruiter")]
        public async Task<ActionResult<ResponseBase<CompanyDto>>> SubmitUpdateRequest(Guid id, [FromBody] VerifyCompanyDto dto)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<CompanyDto>("Unauthorized"));
            }

            var company = await _companyUseCase.SubmitUpdateRequestAsync(id, dto, userId);
            return Ok(new ResponseBase<CompanyDto>(company, "Company update request submitted successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "recruiter")]
        public async Task<ActionResult<ResponseBase<CompanyDto>>> Update(Guid id, [FromBody] UpdateCompanyDto dto)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseBase<CompanyDto>("Unauthorized"));
            }

            try
            {
                var company = await _companyUseCase.UpdateCompanyAsync(id, dto, userId);
                return Ok(new ResponseBase<CompanyDto>(company, "Company updated successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseBase<CompanyDto>(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseBase<CompanyDto>(ex.Message));
            }
        }
    }
}
