using FluentAssertions;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingRetryPolicyTests
{
    [Theory]
    [InlineData("AI_PROVIDER_TIMEOUT")]
    [InlineData("AI_PROVIDER_REQUEST_FAILED")]
    [InlineData("AI_PROVIDER_HTTP_ERROR")]
    [InlineData("AI_PROVIDER_INVALID_JSON")]
    [InlineData("LEASE_EXPIRED")]
    public void IsManualRetryAllowed_TransientFailure_ReturnsTrue(string errorCode)
    {
        MatchingRetryPolicy.IsManualRetryAllowed(errorCode).Should().BeTrue();
    }

    [Theory]
    [InlineData("AI_OUTPUT_INVALID")]
    [InlineData("MATCHING_INPUT_INVALID")]
    [InlineData("MATCHING_CONFIGURATION_INVALID")]
    [InlineData(null)]
    public void IsManualRetryAllowed_DeterministicFailure_ReturnsFalse(string? errorCode)
    {
        MatchingRetryPolicy.IsManualRetryAllowed(errorCode).Should().BeFalse();
    }
}
