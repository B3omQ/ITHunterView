using System.Net;
using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingFailureClassifierTests
{
    [Theory]
    [InlineData("CV_ANALYSIS_EMPTY_OUTPUT")]
    [InlineData("CV_ANALYSIS_INVALID_JSON")]
    [InlineData("CV_ANALYSIS_SCHEMA_INVALID")]
    [InlineData("CV_ANALYSIS_EVIDENCE_NOT_GROUNDED")]
    public void Classify_CvAnalysisOutputFailure_IsNonRetryable(string failureCode)
    {
        var exception = ValidationException(failureCode);

        var result = MatchingFailureClassifier.Classify(exception);

        result.ErrorCode.Should().Be("AI_OUTPUT_INVALID");
        result.Retryable.Should().BeFalse();
        result.CvAnalysisQuality.Should().Be(CvAnalysisQuality.INVALID);
    }

    [Theory]
    [InlineData("CV_ANALYSIS_RAW_TEXT_REQUIRED")]
    [InlineData("CV_ANALYSIS_INPUT_INVALID")]
    public void Classify_CvAnalysisInputFailure_IsNonRetryable(string failureCode)
    {
        var exception = ValidationException(failureCode);

        var result = MatchingFailureClassifier.Classify(exception);

        result.ErrorCode.Should().Be("MATCHING_INPUT_INVALID");
        result.Retryable.Should().BeFalse();
        result.CvAnalysisQuality.Should().BeNull();
    }

    [Fact]
    public void Classify_PromptConfigurationFailure_IsNonRetryable()
    {
        var result = MatchingFailureClassifier.Classify(
            new InvalidOperationException("PROMPT_CONFIGURATION_INVALID: active pair mismatch"));

        result.ErrorCode.Should().Be("MATCHING_CONFIGURATION_INVALID");
        result.Retryable.Should().BeFalse();
    }

    [Fact]
    public void Classify_MissingActivePrompt_IsNonRetryableConfigurationFailure()
    {
        var result = MatchingFailureClassifier.Classify(
            new InvalidOperationException("PROMPT_NOT_CONFIGURED: active prompt not found"));

        result.ErrorCode.Should().Be("MATCHING_CONFIGURATION_INVALID");
        result.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData("MATCHING_PROMPT_SCHEMA_MUTATION")]
    [InlineData("MATCHING_PROMPT_PLACEHOLDER_INVALID:[CV_TEXT]")]
    public void Classify_MatchingPromptContractFailure_IsConfigurationFailure(string message)
    {
        var result = MatchingFailureClassifier.Classify(new InvalidOperationException(message));

        result.ErrorCode.Should().Be("MATCHING_CONFIGURATION_INVALID");
        result.Retryable.Should().BeFalse();
    }

    [Fact]
    public void Classify_UnusableJdAnalysis_IsAiOutputFailureNotConfigurationFailure()
    {
        var result = MatchingFailureClassifier.Classify(new InvalidOperationException("INVALID_JD_ANALYSIS"));

        result.ErrorCode.Should().Be("AI_OUTPUT_INVALID");
        result.Retryable.Should().BeFalse();
        result.JdAnalysisQuality.Should().Be(JdAnalysisQuality.INVALID);
    }

    [Theory]
    [MemberData(nameof(TransientProviderFailures))]
    public void Classify_TransientProviderFailure_RemainsRetryable(Exception exception)
    {
        var result = MatchingFailureClassifier.Classify(exception);

        result.Retryable.Should().BeTrue();
    }

    [Fact]
    public void Classify_UnknownFailure_RemainsRetryableTechnicalFailure()
    {
        var result = MatchingFailureClassifier.Classify(new InvalidOperationException("unexpected"));

        result.ErrorCode.Should().Be("MATCHING_TECHNICAL_ERROR");
        result.Retryable.Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public void Classify_ProviderAuthenticationFailure_IsNonRetryableConfigurationFailure(
        HttpStatusCode statusCode)
    {
        var exception = new HttpRequestException(
            "Provider authentication failed.",
            inner: null,
            statusCode);

        var result = MatchingFailureClassifier.Classify(exception);

        result.ErrorCode.Should().Be("MATCHING_CONFIGURATION_INVALID");
        result.Retryable.Should().BeFalse();
    }

    [Fact]
    public void Classify_MissingProviderApiKey_IsNonRetryableConfigurationFailure()
    {
        var result = MatchingFailureClassifier.Classify(
            new InvalidOperationException("Gemini API Key is not configured in DB or appsettings."));

        result.ErrorCode.Should().Be("MATCHING_CONFIGURATION_INVALID");
        result.Retryable.Should().BeFalse();
    }

    public static TheoryData<Exception> TransientProviderFailures => new()
    {
        new TimeoutException(),
        new HttpRequestException()
    };

    private static CvAnalysisValidationException ValidationException(string failureCode) =>
        new(CvAnalysisValidationResult.Invalid(
            failureCode,
            "TEST_DIAGNOSTIC",
            "$.matching_metrics"));
}
