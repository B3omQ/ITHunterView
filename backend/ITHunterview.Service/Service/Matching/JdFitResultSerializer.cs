using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdFitSerializationContext(
    Guid MatchingPromptVersionId,
    string MatchingPromptVersionTag,
    string SemanticPromptHash,
    string LockedSchemaHash,
    int ProviderAttemptCount);

public sealed record JdFitScoreCalculation(decimal FinalScore, string JsonString);

/// <summary>
/// Writes the application-owned jd-matching/v4 persistence contract. It only
/// serializes the projection, accepted provider assessments and deterministic
/// backend calculations; it never reinterprets requirement semantics.
/// </summary>
public sealed class JdFitResultSerializer
{
    private const int MaximumNarrativeLength = 4_000;

    public JdFitScoreCalculation Serialize(
        JdRequirementProjection projection,
        JdStageTwoValidatedResponse response,
        JdFitScoreResult scoreResult,
        JdCriticalGapEvaluation gapEvaluation,
        JdFitSerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(scoreResult);
        ArgumentNullException.ThrowIfNull(gapEvaluation);
        ArgumentNullException.ThrowIfNull(context);
        if (response.Quality != JdStageTwoOutputQuality.COMPLETE)
        {
            throw new InvalidOperationException("MATCHING_STAGE2_OUTPUT_INVALID");
        }

        var itemGapIds = gapEvaluation.CriticalGaps
            .Where(gap => gap.ItemId != null)
            .Select(gap => gap.ItemId!)
            .ToHashSet(StringComparer.Ordinal);
        var groupGapIds = gapEvaluation.CriticalGaps
            .Select(gap => gap.GroupId)
            .ToHashSet(StringComparer.Ordinal);

        var groups = scoreResult.Groups
            .OrderBy(group => group.SourceOrder)
            .Select(groupScore => SerializeGroup(groupScore, response, itemGapIds, groupGapIds))
            .ToArray();
        var roundedPercent = Math.Round(
            Math.Clamp(scoreResult.ScorePercent, 0m, 100m),
            1,
            MidpointRounding.AwayFromZero);
        var narrative = Bound(response.Narrative, MaximumNarrativeLength);
        if (narrative.Length == 0)
        {
            narrative = $"Kết quả đánh giá: {scoreResult.ResultBand.Label}.";
        }

        var persisted = new
        {
            mode = "jd_fit",
            contract = JdFitResultContract.Current,
            sourceJdSchemaVersion = projection.SourceSchemaVersion,
            analysis = new
            {
                providerOutputContract = "jd-stage2/v2",
                matchingPromptVersionId = context.MatchingPromptVersionId,
                matchingPromptVersionTag = Bound(context.MatchingPromptVersionTag, 100),
                semanticPromptHash = context.SemanticPromptHash,
                lockedSchemaHash = context.LockedSchemaHash,
                scoringPolicyVersion = MatchingScorePolicy.Version,
                providerAttemptCount = context.ProviderAttemptCount,
                expectedCount = response.Coverage.ExpectedCount,
                acceptedCount = response.Coverage.AcceptedCount,
                recoveryWarningCodes = response.WarningCodes
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.Ordinal)
                    .Take(100)
                    .ToArray()
            },
            jdFit = new
            {
                scorePercent = roundedPercent,
                resultCode = scoreResult.ResultBand.ResultCode,
                resultLabel = scoreResult.ResultBand.Label,
                narrative,
                requirementGroups = groups,
                criticalGaps = gapEvaluation.CriticalGaps.Select(gap => new
                {
                    code = gap.Code,
                    scope = gap.Scope,
                    groupId = gap.GroupId,
                    itemId = gap.ItemId,
                    @operator = gap.Operator,
                    requiredCount = gap.RequiredCount,
                    satisfiedCount = gap.SatisfiedCount,
                    affectedItemIds = gap.AffectedItemIds
                }).ToArray(),
                warningFlags = gapEvaluation.WarningFlags
                    .Distinct(StringComparer.Ordinal)
                    .Take(100)
                    .ToArray()
            }
        };

        return new JdFitScoreCalculation(
            roundedPercent,
            JsonSerializer.Serialize(persisted));
    }

    private static object SerializeGroup(
        JdFitGroupScore groupScore,
        JdStageTwoValidatedResponse response,
        IReadOnlySet<string> itemGapIds,
        IReadOnlySet<string> groupGapIds)
    {
        var group = groupScore.Group;
        var items = group.Items.Select((item, sourceOrder) =>
        {
            var assessment = response.ItemAssessments[item.ItemId];
            return new
            {
                itemId = item.ItemId,
                normalizedText = item.SkillName,
                detailVerbatim = item.DetailVerbatim,
                rawMention = item.RawMention,
                category = item.Category,
                score = RoundUnit(assessment.Score),
                handlerCode = assessment.HandlerCode,
                reasoning = assessment.Reasoning,
                evidence = assessment.Evidence.Select(evidence => new
                {
                    quotation = evidence.Quotation,
                    section = evidence.Section
                }).ToArray(),
                isCriticalGap = itemGapIds.Contains(item.ItemId),
                sourceOrder
            };
        }).ToArray();

        return new
        {
            groupId = group.GroupId,
            sourceRequirementId = group.SourceRequirementId,
            intent = group.Intent,
            @operator = group.Operator,
            minSatisfied = group.MinSatisfied,
            importance = group.Importance,
            sourceSection = group.SourceSection,
            requirementVerbatim = group.RequirementVerbatim,
            sourceOrder = groupScore.SourceOrder,
            groupScore = RoundUnit(groupScore.GroupScore),
            selectedItemIds = groupScore.SelectedItemIds,
            satisfiedItemIds = groupScore.SatisfiedItemIds,
            isCriticalGap = groupGapIds.Contains(group.GroupId),
            items
        };
    }

    private static decimal RoundUnit(decimal value) =>
        Math.Round(Math.Clamp(value, 0m, 1m), 4, MidpointRounding.AwayFromZero);

    private static string Bound(string? value, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
