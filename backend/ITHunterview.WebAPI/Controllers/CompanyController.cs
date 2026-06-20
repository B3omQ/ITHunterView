using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize(Roles = "Recruiter")]
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
        [Authorize(Roles = "Recruiter")]
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
    }
}
