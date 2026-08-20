using System.Security.Claims;
using FluentAssertions;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ITHunterview.WebAPI.Tests.Controllers;

public sealed class CvControllerCandidateScanTests
{
    [Fact]
    [Trait("Requirement", "R-02")]
    public async Task MatchJobsHardcode_CandidateOwner_ReturnsAcceptedRunId()
    {
        var userId = Guid.NewGuid(); var cvId = Guid.NewGuid(); var runId = Guid.NewGuid();
        var scan = new Mock<ICandidateJobScanUseCase>();
        scan.Setup(x => x.CreateRunAsync(userId, cvId, It.IsAny<CancellationToken>())).ReturnsAsync(new CandidateJobScanAcceptedDto(runId, "Pending"));
        var result = await CreateController(userId, scan.Object).MatchJobsHardcode(cvId, CancellationToken.None);
        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        Assert.IsType<ResponseBase<CandidateJobScanAcceptedDto>>(accepted.Value).Data!.RunId.Should().Be(runId);
    }

    [Fact]
    [Trait("Requirement", "R-07")]
    public async Task MatchJobsHardcode_ForeignCv_ReturnsNotFoundWithoutQueue()
    {
        var scan = new Mock<ICandidateJobScanUseCase>();
        scan.Setup(x => x.CreateRunAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new KeyNotFoundException());
        var result = await CreateController(Guid.NewGuid(), scan.Object).MatchJobsHardcode(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    [Trait("Requirement", "R-07")]
    public async Task GetLatestJobScan_OtherCandidate_CannotReadRun()
    {
        var scan = new Mock<ICandidateJobScanUseCase>();
        scan.Setup(x => x.GetLatestSuccessfulAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CandidateJobScanResultDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });
        var result = await CreateController(Guid.NewGuid(), scan.Object).GetLatestJobScan(Guid.NewGuid(), 1, 20, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    public async Task MatchJobsAiBulk_ReturnsGoneWithoutQueueOrEngineCall()
    {
        var result = await CreateController(Guid.NewGuid(), Mock.Of<ICandidateJobScanUseCase>()).MatchJobs(Guid.NewGuid());
        Assert.IsType<ObjectResult>(result.Result).StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    private static CvController CreateController(Guid userId, ICandidateJobScanUseCase scan) => new(
        Mock.Of<ICvUseCase>(), Mock.Of<ICvJobMatchingUseCase>(),
        Mock.Of<ICvJdMatchingSubmissionUseCase>(), Mock.Of<ICvJdMatchingRetryUseCase>(), Mock.Of<ICvJdMatchingHistoryUseCase>(),
        Mock.Of<ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService>(),
        Mock.Of<ICandidateFeatureUsageUseCase>(), Mock.Of<IMatchingInputPreflightUseCase>(),
        scan)
    { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("userId", userId.ToString())], "test")) } } };
}
