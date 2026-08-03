using System;
using System.Collections.Generic;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record HardcodeJdRequirementScoreDecision(
    bool HasRequirementGroups,
    JdRequirementProjection? Projection,
    JdHardcodeRequirementEvaluation? Evaluation,
    string? FailureCode,
    bool CanUseLegacyCompatibilityFallback);

/// <summary>
/// Projects the effective JD once, then delegates only technical groups to the
/// hardcode skill component. A malformed legacy document may use compatibility
/// metrics, but a malformed v3 document must fail closed: falling back would
/// silently discard its group semantics.
/// </summary>
public sealed class HardcodeJdRequirementScoringService
{
    private readonly IJdRequirementProjector _projector;
    private readonly JdHardcodeRequirementEvaluator _evaluator;

    public HardcodeJdRequirementScoringService(
        IJdRequirementProjector projector,
        JdHardcodeRequirementEvaluator evaluator)
    {
        _projector = projector;
        _evaluator = evaluator;
    }

    public HardcodeJdRequirementScoreDecision Evaluate(
        string? effectiveJdJson,
        IReadOnlyCollection<string> cvSkills)
    {
        try
        {
            var projection = _projector.Project(effectiveJdJson);
            if (projection.Groups.Count == 0)
            {
                return new HardcodeJdRequirementScoreDecision(false, projection, null, null, true);
            }

            return new HardcodeJdRequirementScoreDecision(
                true,
                projection,
                _evaluator.Evaluate(projection, cvSkills),
                null,
                false);
        }
        catch (InvalidOperationException exception) when (exception.Message == JdRequirementProjector.InvalidEffectiveJdAnalysis)
        {
            return new HardcodeJdRequirementScoreDecision(
                false,
                null,
                null,
                JdRequirementProjector.InvalidEffectiveJdAnalysis,
                !ClaimsV3Contract(effectiveJdJson));
        }
    }

    private static bool ClaimsV3Contract(string? effectiveJdJson)
    {
        if (string.IsNullOrWhiteSpace(effectiveJdJson))
            return false;

        try
        {
            using var document = JsonDocument.Parse(effectiveJdJson);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("schema_version", out var version) &&
                   string.Equals(version.GetString(), "jd-analysis/v3", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
