using System;
using System.Linq;
using System.Net;
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
    private readonly JdCriticalGapEvaluator _criticalGapEvaluator;
    private readonly JdFitResultSerializer _resultSerializer;
    private readonly JdStructuredUnscoredResultFactory _unscoredResultFactory;

    public JdStageTwoMatchingService(
        IAiService aiService,
        ILogger<JdStageTwoMatchingService> logger,
        JdMatchingRequirementContextBuilder? contextBuilder = null,
        JdMatchingResponseAdapter? responseAdapter = null,
        JdFitScoreCalculator? calculator = null,
        JdCriticalGapEvaluator? criticalGapEvaluator = null,
        JdFitResultSerializer? resultSerializer = null,
        JdStructuredUnscoredResultFactory? unscoredResultFactory = null)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextBuilder = contextBuilder ?? new JdMatchingRequirementContextBuilder();
        _responseAdapter = responseAdapter ?? new JdMatchingResponseAdapter();
        _calculator = calculator ?? new JdFitScoreCalculator();
        _criticalGapEvaluator = criticalGapEvaluator ?? new JdCriticalGapEvaluator();
        _resultSerializer = resultSerializer ?? new JdFitResultSerializer();
        _unscoredResultFactory = unscoredResultFactory ?? new JdStructuredUnscoredResultFactory(
            _calculator,
            _criticalGapEvaluator,
            _resultSerializer);
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

        var fullContext = _contextBuilder.Build(jdProjection);
        var allExpectedIds = jdProjection.Groups
            .SelectMany(group => group.Items)
            .Select(item => item.ItemId)
            .ToHashSet(StringComparer.Ordinal);
        var provider = await _aiService.GetActiveProviderNameAsync();

        JdStageTwoValidatedResponse? firstAttempt = null;
        IReadOnlySet<string> requestedIds = allExpectedIds;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var jdContext = attempt == 1
                ? fullContext
                : _contextBuilder.Build(jdProjection, requestedIds);
            var managedContent = attempt == 1
                ? activePrompt.Content
                : $"{activePrompt.Content.Trim()}\n\n{BuildRetryInstruction(requestedIds)}";
            var prompt = ComposePrompt(managedContent, cvContextJson, jdContext.Json);
            _logger.LogInformation(
                "JD Stage 2 prompt prepared. PromptVersionId={PromptVersionId}; VersionTag={VersionTag}; Provider={Provider}; Attempt={Attempt}; GroupCount={GroupCount}; RequirementCount={RequirementCount}; PromptLength={PromptLength}; PromptHash={PromptHash}; SchemaHash={SchemaHash}",
                activePrompt.VersionId,
                activePrompt.VersionTag,
                provider,
                attempt,
                jdContext.GroupCount,
                jdContext.RequirementCount,
                prompt.Length,
                HashForLog(prompt),
                HashForLog(JdMatchingOutputSchema.LockedBlock));

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
                var candidate = recovered.Document == null
                    ? CreateInvalidResponse(requestedIds, recovered.WasTruncated, recovered.WarningCodes)
                    : _responseAdapter.Adapt(
                        recovered.Document,
                        jdProjection,
                        recovered.IsCompleteJson,
                        recovered.WasTruncated,
                        recovered.WarningCodes);

                if (attempt == 1 && candidate.Quality == JdStageTwoOutputQuality.COMPLETE)
                {
                    return CalculateAndSerialize(activePrompt, jdProjection, candidate, attempt);
                }

                if (attempt == 1)
                {
                    firstAttempt = candidate;
                    requestedIds = candidate.Quality == JdStageTwoOutputQuality.PARTIAL
                        ? candidate.Coverage.MissingItemIds.ToHashSet(StringComparer.Ordinal)
                        : allExpectedIds;
                    LogOutputEvaluation(activePrompt, provider, attempt, responseText, candidate);
                    continue;
                }

                var merged = _responseAdapter.MergeMissingOnly(
                    firstAttempt ?? CreateInvalidResponse(allExpectedIds, false, Array.Empty<string>()),
                    candidate,
                    allExpectedIds,
                    requestedIds);
                if (IsPublishable(merged))
                {
                    return CalculateAndSerialize(activePrompt, jdProjection, merged, attempt);
                }

                LogOutputEvaluation(activePrompt, provider, attempt, responseText, merged);
                return CreateStructuredUnscored(
                    activePrompt,
                    jdProjection,
                    merged,
                    attempt,
                    JdStructuredUnscoredReason.NoUsableAssessments);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt == 1 && IsRetryableOutputFailure(exception))
            {
                LogOutputFailure(activePrompt, provider, attempt, responseText, exception);
                firstAttempt = CreateInvalidResponse(allExpectedIds, false, new[] { "FIRST_ATTEMPT_OUTPUT_INVALID" });
                requestedIds = allExpectedIds;
                continue;
            }
            catch (HttpRequestException exception) when (IsAuthorizationFailure(exception))
            {
                LogOutputFailure(activePrompt, provider, attempt, responseText, exception);
                var unavailable = firstAttempt ?? CreateInvalidResponse(
                    allExpectedIds,
                    false,
                    new[] { "PROVIDER_AUTHORIZATION_UNAVAILABLE" });
                return IsPublishable(unavailable)
                    ? CalculateAndSerialize(
                        activePrompt,
                        jdProjection,
                        AddWarning(unavailable, "RECOVERY_ATTEMPT_FAILED"),
                        attempt)
                    : CreateStructuredUnscored(
                        activePrompt,
                        jdProjection,
                        unavailable,
                        attempt,
                        JdStructuredUnscoredReason.ProviderAuthorizationUnavailable);
            }
            catch (Exception exception) when (attempt == 2 && IsRetryableOutputFailure(exception))
            {
                LogOutputFailure(activePrompt, provider, attempt, responseText, exception);
                if (firstAttempt is not null && IsPublishable(firstAttempt))
                {
                    return CalculateAndSerialize(
                        activePrompt,
                        jdProjection,
                        AddWarning(firstAttempt, "RECOVERY_ATTEMPT_FAILED"),
                        attempt);
                }

                return CreateStructuredUnscored(
                    activePrompt,
                    jdProjection,
                    firstAttempt ?? CreateInvalidResponse(allExpectedIds, false, Array.Empty<string>()),
                    attempt,
                    JdStructuredUnscoredReason.TransientProviderExhausted);
            }
        }

        return CreateStructuredUnscored(
            activePrompt,
            jdProjection,
            firstAttempt ?? CreateInvalidResponse(allExpectedIds, false, Array.Empty<string>()),
            2,
            JdStructuredUnscoredReason.NoUsableAssessments);
    }

    public JdFitScoreCalculation CreateConfigurationUnavailableResult(
        JdRequirementProjection jdProjection)
    {
        ArgumentNullException.ThrowIfNull(jdProjection);
        var expectedIds = jdProjection.Groups
            .SelectMany(group => group.Items)
            .Select(item => item.ItemId)
            .ToHashSet(StringComparer.Ordinal);
        return _unscoredResultFactory.Create(
            jdProjection,
            CreateInvalidResponse(
                expectedIds,
                wasTruncated: false,
                new[] { "MATCHING_CONFIGURATION_UNAVAILABLE" }),
            new JdFitSerializationContext(
                Guid.Empty,
                "unavailable",
                HashForStorage(string.Empty),
                HashForStorage(JdMatchingOutputSchema.LockedBlock),
                0),
            JdStructuredUnscoredReason.MatchingConfigurationUnavailable);
    }

    private static bool IsPublishable(JdStageTwoValidatedResponse response) =>
        response.Quality != JdStageTwoOutputQuality.INVALID && response.Coverage.AcceptedCount > 0;

    private static JdStageTwoValidatedResponse AddWarning(
        JdStageTwoValidatedResponse response,
        string warning) => response with
        {
            WarningCodes = response.WarningCodes
                .Append(warning)
                .Distinct(StringComparer.Ordinal)
                .Take(100)
                .ToArray()
        };

    private JdFitScoreCalculation CalculateAndSerialize(
        PromptSnapshotDto activePrompt,
        JdRequirementProjection projection,
        JdStageTwoValidatedResponse response,
        int providerAttemptCount)
    {
        var scoreResult = _calculator.Calculate(projection, response);
        var criticalGaps = _criticalGapEvaluator.Evaluate(projection, response.ItemAssessments);
        var semanticContent = JdMatchingOutputSchema.NormalizeManagedContent(activePrompt.Content).SemanticContent;
        return _resultSerializer.Serialize(
            projection,
            response,
            scoreResult,
            criticalGaps,
            new JdFitSerializationContext(
                activePrompt.VersionId,
                activePrompt.VersionTag,
                HashForStorage(semanticContent),
                HashForStorage(JdMatchingOutputSchema.LockedBlock),
                providerAttemptCount));
    }

    private JdFitScoreCalculation CreateStructuredUnscored(
        PromptSnapshotDto activePrompt,
        JdRequirementProjection projection,
        JdStageTwoValidatedResponse response,
        int providerAttemptCount,
        JdStructuredUnscoredReason reason)
    {
        var semanticContent = JdMatchingOutputSchema.NormalizeManagedContent(activePrompt.Content).SemanticContent;
        return _unscoredResultFactory.Create(
            projection,
            response,
            new JdFitSerializationContext(
                activePrompt.VersionId,
                activePrompt.VersionTag,
                HashForStorage(semanticContent),
                HashForStorage(JdMatchingOutputSchema.LockedBlock),
                providerAttemptCount),
            reason);
    }

    private static string BuildRetryInstruction(IReadOnlySet<string> requestedIds) =>
        "RECOVERY ATTEMPT: Return assessments only for these missing reqId values. " +
        "Do not repeat any other reqId: " +
        JsonSerializer.Serialize(requestedIds.OrderBy(id => id, StringComparer.Ordinal));

    private static JdStageTwoValidatedResponse CreateInvalidResponse(
        IEnumerable<string> expectedIds,
        bool wasTruncated,
        IReadOnlyList<string> warnings)
    {
        var missing = expectedIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        return new JdStageTwoValidatedResponse(
            new Dictionary<string, JdStageTwoItemAssessment>(StringComparer.Ordinal),
            string.Empty,
            JdStageTwoOutputQuality.INVALID,
            new JdStageTwoOutputCoverage(missing.Length, 0, 0, 0, missing, wasTruncated),
            warnings);
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
        exception is TimeoutException ||
        exception is TaskCanceledException ||
        exception is HttpRequestException request && IsTransientStatus(request.StatusCode) ||
        exception is InvalidOperationException invalid &&
        invalid.Message is JdMatchingResponseValidator.InvalidStageTwoResponse or
            "AI_OUTPUT_TRUNCATED" or
            "AI_PROVIDER_INVALID_JSON";

    private static bool IsAuthorizationFailure(HttpRequestException exception) =>
        exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    private static bool IsTransientStatus(HttpStatusCode? statusCode) =>
        statusCode is null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
        (int)statusCode.Value >= 500;

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

    private void LogOutputEvaluation(
        PromptSnapshotDto prompt,
        string provider,
        int attempt,
        string? response,
        JdStageTwoValidatedResponse evaluated)
    {
        var boundedResponse = response ?? string.Empty;
        var diagnosticCounts = string.Join(',', evaluated.HandlerDiagnostics
            .GroupBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Take(10)
            .Select(group => $"{SanitizeForLog(group.Key)}:{group.Count()}"));
        var diagnosticSamples = string.Join(',', evaluated.HandlerDiagnostics
            .Take(10)
            .Select(diagnostic =>
                $"{SanitizeForLog(diagnostic.ExpectedCategory)}:{SanitizeForLog(diagnostic.ReturnedHandlerCode)}"));
        _logger.LogWarning(
            "JD Stage 2 output evaluated. PromptVersionId={PromptVersionId}; VersionTag={VersionTag}; Provider={Provider}; Attempt={Attempt}; Quality={Quality}; ResponseLength={ResponseLength}; ResponseHash={ResponseHash}; AcceptedScoreCount={AcceptedScoreCount}; ExpectedScoreCount={ExpectedScoreCount}; MissingScoreCount={MissingScoreCount}; WasTruncated={WasTruncated}; WarningCodes={WarningCodes}; HandlerDiagnosticCounts={HandlerDiagnosticCounts}; HandlerDiagnosticSamples={HandlerDiagnosticSamples}",
            prompt.VersionId,
            prompt.VersionTag,
            provider,
            attempt,
            evaluated.Quality,
            boundedResponse.Length,
            HashForLog(boundedResponse),
            evaluated.Coverage.AcceptedCount,
            evaluated.Coverage.ExpectedCount,
            evaluated.Coverage.MissingItemIds.Count,
            evaluated.Coverage.WasTruncated,
            string.Join(',', evaluated.WarningCodes),
            diagnosticCounts,
            diagnosticSamples);
    }

    private static string SanitizeForLog(string? value)
    {
        var bounded = (value ?? string.Empty).Trim();
        if (bounded.Length > 100)
        {
            bounded = bounded[..100];
        }

        return new string(bounded.Select(character => char.IsControl(character) ? '_' : character).ToArray());
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

    private static string HashForStorage(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)))
            .ToLowerInvariant();
}
