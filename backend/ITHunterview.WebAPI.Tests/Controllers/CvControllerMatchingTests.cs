using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.FeatureUsage;
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
    public async Task MatchJd_PreflightsBeforeBillingAndSubmission()
    {
        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var prepared = new PreparedMatchingRequest(
            new PreparedRawCvSource(new string('c', 100), "cv.pdf"),
            new PreparedRawJdSource(new string('j', 100), "JD"),
            MatchingMode.JdFit);
        var sequence = new MockSequence();
        var preflight = new Mock<IMatchingInputPreflightUseCase>();
        var usage = new Mock<ICandidateFeatureUsageUseCase>();
        var matching = new Mock<ICvJobMatchingUseCase>();
        var consumption = new FeatureConsumptionResult { DeductTransactionId = Guid.NewGuid() };

        preflight.InSequence(sequence)
            .Setup(x => x.PrepareAsync(userId, It.IsAny<MatchingRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prepared);
        usage.InSequence(sequence)
            .Setup(x => x.TryConsumeFeatureAsync(userId, "CvJdMatching", It.IsAny<string>()))
            .ReturnsAsync(consumption);
        matching.InSequence(sequence)
            .Setup(x => x.SubmitMatchingJobAsync(userId, prepared, It.IsAny<Guid>()))
            .ReturnsAsync(matchId);

        var controller = CreateController(userId, matching.Object, usage.Object, preflight.Object);

        var result = await controller.MatchJd(new MatchingRequestDto
        {
            CvText = new string('c', 100),
            RawJdText = new string('j', 100)
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        preflight.Verify(x => x.PrepareAsync(userId, It.IsAny<MatchingRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
        usage.Verify(x => x.TryConsumeFeatureAsync(userId, "CvJdMatching", It.IsAny<string>()), Times.Once);
        matching.Verify(x => x.SubmitMatchingJobAsync(userId, prepared, It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task MatchJd_WhenPreflightFails_DoesNotConsumeFeature()
    {
        var userId = Guid.NewGuid();
        var preflight = new Mock<IMatchingInputPreflightUseCase>();
        preflight.Setup(x => x.PrepareAsync(userId, It.IsAny<MatchingRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("MULTIPLE_CV_SOURCES"));
        var usage = new Mock<ICandidateFeatureUsageUseCase>();
        var controller = CreateController(userId, Mock.Of<ICvJobMatchingUseCase>(), usage.Object, preflight.Object);

        Func<Task> action = async () => await controller.MatchJd(new MatchingRequestDto(), CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(action);
        usage.Verify(x => x.TryConsumeFeatureAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
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
        ICandidateFeatureUsageUseCase usage,
        IMatchingInputPreflightUseCase preflight)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(x => x.GetService(typeof(ICvJobMatchingUseCase))).Returns(matching);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(provider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        return new CvController(
            Mock.Of<ICvUseCase>(),
            matching,
            Mock.Of<IHardcodeCvJobMatchingUseCase>(),
            scopeFactory.Object,
            Mock.Of<ICvTextExtractorService>(),
            usage,
            preflight)
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
