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
