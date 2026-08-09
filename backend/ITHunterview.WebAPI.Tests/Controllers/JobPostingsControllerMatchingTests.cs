using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ITHunterview.WebAPI.Tests.Controllers;

public sealed class JobPostingsControllerMatchingTests
{
    [Fact]
    public async Task GetJobMatches_OwnerReceivesTypedSummaryWithoutEvidencePayload()
    {
        var recruiterId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var matching = new Mock<ICvJobMatchingUseCase>();
        matching
            .Setup(useCase => useCase.GetJobMatchHistoryAsync(jobId, recruiterId, 1, 10))
            .ReturnsAsync(new PagedResult<MatchHistoryDto>
            {
                Items =
                [
                    new MatchHistoryDto
                    {
                        JobId = jobId,
                        CvId = cvId,
                        CandidateId = Guid.NewGuid(),
                        IsUnlocked = true,
                        ScorePercent = 81.8m,
                        ReportKind = "structured",
                        MatchMethod = "one_to_one_ai",
                        Status = "Completed",
                        UpdatedAt = DateTime.UtcNow
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            });

        var controller = CreateController(recruiterId, recruiterId, jobId, matching.Object);

        var action = await controller.GetJobMatches(jobId);

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.Should().Contain("\"scorePercent\":81.8");
        json.Should().Contain("\"reportKind\":\"structured\"");
        json.Should().Contain("\"matchMethod\":\"one_to_one_ai\"");
        json.Should().NotContain("matchDetails");
        json.Should().NotContain("evidence");
    }

    [Fact]
    public async Task GetJobMatches_NonOwnerIsForbiddenBeforeHistoryLookup()
    {
        var recruiterId = Guid.NewGuid();
        var jobOwnerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var matching = new Mock<ICvJobMatchingUseCase>();
        var controller = CreateController(recruiterId, jobOwnerId, jobId, matching.Object);

        var action = await controller.GetJobMatches(jobId);

        action.Result.Should().BeOfType<ForbidResult>();
        matching.Verify(
            useCase => useCase.GetJobMatchHistoryAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    private static JobPostingsController CreateController(
        Guid recruiterId,
        Guid jobOwnerId,
        Guid jobId,
        ICvJobMatchingUseCase matching)
    {
        var jobs = new Mock<IJobPostingsUseCase>();
        jobs.Setup(useCase => useCase.GetJobByIdAsync(jobId))
            .ReturnsAsync(new ResponseBase<JobPostingDetailDto>(new JobPostingDetailDto
            {
                Id = jobId,
                RecruiterId = jobOwnerId
            }));

        var users = new Mock<IUserUseCase>();
        users.Setup(useCase => useCase.ResolveRecruiterIdAsync(recruiterId.ToString()))
            .ReturnsAsync(recruiterId);

        var controller = new JobPostingsController(
            jobs.Object,
            users.Object,
            matching,
            Mock.Of<IHardcodeCvJobMatchingUseCase>(),
            Mock.Of<IJobAnalysisUseCase>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, recruiterId.ToString())],
                "test"))
            }
        };
        return controller;
    }
}
