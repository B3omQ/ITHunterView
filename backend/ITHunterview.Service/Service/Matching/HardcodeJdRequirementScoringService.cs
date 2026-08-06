using System;
using System.Collections.Generic;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record HardcodeJdRequirementScoreDecision(
    bool HasRequirementGroups,
    JdRequirementProjection? Projection,
    JdHardcodeRequirementEvaluation? Evaluation,
    string? FailureCode);

/// <summary>
/// Projects the effective JD once, then delegates only technical groups to the
/// hardcode skill component. Invalid analysis deliberately falls back to the
/// legacy normalized metrics path owned by the caller.
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
                return new HardcodeJdRequirementScoreDecision(false, projection, null, null);
            }

            return new HardcodeJdRequirementScoreDecision(
                true,
                projection,
                _evaluator.Evaluate(projection, cvSkills),
                null);
        }
        catch (InvalidOperationException exception) when (exception.Message == JdRequirementProjector.InvalidEffectiveJdAnalysis)
        {
            return new HardcodeJdRequirementScoreDecision(
                false,
                null,
                null,
                JdRequirementProjector.InvalidEffectiveJdAnalysis);
        }
    }
}
