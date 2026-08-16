using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Company;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.UseCase;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CompanyUseCaseTests
    {
        private readonly Mock<ICompanyRepository> _companyRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IWalletUseCase> _walletMock;
        private readonly Mock<INotificationUseCase> _notificationUseCaseMock;
        private readonly CompanyUseCase _useCase;

        public CompanyUseCaseTests()
        {
            _companyRepoMock = new Mock<ICompanyRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _walletMock = new Mock<IWalletUseCase>();
            _notificationUseCaseMock = new Mock<INotificationUseCase>();

            _useCase = new CompanyUseCase(
                _companyRepoMock.Object,
                _userRepoMock.Object,
                _walletMock.Object,
                _notificationUseCaseMock.Object
            );
        }

        [Fact]
        public async Task VerifyCompanyAsync_UserSubmitsRequest_UpdatesToPending()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies { Id = companyId, Status = CompanyStatus.DRAFT };

            var dto = new VerifyCompanyDto
            {
                CompanyName = "Test Company",
                TaxCode = "123456",
                VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION
            };

            _companyRepoMock.Setup(repo => repo.GetByIdAsync(companyId)).ReturnsAsync(company);
            _companyRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Companies>())).Returns(Task.CompletedTask);

            // Act
            var result = await _useCase.VerifyCompanyAsync(companyId, dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(CompanyStatus.PENDING);
            result.Name.Should().Be("Test Company");
            _companyRepoMock.Verify(repo => repo.UpdateAsync(It.IsAny<Companies>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_AdminApproves_StatusVerified()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies { Id = companyId, Status = CompanyStatus.PENDING };

            var dto = new UpdateCompanyStatusDto
            {
                Status = CompanyStatus.VERIFIED
            };

            _companyRepoMock.Setup(repo => repo.GetByIdAsync(companyId)).ReturnsAsync(company);
            _companyRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Companies>())).Returns(Task.CompletedTask);

            // Act
            var result = await _useCase.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(CompanyStatus.VERIFIED);
            _companyRepoMock.Verify(repo => repo.UpdateAsync(It.IsAny<Companies>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_AdminRejects_WithoutReason_ThrowsException()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies { Id = companyId, Status = CompanyStatus.VERIFIED };

            var dto = new UpdateCompanyStatusDto
            {
                Status = CompanyStatus.REJECTED,
                RejectReason = "" // Missing reason
            };

            _companyRepoMock.Setup(repo => repo.GetByIdAsync(companyId)).ReturnsAsync(company);

            // Act & Assert
            var action = async () => await _useCase.UpdateCompanyStatusAsync(companyId, dto, userId);
            await action.Should().ThrowAsync<ArgumentException>().WithMessage("*yêu cầu lý do*");
        }

        [Fact]
        public async Task ClaimNewbieRewardAsync_WhenVerified_AwardsCoins()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var company = new Companies 
            { 
                Id = Guid.NewGuid(), 
                Status = CompanyStatus.VERIFIED,
                IsNewbieRewardClaimed = false,
                CreatedBy = userId 
            };

            _companyRepoMock.Setup(repo => repo.GetByUserIdAsync(userId)).ReturnsAsync(company);
            _companyRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Companies>())).Returns(Task.CompletedTask);
            _walletMock.Setup(w => w.AddBonusCoinsAsync(userId, 25000, It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _useCase.ClaimNewbieRewardAsync(userId);

            // Assert
            result.Should().NotBeNull();
            company.IsNewbieRewardClaimed.Should().BeTrue();
            _walletMock.Verify(w => w.AddBonusCoinsAsync(userId, 25000, It.IsAny<string>()), Times.Once);
        }
    }
}
