using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ITHunterview.WebAPI.Tests.Controllers;

public sealed class JobPostingsControllerRecruiterScanTests
{
    [Fact]
    [Trait("Requirement", "R-03")]
    public async Task MatchCvsHardcode_OwnerStartsOnlyRecruiterScanWithAuthenticatedUserId()
    {
        var recruiterUserId = Guid.NewGuid();
        var recruiterProfileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var scan = new Mock<IRecruiterCvScanUseCase>(MockBehavior.Strict);
        scan.Setup(useCase => useCase.ScanAsync(recruiterUserId, jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecruiterCvScanRunDto { RunId = Guid.NewGuid(), JobId = jobId, Status = "Completed" });
        var controller = CreateController(recruiterUserId, recruiterProfileId, recruiterProfileId, jobId, scan.Object);

        var action = await controller.MatchCvsHardcode(jobId, CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>();
        scan.Verify(useCase => useCase.ScanAsync(recruiterUserId, jobId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-03")]
    public async Task MatchCvsHardcode_NonOwnerIsForbiddenBeforeScan()
    {
        var callerUserId = Guid.NewGuid();
        var callerProfileId = Guid.NewGuid();
        var jobOwnerProfileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var scan = new Mock<IRecruiterCvScanUseCase>(MockBehavior.Strict);
        scan.Setup(useCase => useCase.ScanAsync(callerUserId, jobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController(callerUserId, callerProfileId, jobOwnerProfileId, jobId, scan.Object);

        var action = () => controller.MatchCvsHardcode(jobId, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        scan.Verify(useCase => useCase.ScanAsync(callerUserId, jobId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchCvs_LegacyAiVectorRouteReturnsGoneWithoutCallingEngine()
    {
        var recruiterUserId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var legacy = new Mock<ICvJobMatchingUseCase>(MockBehavior.Strict);
        var controller = CreateController(recruiterUserId, profileId, profileId, jobId, Mock.Of<IRecruiterCvScanUseCase>(), legacy.Object);

        var action = await controller.MatchCvs(jobId);

        action.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        legacy.Verify(useCase => useCase.MatchJobWithAllCvsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Requirement", "R-06")]
    [Trait("Requirement", "R-10")]
    public async Task GetJobMatches_OwnerReceivesLatestMaskedRecruiterSnapshotWithoutStableIdentity()
    {
        var recruiterUserId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        const string filename = "private-candidate-resume.pdf";
        const string fileUrl = "https://files.example.test/private-candidate-resume.pdf";
        var scan = new Mock<IRecruiterCvScanUseCase>(MockBehavior.Strict);
        scan.Setup(useCase => useCase.GetLatestSuccessfulAsync(recruiterUserId, jobId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<RecruiterCvScanResultDto>
            {
                Items = [new RecruiterCvScanResultDto { ScanResultId = Guid.NewGuid(), AnonymousLabel = "Candidate #1", Rank = 1, MatchScore = 80m, MatchDetails = "hardcode", IsUnlocked = false, UnlockCost = 1 }],
                TotalCount = 1, Page = 1, PageSize = 20
            });
        var controller = CreateController(recruiterUserId, profileId, profileId, jobId, scan.Object);

        var action = await controller.GetJobMatches(jobId, 1, 20, CancellationToken.None);

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using (new AssertionScope())
        {
            json.Should().Contain("scanResultId");
            json.Should().NotContain(candidateId.ToString());
            json.Should().NotContain(cvId.ToString());
            json.Should().NotContain(filename);
            json.Should().NotContain(fileUrl);
            json.Should().NotContain("fileUrl");
            json.Should().NotContain("candidateId");
            json.Should().NotContain("cvId");
        }
    }

    private static JobPostingsController CreateController(
        Guid recruiterUserId,
        Guid resolvedRecruiterProfileId,
        Guid jobOwnerProfileId,
        Guid jobId,
        IRecruiterCvScanUseCase scan,
        ICvJobMatchingUseCase? legacyMatching = null)
    {
        var jobs = new Mock<IJobPostingsUseCase>();
        jobs.Setup(useCase => useCase.GetJobByIdAsync(jobId)).ReturnsAsync(new ResponseBase<JobPostingDetailDto>(new JobPostingDetailDto { Id = jobId, RecruiterId = jobOwnerProfileId }));
        var users = new Mock<IUserUseCase>();
        users.Setup(useCase => useCase.ResolveRecruiterIdAsync(recruiterUserId.ToString())).ReturnsAsync(resolvedRecruiterProfileId);
        var controller = new JobPostingsController(jobs.Object, users.Object, legacyMatching ?? Mock.Of<ICvJobMatchingUseCase>(), Mock.Of<IHardcodeCvJobMatchingUseCase>(), Mock.Of<IJobAnalysisUseCase>(), scan)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, recruiterUserId.ToString()), new Claim("userId", recruiterUserId.ToString())], "test")) } }
        };
        return controller;
    }
}
