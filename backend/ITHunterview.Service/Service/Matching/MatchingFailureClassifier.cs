using System;
using System.Net.Http;
using ITHunterview.Service.Exceptions;

namespace ITHunterview.Service.Service.Matching;

public sealed record MatchingFailureClassification(string ErrorCode, bool Retryable);

/// <summary>
/// Converts provider/parser exceptions to bounded codes suitable for storage
/// and logs. Exception messages and provider bodies never cross this boundary.
/// </summary>
public static class MatchingFailureClassifier
{
    public static MatchingFailureClassification Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message ?? string.Empty;

        if (exception is CvAnalysisValidationException cvAnalysisFailure)
        {
            if (cvAnalysisFailure.FailureCode is "CV_ANALYSIS_RAW_TEXT_REQUIRED"
                or "CV_ANALYSIS_INPUT_INVALID")
            {
                return new MatchingFailureClassification("MATCHING_INPUT_INVALID", false);
            }

            // A typed CV validation failure means the provider already returned
            // a response and repeating the same request cannot repair its contract.
            return new MatchingFailureClassification("AI_OUTPUT_INVALID", false);
        }

        if (message.StartsWith("MATCHING_STAGE2_OUTPUT_INVALID", StringComparison.Ordinal)
            || exception is FormatException
            || exception is System.Text.Json.JsonException)
            return new MatchingFailureClassification("AI_OUTPUT_INVALID", false);

        if (message is "AI_PROVIDER_TIMEOUT"
            or "AI_PROVIDER_REQUEST_FAILED"
            or "AI_PROVIDER_HTTP_ERROR"
            or "AI_PROVIDER_INVALID_JSON")
            return new MatchingFailureClassification(message, true);

        if (message is "AI_RESPONSE_TOO_LARGE"
            or "SNAPSHOT_HASH_MISMATCH")
            return new MatchingFailureClassification(message, false);

        if (exception is TimeoutException
            || exception is OperationCanceledException
            || exception is HttpRequestException)
            return new MatchingFailureClassification("AI_PROVIDER_TIMEOUT", true);

        if (message is "JOB_ANALYSIS_EXTRACTION_SERVICE_NOT_CONFIGURED"
            or "CV_ANALYSIS_EMPTY_OUTPUT"
            or "CV_ANALYSIS_INVALID_FOR_MATCHING"
            or "INVALID_EFFECTIVE_JD_ANALYSIS"
            or "SNAPSHOT_INVALID")
            return new MatchingFailureClassification("MATCHING_INPUT_INVALID", false);

        if (message.StartsWith("MATCHING_PROMPT_PLACEHOLDER_MISSING", StringComparison.Ordinal)
            || message.StartsWith("PROMPT_CONFIGURATION_INVALID", StringComparison.Ordinal)
            || message is "MATCHING_LEGACY_CONTRACT_UNREPRESENTABLE"
            || message.StartsWith("INVALID_JD_ANALYSIS", StringComparison.Ordinal))
            return new MatchingFailureClassification("MATCHING_CONFIGURATION_INVALID", false);

        return new MatchingFailureClassification("MATCHING_TECHNICAL_ERROR", true);
    }
}
