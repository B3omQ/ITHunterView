using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Wallet;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITHunterview.WebAPI.Tests.Controllers
{
    public class WalletControllerCustomCoinTopupTests
    {
        [Fact]
        public async Task CreateCustomCoinTopupPayment_DelegatesCandidateAmountToWalletUseCase()
        {
            var userId = Guid.NewGuid();
            var request = new CreateCustomCoinTopupDto
            {
                CoinAmount = 2,
                PaymentGateway = PaymentGateway.PAYOS
            };
            var walletUseCase = new Mock<IWalletUseCase>();
            walletUseCase
                .Setup(x => x.CreateCustomCoinTopupPaymentAsync(userId, request))
                .ReturnsAsync(new ResponseBase<CreatePaymentResponseDto>(new CreatePaymentResponseDto
                {
                    PaymentId = Guid.NewGuid(),
                    OrderCode = 1234567890,
                    CheckoutUrl = "https://checkout.example.test"
                }));

            var controller = CreateController(userId, walletUseCase.Object);

            var result = await controller.CreateCustomCoinTopupPayment(request);

            Assert.IsType<OkObjectResult>(result);
            walletUseCase.Verify(x => x.CreateCustomCoinTopupPaymentAsync(userId, request), Times.Once);
        }

        [Fact]
        public async Task CreateCustomCoinTopupPayment_ReturnsBadRequestWhenUseCaseRejectsRequest()
        {
            var userId = Guid.NewGuid();
            var request = new CreateCustomCoinTopupDto
            {
                CoinAmount = 0,
                PaymentGateway = PaymentGateway.PAYOS
            };
            var walletUseCase = new Mock<IWalletUseCase>();
            walletUseCase
                .Setup(x => x.CreateCustomCoinTopupPaymentAsync(userId, request))
                .ReturnsAsync(new ResponseBase<CreatePaymentResponseDto>("Coin amount is invalid."));

            var controller = CreateController(userId, walletUseCase.Object);

            var result = await controller.CreateCustomCoinTopupPayment(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        private static WalletController CreateController(Guid userId, IWalletUseCase walletUseCase)
        {
            return new WalletController(
                walletUseCase,
                Mock.Of<ICoinConfigUseCase>(),
                null!,
                Mock.Of<ILogger<WalletController>>())
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
