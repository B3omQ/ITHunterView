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
        var legacyMatching = new Mock<ICvJobMatchingUseCase>(MockBehavior.Strict);
        var controller = new JobPostingsController(
            Mock.Of<IJobPostingsUseCase>(),
            Mock.Of<IUserUseCase>(),
            legacyMatching.Object,
            Mock.Of<IHardcodeCvJobMatchingUseCase>(),
            Mock.Of<IJobAnalysisUseCase>(),
            Mock.Of<IRecruiterCvScanUseCase>())
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
        legacyMatching.Verify(useCase => useCase.MatchJobWithAllCvsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }
}
