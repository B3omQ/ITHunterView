using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
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
    }

    private static CvController CreateController(
        Guid userId,
        ICvJobMatchingUseCase matching,
        ICvJdMatchingSubmissionUseCase submission)
    {
        return new CvController(
            Mock.Of<ICvUseCase>(),
            matching,
            Mock.Of<IHardcodeCvJobMatchingUseCase>(),
            submission,
            Mock.Of<ICvTextExtractorService>())
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
