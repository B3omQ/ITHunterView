using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ITHunterview.WebAPI.Tests.Controllers;

public class CvControllerMatchingTests
{
    [Fact]
    public async Task MatchJd_SubmitsWithIdempotencyKeyAndReturnsAccepted()
    {
        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var submission = new Mock<ICvJdMatchingSubmissionUseCase>();
        var matching = new Mock<ICvJobMatchingUseCase>();
        submission
            .Setup(x => x.SubmitAsync(userId, It.IsAny<MatchingRequestDto>(), "request-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MatchingSubmissionResult(matchId, false));

        var controller = CreateController(userId, matching.Object, submission.Object);
        controller.HttpContext.Request.Headers["Idempotency-Key"] = "request-123";

        var result = await controller.MatchJd(new MatchingRequestDto
        {
            CvText = new string('c', 100),
            RawJdText = new string('j', 100)
        }, CancellationToken.None);

        Assert.IsType<AcceptedResult>(result.Result);
        submission.Verify(x => x.SubmitAsync(userId, It.IsAny<MatchingRequestDto>(), "request-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MatchJd_WhenPreflightFails_DoesNotConsumeFeature()
    {
        var userId = Guid.NewGuid();
        var submission = new Mock<ICvJdMatchingSubmissionUseCase>();
        submission.Setup(x => x.SubmitAsync(userId, It.IsAny<MatchingRequestDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("MULTIPLE_CV_SOURCES"));
        var controller = CreateController(userId, Mock.Of<ICvJobMatchingUseCase>(), submission.Object);

        var result = await controller.MatchJd(new MatchingRequestDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void MatchJd_RequiresCandidateOnlyPolicyAndCapsRequestBody()
    {
        var method = typeof(CvController).GetMethod(nameof(CvController.MatchJd), BindingFlags.Instance | BindingFlags.Public)!;
        var authorization = method.GetCustomAttribute<AuthorizeAttribute>();
        var requestSize = method.GetCustomAttribute<RequestSizeLimitAttribute>();

        authorization!.Policy.Should().Be("CandidateOnly");
        ((IRequestSizeLimitMetadata)requestSize!).MaxRequestBodySize.Should().Be(524288);

        var resultMethod = typeof(CvController).GetMethod(nameof(CvController.GetMatchResult), BindingFlags.Instance | BindingFlags.Public)!;
        resultMethod.GetCustomAttribute<AuthorizeAttribute>()!.Policy.Should().Be("CandidateOnly");
    }

    [Fact]
    public async Task RetryMatch_SubmitsWithIdempotencyKeyAndReturnsAccepted()
    {
        var userId = Guid.NewGuid();
        var failedJobId = Guid.NewGuid();
        var retryId = Guid.NewGuid();
        var retry = new Mock<ICvJdMatchingRetryUseCase>();
        retry.Setup(x => x.RetryAsync(userId, failedJobId, "retry-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MatchingSubmissionResult(retryId, false));
        var controller = CreateController(userId, Mock.Of<ICvJobMatchingUseCase>(), Mock.Of<ICvJdMatchingSubmissionUseCase>(), retry.Object);
        controller.HttpContext.Request.Headers["Idempotency-Key"] = "retry-123";

        var result = await controller.RetryMatch(failedJobId, CancellationToken.None);

        Assert.IsType<AcceptedResult>(result.Result);
        retry.Verify(x => x.RetryAsync(userId, failedJobId, "retry-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMatchResult_CompletedResult_SerializesTypedReportAndLegacyFields()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var matching = new Mock<ICvJobMatchingUseCase>();
        matching.Setup(x => x.GetMatchingResultAsync(jobId, userId)).ReturnsAsync(new MatchingResultDto
        {
            Id = jobId,
            Status = "Completed",
            MatchDetails = "{legacy-compatible-details}",
            ScorePercent = 81.8m,
            ReportKind = MatchReportKinds.Structured,
            MatchMethod = MatchMethodCodes.OneToOneAi,
            Report = new MatchReportDto
            {
                ReportKind = MatchReportKinds.Structured,
                SchemaVersion = "jd-matching/v4",
                MatchMethod = MatchMethodCodes.OneToOneAi,
                ScorePercent = 81.8m,
                RequirementGroups = new()
            }
        });
        var controller = CreateController(userId, matching.Object, Mock.Of<ICvJdMatchingSubmissionUseCase>());

        var action = await controller.GetMatchResult(jobId);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<ResponseBase<MatchingResultDto>>(ok.Value);
        envelope.Data!.Report.Should().NotBeNull();
        envelope.Data.MatchDetails.Should().Be("{legacy-compatible-details}");
        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.Should().Contain("\"scorePercent\":81.8");
        json.Should().Contain("\"reportKind\":\"structured\"");
        json.Should().Contain("\"matchMethod\":\"one_to_one_ai\"");
        json.Should().Contain("\"matchDetails\":\"{legacy-compatible-details}\"");
    }

    [Fact]
    public async Task DeleteMatchHistory_ActiveJobReturnsConflictAndRequiresCandidateOnly()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var history = new Mock<ICvJdMatchingHistoryUseCase>();
        history
            .Setup(x => x.HideAsync(jobId, userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HideMatchHistoryResult.ActiveJob);
        var controller = CreateController(
            userId,
            Mock.Of<ICvJobMatchingUseCase>(),
            Mock.Of<ICvJdMatchingSubmissionUseCase>(),
            history: history.Object);

        var result = await controller.DeleteMatchHistory(jobId);

        Assert.IsType<ConflictObjectResult>(result.Result);
        var method = typeof(CvController).GetMethod(nameof(CvController.DeleteMatchHistory), BindingFlags.Instance | BindingFlags.Public)!;
        method.GetCustomAttribute<AuthorizeAttribute>()!.Policy.Should().Be("CandidateOnly");
    }

    private static CvController CreateController(
        Guid userId,
        ICvJobMatchingUseCase matching,
        ICvJdMatchingSubmissionUseCase submission,
        ICvJdMatchingRetryUseCase? retry = null,
        ICvJdMatchingHistoryUseCase? history = null)
    {
        return new CvController(
            Mock.Of<ICvUseCase>(),
            matching,
            Mock.Of<IHardcodeCvJobMatchingUseCase>(),
            submission,
            retry ?? Mock.Of<ICvJdMatchingRetryUseCase>(),
            history ?? Mock.Of<ICvJdMatchingHistoryUseCase>(),
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<ICvTextExtractorService>(),
            Mock.Of<ICandidateFeatureUsageUseCase>(),
            Mock.Of<IMatchingInputPreflightUseCase>(),
            Mock.Of<ITHunterview.WebAPI.BackgroundServices.ICvMatchingQueue>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim("userId", userId.ToString())
                    }, "test"))
                }
            }
        };
    }
}
