using System.Security.Claims;
using FluentAssertions;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ITHunterview.WebAPI.Tests.Controllers;

public sealed class JobPostingsControllerMatchingTests
{
    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchCvs_LegacyMixedBulkRouteIsGoneWithoutCallingLegacyMatchingEngine()
    {
        var controller = new JobPostingsController(
            Mock.Of<IJobPostingsUseCase>(),
            Mock.Of<IUserUseCase>(),
            Mock.Of<IJobAnalysisUseCase>(),
            Mock.Of<IRecruiterCvScanUseCase>(),
            Mock.Of<IRecruiterCvUnlockUseCase>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
                }
            }
        };

        var action = await controller.MatchCvs(Guid.NewGuid());

        action.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }
}
