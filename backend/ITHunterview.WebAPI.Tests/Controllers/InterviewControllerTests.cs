using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.DTOs.Interview;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ITHunterview.WebAPI.Tests.Controllers
{
    public class InterviewControllerTests
    {
        [Fact]
        public async Task CreateSession_WhenSessionWasNotPersisted_RefundsConsumedCoins()
        {
            var userId = Guid.NewGuid();
            var consumption = new FeatureConsumptionResult
            {
                ChargedCoins = 100,
                DeductTransactionId = Guid.NewGuid()
            };
            var interviewUseCase = new Mock<IInterviewUseCase>();
            var featureUsageUseCase = new Mock<ICandidateFeatureUsageUseCase>();
            featureUsageUseCase
                .Setup(x => x.TryConsumeFeatureAsync(userId, "MockInterview", null))
                .ReturnsAsync(consumption);
            interviewUseCase
                .Setup(x => x.CreateSessionAsync(userId, It.IsAny<CreateInterviewSessionDto>()))
                .ThrowsAsync(new InterviewSessionCreationException("Creation failed.", false, new Exception("Database failed.")));

            var controller = CreateController(userId, interviewUseCase.Object, featureUsageUseCase.Object);

            var result = await controller.CreateSession(new CreateInterviewSessionDto());

            Assert.IsType<ObjectResult>(result.Result);
            featureUsageUseCase.Verify(x => x.RefundFeatureUsageAsync(
                userId,
                consumption,
                "Hoàn Coin vì không thể khởi tạo Mock Interview."), Times.Once);
        }

        [Fact]
        public async Task CreateSession_WhenSessionWasPersisted_DoesNotRefundConsumedCoins()
        {
            var userId = Guid.NewGuid();
            var consumption = new FeatureConsumptionResult
            {
                ChargedCoins = 100,
                DeductTransactionId = Guid.NewGuid()
            };
            var interviewUseCase = new Mock<IInterviewUseCase>();
            var featureUsageUseCase = new Mock<ICandidateFeatureUsageUseCase>();
            featureUsageUseCase
                .Setup(x => x.TryConsumeFeatureAsync(userId, "MockInterview", null))
                .ReturnsAsync(consumption);
            interviewUseCase
                .Setup(x => x.CreateSessionAsync(userId, It.IsAny<CreateInterviewSessionDto>()))
                .ThrowsAsync(new InterviewSessionCreationException("Question generation failed.", true, new Exception("AI failed.")));

            var controller = CreateController(userId, interviewUseCase.Object, featureUsageUseCase.Object);

            var result = await controller.CreateSession(new CreateInterviewSessionDto());

            Assert.IsType<ObjectResult>(result.Result);
            featureUsageUseCase.Verify(x => x.RefundFeatureUsageAsync(
                It.IsAny<Guid>(),
                It.IsAny<FeatureConsumptionResult>(),
                It.IsAny<string>()), Times.Never);
        }

        private static InterviewController CreateController(
            Guid userId,
            IInterviewUseCase interviewUseCase,
            ICandidateFeatureUsageUseCase featureUsageUseCase)
        {
            var speechToTextService = new Mock<ISpeechToTextService>();
            return new InterviewController(interviewUseCase, speechToTextService.Object, featureUsageUseCase)
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
}
