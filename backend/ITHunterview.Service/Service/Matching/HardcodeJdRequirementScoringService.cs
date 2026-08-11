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
    bool CanUseLegacyCompatibilityFallback)
{
    public string? AnalysisQuality => Projection?.AnalysisQuality;
}

/// <summary>
/// Projects the effective JD once, then delegates only technical groups to the
/// hardcode skill component. If no usable group remains, callers may inspect
/// independently safe compatibility metrics; prompt/schema labels never choose
/// an algorithm or force a terminal failure.
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
        var projection = _projector.Project(effectiveJdJson);
        if (projection.Groups.Count == 0)
        {
            return new HardcodeJdRequirementScoreDecision(
                false,
                projection,
                null,
                projection.Quality == ITHunterview.Domain.Enums.JdAnalysisQuality.INVALID
                    ? JdRequirementProjector.InvalidEffectiveJdAnalysis
                    : null,
                !ClaimsStructuredAnalysisSchema(effectiveJdJson));
        }

        return new HardcodeJdRequirementScoreDecision(
            true,
            projection,
            _evaluator.Evaluate(projection, cvSkills),
            null,
            false);
    }

    // This inspects the analysis document shape only. It is deliberately
    // unrelated to prompt-pair contract metadata and never selects a scoring
    // implementation when usable projected groups exist.
    private static bool ClaimsStructuredAnalysisSchema(string? effectiveJdJson)
    {
        if (string.IsNullOrWhiteSpace(effectiveJdJson)) return false;
        try
        {
            using var document = JsonDocument.Parse(effectiveJdJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("schema_version", out var schema) ||
                schema.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return schema.GetString() is { } version &&
                   (version.Equals("jd-analysis/v3", StringComparison.OrdinalIgnoreCase) ||
                    version.Equals("jd-analysis/v4", StringComparison.OrdinalIgnoreCase) ||
                    version.Equals("jd-analysis/v5", StringComparison.OrdinalIgnoreCase) ||
                    version.Equals("jd-analysis-effective/v1", StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
