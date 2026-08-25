using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.FeatureUsage;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.WebAPI.Tests.Infrastructure;
using Moq;

namespace ITHunterview.WebAPI.Tests.Controllers;

public sealed class JobPostingsControllerPushTopAuthorizationTests : IClassFixture<PushTopWebApplicationFactory>
{
    private readonly PushTopWebApplicationFactory _factory;
    private static readonly Guid FixedJobId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FixedRecruiterId = PushTopTestAuthenticationHandler.ValidRecruiterUserId;

    public JobPostingsControllerPushTopAuthorizationTests(PushTopWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.JobPostingsUseCaseMock.Reset();
        _factory.UserUseCaseMock.Reset();
    }

    private static HttpContent CreateValidPushTopContent()
    {
        var json = JsonSerializer.Serialize(new
        {
            expectedPaymentMethod = "SUBSCRIPTION_QUOTA"
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    public async Task AUTH01_PushTop_WhenAnonymous_Returns401BeforeResolvingRecruiterOrJob(string httpMethod)
    {
        // Mutation caught: removing/missing RecruiterOnly policy on either verb
        var client = _factory.CreateClientForIdentity("anonymous");
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), $"/api/jobpostings/{FixedJobId}/push-top")
        {
            Content = CreateValidPushTopContent()
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.UserUseCaseMock.Verify(u => u.ResolveRecruiterIdAsync(It.IsAny<string?>()), Times.Never);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    public async Task AUTH02_PushTop_WhenCandidate_Returns403BeforeResolvingRecruiterOrJob(string httpMethod)
    {
        // Mutation caught: weakening policy to authenticated-only or Candidate-or-Recruiter
        var client = _factory.CreateClientForIdentity("candidate");
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), $"/api/jobpostings/{FixedJobId}/push-top")
        {
            Content = CreateValidPushTopContent()
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _factory.UserUseCaseMock.Verify(u => u.ResolveRecruiterIdAsync(It.IsAny<string?>()), Times.Never);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
    }

    [Theory]
    [InlineData("recruiter-no-id")]
    [InlineData("recruiter-invalid-id")]
    public async Task AUTH03_PushTop_WhenMissingOrInvalidRecruiterIdClaim_Returns401WithoutSeedFallback(string identity)
    {
        // Mutation caught: calling ResolveRecruiterIdAsync(null) and falling back to seed recruiter
        var client = _factory.CreateClientForIdentity(identity);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/jobpostings/{FixedJobId}/push-top")
        {
            Content = CreateValidPushTopContent()
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.UserUseCaseMock.Verify(u => u.ResolveRecruiterIdAsync(It.IsAny<string?>()), Times.Never);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
    }

    [Fact]
    public async Task AUTH04_PushTop_WhenValidRecruiter_ProceedsWithExactAuthenticatedUserId()
    {
        // Mutation caught: ignoring caller identity or converting through recruiter profile
        var client = _factory.CreateClientForIdentity("recruiter");
        _factory.JobPostingsUseCaseMock
            .Setup(j => j.PushTopJobAsync(
                FixedJobId,
                FixedRecruiterId,
                It.Is<FeatureConsumptionExpectation>(expectation =>
                    expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA &&
                    expectation.ExpectedCoinCost == null)))
            .ReturnsAsync(new ResponseBase<JobPostingDetailDto>(new JobPostingDetailDto { Id = FixedJobId, RecruiterId = FixedRecruiterId }));

        var response = await client.PostAsync($"/api/jobpostings/{FixedJobId}/push-top", CreateValidPushTopContent());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.UserUseCaseMock.Verify(u => u.ResolveRecruiterIdAsync(It.IsAny<string?>()), Times.Never);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(
            FixedJobId,
            FixedRecruiterId,
            It.Is<FeatureConsumptionExpectation>(expectation =>
                expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.SUBSCRIPTION_QUOTA &&
                expectation.ExpectedCoinCost == null)), Times.Once);
    }

    [Fact]
    public async Task PushTop_WhenUseCaseThrowsKeyNotFoundException_Returns404WithFailurePayload()
    {
        // External 404 status mapping for missing/foreign jobs
        var client = _factory.CreateClientForIdentity("recruiter");
        _factory.JobPostingsUseCaseMock
            .Setup(j => j.PushTopJobAsync(FixedJobId, FixedRecruiterId, It.IsAny<FeatureConsumptionExpectation>()))
            .ThrowsAsync(new KeyNotFoundException("Job not found."));

        var response = await client.PostAsync($"/api/jobpostings/{FixedJobId}/push-top", CreateValidPushTopContent());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ResponseBase<JobPostingDetailDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task PushTop_WhenUseCaseThrowsInvalidOperationException_Returns409WithFailurePayload()
    {
        // External 409 status mapping for banned/non-published jobs
        var client = _factory.CreateClientForIdentity("recruiter");
        _factory.JobPostingsUseCaseMock
            .Setup(j => j.PushTopJobAsync(FixedJobId, FixedRecruiterId, It.IsAny<FeatureConsumptionExpectation>()))
            .ThrowsAsync(new InvalidOperationException("Job is not published."));

        var response = await client.PostAsync($"/api/jobpostings/{FixedJobId}/push-top", CreateValidPushTopContent());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ResponseBase<JobPostingDetailDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task BILL_API_01_PushTop_MissingRequestBody_Returns400WithoutCallingUseCase()
    {
        // Mutation caught: bodyless action accepting empty payload
        var client = _factory.CreateClientForIdentity("recruiter");
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/jobpostings/{FixedJobId}/push-top")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
    }

    [Fact]
    public async Task BILL_API_02_PushTop_SubscriptionQuotaWithNonNullCost_Returns400WithoutCallingUseCase()
    {
        // Mutation caught: accepting invalid expectation payload
        var client = _factory.CreateClientForIdentity("recruiter");
        var invalidJson = JsonSerializer.Serialize(new
        {
            expectedPaymentMethod = "SUBSCRIPTION_QUOTA",
            expectedCoinCost = 5000
        });
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"/api/jobpostings/{FixedJobId}/push-top", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
    }

    [Fact]
    public async Task BILL_API_03_PushTop_CoinWithNullCost_Returns400WithoutCallingUseCase()
    {
        // Mutation caught: accepting coin payment without explicit expected price
        var client = _factory.CreateClientForIdentity("recruiter");
        var invalidJson = JsonSerializer.Serialize(new
        {
            expectedPaymentMethod = "COIN",
            expectedCoinCost = (int?)null
        });
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"/api/jobpostings/{FixedJobId}/push-top", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
    }

    [Fact]
    public async Task BILL_API_04_PushTop_CoinWithNegativeCost_Returns400WithoutCallingUseCase()
    {
        // Mutation caught: accepting negative coin cost
        var client = _factory.CreateClientForIdentity("recruiter");
        var invalidJson = JsonSerializer.Serialize(new
        {
            expectedPaymentMethod = "COIN",
            expectedCoinCost = -100
        });
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"/api/jobpostings/{FixedJobId}/push-top", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FeatureConsumptionExpectation>()), Times.Never);
    }

    [Fact]
    public async Task BILL_API_05_PushTop_CoinWithZeroCost_ForwardsExactSnapshot()
    {
        var client = _factory.CreateClientForIdentity("recruiter");
        _factory.JobPostingsUseCaseMock
            .Setup(j => j.PushTopJobAsync(
                FixedJobId,
                FixedRecruiterId,
                It.Is<FeatureConsumptionExpectation>(expectation =>
                    expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.COIN &&
                    expectation.ExpectedCoinCost == 0)))
            .ReturnsAsync(new ResponseBase<JobPostingDetailDto>(
                new JobPostingDetailDto { Id = FixedJobId, RecruiterId = FixedRecruiterId }));
        var content = JsonContent.Create(new
        {
            expectedPaymentMethod = "COIN",
            expectedCoinCost = 0
        });

        var response = await client.PostAsync($"/api/jobpostings/{FixedJobId}/push-top", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.JobPostingsUseCaseMock.Verify(j => j.PushTopJobAsync(
            FixedJobId,
            FixedRecruiterId,
            It.Is<FeatureConsumptionExpectation>(expectation =>
                expectation.ExpectedPaymentMethod == FeatureConsumptionPaymentMethod.COIN &&
                expectation.ExpectedCoinCost == 0)), Times.Once);
    }
}
