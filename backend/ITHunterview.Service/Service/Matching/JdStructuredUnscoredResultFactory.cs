using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public enum JdStructuredUnscoredReason
{
    NoUsableAssessments,
    TransientProviderExhausted,
    ProviderAuthorizationUnavailable,
    MatchingConfigurationUnavailable
}

/// <summary>
/// Produces an application-owned terminal result without inventing an AI score.
/// All projected requirements remain available to the report as unresolved.
/// </summary>
public sealed class JdStructuredUnscoredResultFactory
{
    private readonly JdFitScoreCalculator _calculator;
    private readonly JdCriticalGapEvaluator _criticalGapEvaluator;
    private readonly JdFitResultSerializer _serializer;

    public JdStructuredUnscoredResultFactory(
        JdFitScoreCalculator? calculator = null,
        JdCriticalGapEvaluator? criticalGapEvaluator = null,
        JdFitResultSerializer? serializer = null)
    {
        _calculator = calculator ?? new JdFitScoreCalculator();
        _criticalGapEvaluator = criticalGapEvaluator ?? new JdCriticalGapEvaluator();
        _serializer = serializer ?? new JdFitResultSerializer();
    }

    public JdFitScoreCalculation Create(
        JdRequirementProjection projection,
        JdStageTwoValidatedResponse response,
        JdFitSerializationContext context,
        JdStructuredUnscoredReason reason)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(context);

        var boundedCode = reason switch
        {
            JdStructuredUnscoredReason.NoUsableAssessments => "NO_USABLE_ASSESSMENTS",
            JdStructuredUnscoredReason.TransientProviderExhausted => "TRANSIENT_PROVIDER_EXHAUSTED",
            JdStructuredUnscoredReason.ProviderAuthorizationUnavailable => "PROVIDER_AUTHORIZATION_UNAVAILABLE",
            JdStructuredUnscoredReason.MatchingConfigurationUnavailable => "MATCHING_CONFIGURATION_UNAVAILABLE",
            _ => "SCORE_UNAVAILABLE"
        };
        var normalized = response with
        {
            WarningCodes = response.WarningCodes
                .Append(boundedCode)
                .Distinct(StringComparer.Ordinal)
                .Take(100)
                .ToArray()
        };
        var score = _calculator.Calculate(projection, normalized);
        var gaps = _criticalGapEvaluator.Evaluate(projection, normalized.ItemAssessments);
        return _serializer.Serialize(projection, normalized, score, gaps, context);
    }
}
