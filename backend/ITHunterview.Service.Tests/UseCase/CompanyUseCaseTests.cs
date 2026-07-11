using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Company;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CompanyUseCaseTests
    {
        private readonly Mock<ICompanyRepository> _mockCompanyRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly CompanyUseCase _sut;

        public CompanyUseCaseTests()
        {
            _mockCompanyRepo = new Mock<ICompanyRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _sut = new CompanyUseCase(_mockCompanyRepo.Object, _mockUserRepo.Object);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldCreateAndLinkCompany_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateCompanyDto
            {
                Name = "Google",
                TaxCode = "123456",
                HeadquartersAddress = "1600 Amphitheatre Pkwy",
                ProvinceCode = "CA",
                DetailedLocation = "Mountain View",
                Latitude = 37.422,
                Longitude = -122.084,
                Industry = "Technology",
                CompanySize = "10000+",
                Description = "Search engine",
                CompanyType = "Public",
                Website = "https://google.com",
                LogoUrl = "https://google.com/logo.png",
                TradeName = "Google LLC",
                TargetCustomers = new List<string> { "Everyone" },
                CompanyEmail = "contact@google.com",
                ContactPhone = "123-456-7890",
                CompanyImages = new List<string> { "image1.png" },
                MainField = "Search",
                OperatingMarkets = new List<string> { "Global" },
                EmployeeBenefits = "Free food"
            };

            var company = new Companies
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                TaxCode = dto.TaxCode,
                HeadquartersAddress = dto.HeadquartersAddress,
                ProvinceCode = dto.ProvinceCode,
                DetailedLocation = dto.DetailedLocation,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Industry = dto.Industry,
                CompanySize = dto.CompanySize,
                Description = dto.Description,
                CompanyType = dto.CompanyType,
                Website = dto.Website,
                LogoUrl = dto.LogoUrl,
                VerificationDocumentUrl = "",
                Status = CompanyStatus.DRAFT,
                TradeName = dto.TradeName,
                TargetCustomers = dto.TargetCustomers,
                CompanyEmail = dto.CompanyEmail,
                ContactPhone = dto.ContactPhone,
                CompanyImages = dto.CompanyImages,
                MainField = dto.MainField,
                OperatingMarkets = dto.OperatingMarkets,
                EmployeeBenefits = dto.EmployeeBenefits,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockCompanyRepo.Setup(x => x.CreateAsync(It.IsAny<Companies>()))
                .ReturnsAsync(company);

            _mockCompanyRepo.Setup(x => x.LinkCompanyToRecruiterAsync(company.Id, userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateCompanyAsync(dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(company.Id);
            result.Name.Should().Be(dto.Name);
            result.Status.Should().Be(CompanyStatus.DRAFT);
            _mockCompanyRepo.Verify(x => x.CreateAsync(It.Is<Companies>(c => c.Name == dto.Name && c.CreatedBy == userId)), Times.Once);
            _mockCompanyRepo.Verify(x => x.LinkCompanyToRecruiterAsync(company.Id, userId), Times.Once);
        }

        [Fact]
        public async Task VerifyCompanyAsync_ShouldThrowKeyNotFoundException_WhenCompanyDoesNotExist()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new VerifyCompanyDto();

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync((Companies?)null);

            // Act
            Func<Task> act = async () => await _sut.VerifyCompanyAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Company not found");
        }

        [Fact]
        public async Task VerifyCompanyAsync_ShouldUpdateCompanyAndSetPendingStatus_WhenCompanyExists()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.DRAFT
            };
            var dto = new VerifyCompanyDto
            {
                VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION,
                VerificationDocumentUrl = "https://doc.com",
                TaxCode = "999",
                CompanyName = "Verified Corp",
                HeadquartersAddress = "123 Main St",
                ProvinceCode = "NY",
                DetailedLocation = "NYC",
                Latitude = 40.7128,
                Longitude = -74.0060,
                CompanyType = "Corp"
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.VerifyCompanyAsync(companyId, dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(CompanyStatus.PENDING);
            result.Name.Should().Be(dto.CompanyName);
            result.VerificationMethod.Should().Be(dto.VerificationMethod);
            result.VerificationDocumentUrl.Should().Be(dto.VerificationDocumentUrl);
            company.UpdatedBy.Should().Be(userId);
            _mockCompanyRepo.Verify(x => x.UpdateAsync(It.Is<Companies>(c => c.Id == companyId && c.Status == CompanyStatus.PENDING)), Times.Once);
        }

        [Fact]
        public async Task GetMyCompanyAsync_ShouldReturnNull_WhenCompanyDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync((Companies?)null);

            // Act
            var result = await _sut.GetMyCompanyAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetMyCompanyAsync_ShouldReturnCompanyDto_WhenCompanyExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = Guid.NewGuid(),
                Name = "My Company"
            };
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(company);

            // Act
            var result = await _sut.GetMyCompanyAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("My Company");
            result.Id.Should().Be(company.Id);
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_ShouldThrowKeyNotFoundException_WhenCompanyDoesNotExist()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.VERIFIED };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync((Companies?)null);

            // Act
            Func<Task> act = async () => await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Company not found");
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_WithPendingChanges_AndVerifiedStatus_ShouldApprovePendingChanges()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Name = "Old Name",
                HasPendingChange = true,
                PendingName = "New Name",
                PendingTaxCode = "New TaxCode",
                PendingHeadquartersAddress = "New HQ",
                PendingProvinceCode = "New PC",
                PendingDetailedLocation = "New DL",
                PendingLatitude = 1.0,
                PendingLongitude = 2.0,
                PendingVerificationMethod = CompanyVerificationMethod.POA_AND_ID,
                PendingVerificationDocumentUrl = "New Doc Url",
                PendingCompanyType = "New Type"
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.VERIFIED };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.Name.Should().Be("New Name");
            result.TaxCode.Should().Be("New TaxCode");
            result.HeadquartersAddress.Should().Be("New HQ");
            result.ProvinceCode.Should().Be("New PC");
            result.DetailedLocation.Should().Be("New DL");
            result.Latitude.Should().Be(1.0);
            result.Longitude.Should().Be(2.0);
            result.VerificationMethod.Should().Be(CompanyVerificationMethod.POA_AND_ID);
            result.VerificationDocumentUrl.Should().Be("New Doc Url");
            result.CompanyType.Should().Be("New Type");
            result.HasPendingChange.Should().BeFalse();

            company.PendingName.Should().BeNull();
            company.HasPendingChange.Should().BeFalse();
            company.UpdatedBy.Should().Be(userId);
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_WithPendingChanges_AndRejectedStatus_ShouldThrowArgumentException_WhenRejectReasonIsEmpty()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                HasPendingChange = true
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.REJECTED, RejectReason = "" };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);

            // Act
            Func<Task> act = async () => await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Rejection reason is required when rejecting changes.");
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_WithPendingChanges_AndRejectedStatus_ShouldClearPendingAndSetRejectReason_WhenRejectReasonIsValid()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Name = "Original Name",
                HasPendingChange = true,
                PendingName = "New Name"
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.REJECTED, RejectReason = "Spam info" };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.Name.Should().Be("Original Name"); // Remains original
            result.HasPendingChange.Should().BeFalse();
            result.RejectReason.Should().Be("Spam info");
            company.PendingName.Should().BeNull();
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_NoPendingChanges_AndRejectedStatus_ShouldThrowArgumentException_WhenRejectReasonIsEmpty()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                HasPendingChange = false
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.REJECTED, RejectReason = "" };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);

            // Act
            Func<Task> act = async () => await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Rejection reason is required when rejecting.");
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_NoPendingChanges_AndRejectedStatus_ShouldSetStatusAndRejectReason_WhenRejectReasonIsValid()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                HasPendingChange = false,
                Status = CompanyStatus.PENDING
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.REJECTED, RejectReason = "Invalid document" };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.Status.Should().Be(CompanyStatus.REJECTED);
            result.RejectReason.Should().Be("Invalid document");
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_NoPendingChanges_AndVerifiedStatus_ShouldSetStatusAndClearRejectReason()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                HasPendingChange = false,
                Status = CompanyStatus.PENDING,
                RejectReason = "Previous reason"
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.VERIFIED };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.Status.Should().Be(CompanyStatus.VERIFIED);
            result.RejectReason.Should().BeNull();
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_ShouldPopulateCreatorInfo_CandidateProfile()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                CreatedBy = creatorId,
                Status = CompanyStatus.VERIFIED
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.VERIFIED };
            var creatorUser = new User
            {
                Id = creatorId,
                Email = "candidate@gmail.com",
                CandidateProfile = new CandidateProfiles
                {
                    FirstName = "John",
                    LastName = "Doe"
                }
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockUserRepo.Setup(x => x.GetUserWithRoleAsync(creatorId))
                .ReturnsAsync(creatorUser);

            // Act
            var result = await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.CreatedByEmail.Should().Be("candidate@gmail.com");
            result.CreatedByName.Should().Be("John Doe");
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_ShouldPopulateCreatorInfo_RecruiterProfile()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                CreatedBy = creatorId,
                Status = CompanyStatus.VERIFIED
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.VERIFIED };
            var creatorUser = new User
            {
                Id = creatorId,
                Email = "recruiter@gmail.com",
                RecruiterProfile = new RecruiterProfiles
                {
                    FullName = "Jane Recruiter"
                }
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockUserRepo.Setup(x => x.GetUserWithRoleAsync(creatorId))
                .ReturnsAsync(creatorUser);

            // Act
            var result = await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.CreatedByEmail.Should().Be("recruiter@gmail.com");
            result.CreatedByName.Should().Be("Jane Recruiter");
        }

        [Fact]
        public async Task UpdateCompanyStatusAsync_ShouldPopulateCreatorInfo_DefaultSystem_WhenCreatedByIsNull()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                CreatedBy = null,
                Status = CompanyStatus.VERIFIED
            };
            var dto = new UpdateCompanyStatusDto { Status = CompanyStatus.VERIFIED };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);

            // Act
            var result = await _sut.UpdateCompanyStatusAsync(companyId, dto, userId);

            // Assert
            result.CreatedByEmail.Should().Be("system@ithunterview.com");
            result.CreatedByName.Should().Be("System");
        }

        [Fact]
        public async Task SubmitUpdateRequestAsync_ShouldThrowKeyNotFoundException_WhenCompanyDoesNotExist()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new VerifyCompanyDto();

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync((Companies?)null);

            // Act
            Func<Task> act = async () => await _sut.SubmitUpdateRequestAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Company not found");
        }

        [Fact]
        public async Task SubmitUpdateRequestAsync_ShouldThrowInvalidOperationException_WhenCompanyIsNotVerified()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.DRAFT
            };
            var dto = new VerifyCompanyDto();

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);

            // Act
            Func<Task> act = async () => await _sut.SubmitUpdateRequestAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Only verified companies can request updates to legal information.");
        }

        [Fact]
        public async Task SubmitUpdateRequestAsync_ShouldSetPendingFieldsAndHasPendingChange_WhenValid()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.VERIFIED,
                HasPendingChange = false
            };
            var dto = new VerifyCompanyDto
            {
                CompanyName = "Pending Corp",
                TaxCode = "555",
                HeadquartersAddress = "Pending address",
                ProvinceCode = "LA",
                DetailedLocation = "Details",
                Latitude = 12.34,
                Longitude = 56.78,
                VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION,
                VerificationDocumentUrl = "https://doc.url",
                CompanyType = "Limited"
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SubmitUpdateRequestAsync(companyId, dto, userId);

            // Assert
            result.HasPendingChange.Should().BeTrue();
            result.PendingName.Should().Be("Pending Corp");
            result.PendingTaxCode.Should().Be("555");
            result.PendingHeadquartersAddress.Should().Be("Pending address");
            result.PendingProvinceCode.Should().Be("LA");
            result.PendingDetailedLocation.Should().Be("Details");
            result.PendingLatitude.Should().Be(12.34);
            result.PendingLongitude.Should().Be(56.78);
            result.PendingVerificationMethod.Should().Be(CompanyVerificationMethod.BUSINESS_REGISTRATION);
            result.PendingVerificationDocumentUrl.Should().Be("https://doc.url");
            result.PendingCompanyType.Should().Be("Limited");

            company.UpdatedBy.Should().Be(userId);
            _mockCompanyRepo.Verify(x => x.UpdateAsync(It.Is<Companies>(c => c.Id == companyId && c.HasPendingChange == true)), Times.Once);
        }

        [Fact]
        public async Task GetPagedCompaniesAsync_ShouldReturnPagedResultsWithCreatorInfo()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;
            var companyId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Name = "Page Company",
                CreatedBy = creatorId
            };
            var items = new List<Companies> { company };
            var total = 1;

            _mockCompanyRepo.Setup(x => x.GetPagedCompaniesAsync(page, pageSize, "search", "status"))
                .ReturnsAsync((items, total));

            var user = new User
            {
                Id = creatorId,
                Email = "creator@mail.com",
                RecruiterProfile = new RecruiterProfiles { FullName = "Recruiter Name" }
            };
            _mockUserRepo.Setup(x => x.GetUserWithRoleAsync(creatorId))
                .ReturnsAsync(user);

            // Act
            var result = await _sut.GetPagedCompaniesAsync(page, pageSize, "search", "status");

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Total.Should().Be(1);
            result.Data.Items[0].Name.Should().Be("Page Company");
            result.Data.Items[0].CreatedByEmail.Should().Be("creator@mail.com");
            result.Data.Items[0].CreatedByName.Should().Be("Recruiter Name");
        }

        [Fact]
        public async Task UpdateCompanyAsync_ShouldThrowKeyNotFoundException_WhenCompanyDoesNotExist()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new UpdateCompanyDto();

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync((Companies?)null);

            // Act
            Func<Task> act = async () => await _sut.UpdateCompanyAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Company not found");
        }

        [Fact]
        public async Task UpdateCompanyAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotLinkedToCompany()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new UpdateCompanyDto();
            var company = new Companies { Id = companyId };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            // GetByUserIdAsync returns a different company
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new Companies { Id = Guid.NewGuid() });

            // Act
            Func<Task> act = async () => await _sut.UpdateCompanyAsync(companyId, dto, userId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not authorized to update this company.");
        }

        [Fact]
        public async Task UpdateCompanyAsync_ShouldUpdateAddressFields_WhenCompanyIsNotVerified()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.DRAFT,
                HeadquartersAddress = "Old Addr"
            };
            var dto = new UpdateCompanyDto
            {
                HeadquartersAddress = "New Addr",
                ProvinceCode = "New PC",
                DetailedLocation = "New Detail",
                Latitude = 1.1,
                Longitude = 2.2
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyAsync(companyId, dto, userId);

            // Assert
            result.HeadquartersAddress.Should().Be("New Addr");
            result.ProvinceCode.Should().Be("New PC");
            result.DetailedLocation.Should().Be("New Detail");
            result.Latitude.Should().Be(1.1);
            result.Longitude.Should().Be(2.2);
        }

        [Fact]
        public async Task UpdateCompanyAsync_ShouldNotUpdateAddressFields_WhenCompanyIsVerified()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.VERIFIED,
                HeadquartersAddress = "Old Addr",
                ProvinceCode = "Old PC",
                DetailedLocation = "Old Detail",
                Latitude = 1.0,
                Longitude = 2.0
            };
            var dto = new UpdateCompanyDto
            {
                HeadquartersAddress = "New Addr",
                ProvinceCode = "New PC",
                DetailedLocation = "New Detail",
                Latitude = 1.1,
                Longitude = 2.2
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyAsync(companyId, dto, userId);

            // Assert
            result.HeadquartersAddress.Should().Be("Old Addr"); // remains old
            result.ProvinceCode.Should().Be("Old PC");
            result.DetailedLocation.Should().Be("Old Detail");
            result.Latitude.Should().Be(1.0);
            result.Longitude.Should().Be(2.0);
        }

        [Fact]
        public async Task UpdateCompanyAsync_ShouldUpdateCommonFields()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.VERIFIED
            };
            var dto = new UpdateCompanyDto
            {
                Website = "newsite.com",
                LogoUrl = "newlogo.png",
                CompanySize = "Medium",
                Description = "New Desc",
                CompanyType = "Private",
                Industry = "Fintech",
                TradeName = "New Trade",
                TargetCustomers = new List<string> { "B2B" },
                CompanyEmail = "new@corp.com",
                ContactPhone = "999-999-9999",
                CompanyImages = new List<string> { "newimg.png" },
                MainField = "Finance",
                OperatingMarkets = new List<string> { "Asia" },
                EmployeeBenefits = "Gym membership"
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyAsync(companyId, dto, userId);

            // Assert
            result.Website.Should().Be("newsite.com");
            result.LogoUrl.Should().Be("newlogo.png");
            result.CompanySize.Should().Be("Medium");
            result.Description.Should().Be("New Desc");
            result.CompanyType.Should().Be("Private");
            result.Industry.Should().Be("Fintech");
            result.TradeName.Should().Be("New Trade");
            result.TargetCustomers.Should().BeEquivalentTo(new List<string> { "B2B" });
            result.CompanyEmail.Should().Be("new@corp.com");
            result.ContactPhone.Should().Be("999-999-9999");
            result.CompanyImages.Should().BeEquivalentTo(new List<string> { "newimg.png" });
            result.MainField.Should().Be("Finance");
            result.OperatingMarkets.Should().BeEquivalentTo(new List<string> { "Asia" });
            result.EmployeeBenefits.Should().Be("Gym membership");
        }

        [Fact]
        public async Task UpdateCompanyAsync_WhenDtoFieldsAreNull_ShouldNotModifyExistingFields()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var originalCompany = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.VERIFIED,
                Name = "Original Name",
                TaxCode = "111",
                HeadquartersAddress = "Original HQ",
                ProvinceCode = "Original PC",
                DetailedLocation = "Original DL",
                Latitude = 10.0,
                Longitude = 20.0,
                Website = "original.com",
                LogoUrl = "original.png",
                CompanySize = "Large",
                Description = "Original Desc",
                CompanyType = "Public",
                Industry = "Tech",
                TradeName = "Original Trade",
                TargetCustomers = new List<string> { "B2C" },
                CompanyEmail = "original@corp.com",
                ContactPhone = "111-111-1111",
                CompanyImages = new List<string> { "originalimg.png" },
                MainField = "Original Field",
                OperatingMarkets = new List<string> { "EU" },
                EmployeeBenefits = "Health insurance",
                UpdatedBy = Guid.Empty,
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            var dto = new UpdateCompanyDto(); // all properties null

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(originalCompany);
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(originalCompany);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyAsync(companyId, dto, userId);

            // Assert
            result.Website.Should().Be("original.com");
            result.LogoUrl.Should().Be("original.png");
            result.CompanySize.Should().Be("Large");
            result.Description.Should().Be("Original Desc");
            result.CompanyType.Should().Be("Public");
            result.Industry.Should().Be("Tech");
            result.TradeName.Should().Be("Original Trade");
            result.TargetCustomers.Should().BeEquivalentTo(new List<string> { "B2C" });
            result.CompanyEmail.Should().Be("original@corp.com");
            result.ContactPhone.Should().Be("111-111-1111");
            result.CompanyImages.Should().BeEquivalentTo(new List<string> { "originalimg.png" });
            result.MainField.Should().Be("Original Field");
            result.OperatingMarkets.Should().BeEquivalentTo(new List<string> { "EU" });
            result.EmployeeBenefits.Should().Be("Health insurance");
            
            originalCompany.UpdatedBy.Should().Be(userId);
            originalCompany.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateCompanyAsync_WhenCompanyIsNotVerifiedAndDtoAddressFieldsAreNull_ShouldNotModifyAddressFields()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var company = new Companies
            {
                Id = companyId,
                Status = CompanyStatus.DRAFT,
                HeadquartersAddress = "Original HQ",
                ProvinceCode = "Original PC",
                DetailedLocation = "Original DL",
                Latitude = 10.0,
                Longitude = 20.0
            };
            var dto = new UpdateCompanyDto
            {
                HeadquartersAddress = null,
                ProvinceCode = null,
                DetailedLocation = null,
                Latitude = null,
                Longitude = null,
                Website = "newsite.com"
            };

            _mockCompanyRepo.Setup(x => x.GetByIdAsync(companyId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(company);
            _mockCompanyRepo.Setup(x => x.UpdateAsync(It.IsAny<Companies>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateCompanyAsync(companyId, dto, userId);

            // Assert
            result.HeadquartersAddress.Should().Be("Original HQ");
            result.ProvinceCode.Should().Be("Original PC");
            result.DetailedLocation.Should().Be("Original DL");
            result.Latitude.Should().Be(10.0);
            result.Longitude.Should().Be(20.0);
            result.Website.Should().Be("newsite.com");
        }
    }
}
