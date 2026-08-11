using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Executes the single approved Stage 2 matching path. Prompt metadata is
/// descriptive only; it never selects a different output contract.
/// </summary>
public sealed class JdStageTwoMatchingService : IJdStageTwoMatchingService
{
    private const string StageTwoOutputInvalid = "MATCHING_STAGE2_OUTPUT_INVALID";

    private readonly IAiService _aiService;
    private readonly ILogger<JdStageTwoMatchingService> _logger;
    private readonly JdMatchingRequirementContextBuilder _contextBuilder;
    private readonly JdMatchingResponseAdapter _responseAdapter;
    private readonly JdFitScoreCalculator _calculator;

    public JdStageTwoMatchingService(
        IAiService aiService,
        ILogger<JdStageTwoMatchingService> logger,
        JdMatchingRequirementContextBuilder? contextBuilder = null,
        JdMatchingResponseAdapter? responseAdapter = null,
        JdFitScoreCalculator? calculator = null)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextBuilder = contextBuilder ?? new JdMatchingRequirementContextBuilder();
        _responseAdapter = responseAdapter ?? new JdMatchingResponseAdapter();
        _calculator = calculator ?? new JdFitScoreCalculator();
    }

    public async Task<JdFitScoreCalculation> ExecuteAsync(
        PromptSnapshotDto activePrompt,
        string cvContextJson,
        JdRequirementProjection jdProjection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activePrompt);
        ArgumentNullException.ThrowIfNull(jdProjection);
        if (string.IsNullOrWhiteSpace(cvContextJson))
        {
            throw new InvalidOperationException("CV_ANALYSIS_EMPTY_OUTPUT");
        }

        var jdContext = _contextBuilder.Build(jdProjection);
        var prompt = ComposePrompt(activePrompt.Content, cvContextJson, jdContext.Json);
        var promptHash = HashForLog(prompt);
        var schemaHash = HashForLog(JdMatchingOutputSchema.LockedBlock);
        var provider = await _aiService.GetActiveProviderNameAsync();

        _logger.LogInformation(
            "JD Stage 2 prompt prepared. PromptVersionId={PromptVersionId}; VersionTag={VersionTag}; Provider={Provider}; GroupCount={GroupCount}; RequirementCount={RequirementCount}; PromptLength={PromptLength}; PromptHash={PromptHash}; SchemaHash={SchemaHash}",
            activePrompt.VersionId,
            activePrompt.VersionTag,
            provider,
            jdContext.GroupCount,
            jdContext.RequirementCount,
            prompt.Length,
            promptHash,
            schemaHash);

        JdStageTwoValidatedResponse? bestPartial = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var options = attempt == 1
                ? AiGenerationOptions.JdMatchingJsonScoring
                : AiGenerationOptions.JdMatchingJsonRetry;

            string? responseText = null;
            try
            {
                responseText = await _aiService.GenerateTextAsync(
                    prompt,
                    systemPrompt: null,
                    provider,
                    options,
                    cancellationToken,
                    featureCode: "CV_JD_MATCHING");

                using var recovered = JdMatchingOutputRecovery.Recover(responseText);
                if (recovered.Document == null)
                {
                    throw new InvalidOperationException(JdMatchingResponseValidator.InvalidStageTwoResponse);
                }

                var candidate = _responseAdapter.Adapt(
                    recovered.Document,
                    jdProjection,
                    recovered.IsCompleteJson,
                    recovered.WasTruncated,
                    recovered.WarningCodes);
                if (candidate.Quality == JdStageTwoOutputQuality.INVALID)
                {
                    throw new InvalidOperationException(JdMatchingResponseValidator.InvalidStageTwoResponse);
                }

                if (candidate.Quality == JdStageTwoOutputQuality.COMPLETE)
                {
                    return _calculator.Calculate(jdProjection, candidate);
                }

                bestPartial = bestPartial == null
                    ? candidate
                    : _responseAdapter.MergePartialAttempts(bestPartial, candidate);
                LogPartialOutputAccepted(activePrompt, provider, attempt, responseText, bestPartial);
                if (attempt == 2)
                {
                    return _calculator.Calculate(jdProjection, bestPartial);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt == 1 && IsRetryableOutputFailure(exception))
            {
                LogOutputFailure(activePrompt, provider, attempt, responseText, exception);
                continue;
            }
            catch (Exception exception) when (IsRetryableOutputFailure(exception))
            {
                LogOutputFailure(activePrompt, provider, attempt, responseText, exception);
                if (bestPartial != null)
                {
                    return _calculator.Calculate(jdProjection, bestPartial);
                }
                throw new InvalidOperationException(StageTwoOutputInvalid);
            }
            catch (Exception exception) when (attempt == 2 && bestPartial != null)
            {
                _logger.LogWarning(
                    "JD Stage 2 retry failed after a usable partial output; returning the partial result. PromptVersionId={PromptVersionId}; Provider={Provider}; ErrorType={ErrorType}",
                    activePrompt.VersionId,
                    provider,
                    exception.GetType().Name);
                return _calculator.Calculate(jdProjection, bestPartial);
            }
        }

        throw new InvalidOperationException(StageTwoOutputInvalid);
    }

    private static string ComposePrompt(string managedContent, string cvContextJson, string jdContextJson)
    {
        var composed = JdMatchingOutputSchema.Compose(managedContent);
        composed = ReplaceExactlyOnce(composed, JdMatchingPromptContract.CvPlaceholder, cvContextJson);
        composed = ReplaceExactlyOnce(composed, JdMatchingPromptContract.RequirementsPlaceholder, jdContextJson);
        return composed;
    }

    private static string ReplaceExactlyOnce(string prompt, string placeholder, string replacement)
    {
        var first = JdMatchingPromptContract.FindOperationalPlaceholderIndex(prompt, placeholder);
        if (first < 0)
        {
            throw new InvalidOperationException($"MATCHING_PROMPT_PLACEHOLDER_INVALID:{placeholder}");
        }

        return prompt[..first] + replacement + prompt[(first + placeholder.Length)..];
    }

    private static bool IsRetryableOutputFailure(Exception exception) =>
        exception is JsonException ||
        exception is AiProviderOutputTruncatedException ||
        exception is InvalidOperationException invalid &&
        invalid.Message is JdMatchingResponseValidator.InvalidStageTwoResponse or
            "AI_OUTPUT_TRUNCATED" or
            "AI_PROVIDER_INVALID_JSON";

    private void LogOutputFailure(
        PromptSnapshotDto prompt,
        string provider,
        int attempt,
        string? response,
        Exception exception)
    {
        var boundedResponse = response ?? string.Empty;
        _logger.LogWarning(
            "JD Stage 2 output rejected. PromptVersionId={PromptVersionId}; VersionTag={VersionTag}; Provider={Provider}; Attempt={Attempt}; ResponseLength={ResponseLength}; ResponseHash={ResponseHash}; FailureCode={FailureCode}",
            prompt.VersionId,
            prompt.VersionTag,
            provider,
            attempt,
            boundedResponse.Length,
            HashForLog(boundedResponse),
            GetBoundedFailureCode(exception));
    }

    private void LogPartialOutputAccepted(
        PromptSnapshotDto prompt,
        string provider,
        int attempt,
        string? response,
        JdStageTwoValidatedResponse partial)
    {
        var boundedResponse = response ?? string.Empty;
        _logger.LogWarning(
            "JD Stage 2 partial output accepted. PromptVersionId={PromptVersionId}; VersionTag={VersionTag}; Provider={Provider}; Attempt={Attempt}; ResponseLength={ResponseLength}; ResponseHash={ResponseHash}; AcceptedScoreCount={AcceptedScoreCount}; ExpectedScoreCount={ExpectedScoreCount}; MissingScoreCount={MissingScoreCount}; WasTruncated={WasTruncated}; WarningCodes={WarningCodes}",
            prompt.VersionId,
            prompt.VersionTag,
            provider,
            attempt,
            boundedResponse.Length,
            HashForLog(boundedResponse),
            partial.Coverage?.AcceptedScoreCount ?? partial.ItemScores.Count,
            partial.Coverage?.ExpectedScoreCount ?? partial.ItemScores.Count,
            partial.Coverage?.MissingScoreCount ?? 0,
            partial.Coverage?.WasTruncated ?? false,
            string.Join(',', partial.WarningCodes ?? Array.Empty<string>()));
    }

    private static string GetBoundedFailureCode(Exception exception) =>
        exception switch
        {
            JsonException => "JSON_PARSE_FAILED",
            AiProviderOutputTruncatedException => "AI_OUTPUT_TRUNCATED",
            InvalidOperationException invalid when invalid.Message == JdMatchingResponseValidator.InvalidStageTwoResponse
                => JdMatchingResponseValidator.InvalidStageTwoResponse,
            InvalidOperationException invalid when invalid.Message == "AI_OUTPUT_TRUNCATED"
                => "AI_OUTPUT_TRUNCATED",
            InvalidOperationException invalid when invalid.Message == "AI_PROVIDER_INVALID_JSON"
                => "AI_PROVIDER_INVALID_JSON",
            _ => "AI_OUTPUT_INVALID"
        };

    private static string HashForLog(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)))[..16]
            .ToLowerInvariant();
}
