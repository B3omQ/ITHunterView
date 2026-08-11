using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdFitSerializationContext(
    Guid MatchingPromptVersionId,
    string MatchingPromptVersionTag,
    string SemanticPromptHash,
    string LockedSchemaHash,
    int ProviderAttemptCount);

public sealed record JdFitScoreCalculation
{
    private JdFitScoreCalculation(
        decimal? finalScore,
        string jsonString,
        MatchingCompletionDisposition completionDisposition)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            throw new ArgumentException("A persisted matching result is required.", nameof(jsonString));
        }

        if (completionDisposition == MatchingCompletionDisposition.ScoredBillable &&
            (!finalScore.HasValue || finalScore.Value is < 0m or > 100m))
        {
            throw new ArgumentException("A billable result requires a score in [0,100].", nameof(finalScore));
        }

        if (completionDisposition == MatchingCompletionDisposition.UnscoredRefundable && finalScore.HasValue)
        {
            throw new ArgumentException("A refundable result cannot carry a score.", nameof(finalScore));
        }

        FinalScore = finalScore;
        JsonString = jsonString;
        CompletionDisposition = completionDisposition;
    }

    public decimal? FinalScore { get; }
    public string JsonString { get; }
    public MatchingCompletionDisposition CompletionDisposition { get; }
    public bool ScoreAvailable => CompletionDisposition == MatchingCompletionDisposition.ScoredBillable;

    public static JdFitScoreCalculation Scored(decimal score, string jsonString) =>
        new(score, jsonString, MatchingCompletionDisposition.ScoredBillable);

    public static JdFitScoreCalculation Unscored(string jsonString) =>
        new(null, jsonString, MatchingCompletionDisposition.UnscoredRefundable);
}

/// <summary>
/// Writes the application-owned jd-matching/v5 persistence contract. It only
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
        if (scoreResult.CompletionDisposition == MatchingCompletionDisposition.ScoredBillable &&
            response.Quality != JdStageTwoOutputQuality.COMPLETE)
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
        decimal? roundedPercent = scoreResult.ScorePercent.HasValue
            ? Math.Round(
                Math.Clamp(scoreResult.ScorePercent.Value, 0m, 100m),
                1,
                MidpointRounding.AwayFromZero)
            : null;
        var narrative = Bound(response.Narrative, MaximumNarrativeLength);
        if (narrative.Length == 0)
        {
            narrative = scoreResult.ResultBand is null
                ? "Kết quả phân tích đã được chuẩn bị."
                : $"Kết quả đánh giá: {scoreResult.ResultBand.Label}.";
        }

        var persisted = new
        {
            mode = "jd_fit",
            contract = JdFitResultContract.Current,
            scoreAvailable = scoreResult.ScoreAvailable,
            completionDisposition = ToContractValue(scoreResult.CompletionDisposition),
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
                outputQuality = response.Quality.ToString(),
                expectedCount = response.Coverage.ExpectedCount,
                acceptedCount = response.Coverage.AcceptedCount,
                discardedCount = response.Coverage.DiscardedCount,
                missingItemIds = response.Coverage.MissingItemIds,
                wasTruncated = response.Coverage.WasTruncated,
                recoveryWarningCodes = response.WarningCodes
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.Ordinal)
                    .Take(100)
                    .ToArray()
            },
            jdFit = new
            {
                scorePercent = roundedPercent,
                resultCode = scoreResult.ResultBand?.ResultCode,
                resultLabel = scoreResult.ResultBand?.Label,
                narrative,
                requirementGroups = groups,
                criticalGaps = gapEvaluation.CriticalGaps
                    .Select(gap => SerializeGap(gap, projection, response))
                    .ToArray(),
                warningFlags = gapEvaluation.WarningFlags
                    .Distinct(StringComparer.Ordinal)
                    .Take(100)
                    .ToArray()
            }
        };

        var json = JsonSerializer.Serialize(persisted);
        return scoreResult.CompletionDisposition == MatchingCompletionDisposition.ScoredBillable
            ? JdFitScoreCalculation.Scored(roundedPercent!.Value, json)
            : JdFitScoreCalculation.Unscored(json);
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
            var isAssessed = response.ItemAssessments.TryGetValue(item.ItemId, out var assessment);
            return new
            {
                itemId = item.ItemId,
                normalizedText = item.SkillName,
                detailVerbatim = item.DetailVerbatim,
                rawMention = item.RawMention,
                category = item.Category,
                assessmentStatus = isAssessed ? "assessed" : "unresolved",
                score = isAssessed ? RoundUnit(assessment!.Score) : (decimal?)null,
                handlerCode = isAssessed ? assessment!.HandlerCode : null,
                reasoning = isAssessed ? assessment!.Reasoning : string.Empty,
                evidence = (isAssessed ? assessment!.Evidence : Array.Empty<JdMatchingEvidence>()).Select(evidence => new
                {
                    quotation = evidence.Quotation,
                    section = evidence.Section
                }).ToArray(),
                isCriticalGap = isAssessed && itemGapIds.Contains(item.ItemId),
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

    private static object SerializeGap(
        JdCriticalGap gap,
        JdRequirementProjection projection,
        JdStageTwoValidatedResponse response)
    {
        var group = projection.Groups.FirstOrDefault(candidate =>
            string.Equals(candidate.GroupId, gap.GroupId, StringComparison.Ordinal));
        var affectedIds = gap.AffectedItemIds.ToHashSet(StringComparer.Ordinal);
        var affectedItems = group?.Items
            .Where(item => affectedIds.Contains(item.ItemId))
            .ToArray() ?? Array.Empty<ProjectedJdRequirementItem>();

        var item = gap.ItemId is null
            ? null
            : group?.Items.FirstOrDefault(candidate =>
                string.Equals(candidate.ItemId, gap.ItemId, StringComparison.Ordinal));
        var requirement = item is not null
            ? ItemLabel(item, group?.RequirementVerbatim)
            : string.Join(" | ", affectedItems.Select(candidate => ItemLabel(candidate, group?.RequirementVerbatim)));
        if (requirement.Length == 0)
        {
            requirement = Bound(group?.RequirementVerbatim, MaximumNarrativeLength);
        }

        var assessments = affectedItems
            .Select(candidate => response.ItemAssessments.TryGetValue(candidate.ItemId, out var assessment)
                ? (Item: candidate, Assessment: assessment)
                : (Item: candidate, Assessment: (JdStageTwoItemAssessment?)null))
            .Where(entry => entry.Assessment is not null)
            .Select(entry => (entry.Item, Assessment: entry.Assessment!))
            .ToArray();
        var reasoning = item is not null
            ? assessments.FirstOrDefault(entry => string.Equals(entry.Item.ItemId, item.ItemId, StringComparison.Ordinal)).Assessment?.Reasoning
            : string.Join(" ", assessments
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Assessment.Reasoning))
                .Select(entry => $"{ItemLabel(entry.Item, group?.RequirementVerbatim)}: {entry.Assessment.Reasoning.Trim()}"));
        var evidence = assessments
            .SelectMany(entry => entry.Assessment.Evidence)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Quotation))
            .DistinctBy(entry => (entry.Quotation, entry.Section))
            .Take(50)
            .Select(entry => new
            {
                quotation = Bound(entry.Quotation, MaximumNarrativeLength),
                section = Bound(entry.Section, MaximumNarrativeLength)
            })
            .ToArray();
        var categories = affectedItems
            .Select(candidate => candidate.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new
        {
            gapId = BuildGapId(gap),
            code = gap.Code,
            scope = gap.Scope,
            groupId = gap.GroupId,
            itemId = gap.ItemId,
            sourceRequirementId = group?.SourceRequirementId,
            sourceSection = group?.SourceSection,
            category = item?.Category ?? (categories.Length == 1 ? categories[0] : null),
            importance = group?.Importance,
            @operator = gap.Operator,
            requiredCount = gap.RequiredCount,
            satisfiedCount = gap.SatisfiedCount,
            affectedItemIds = gap.AffectedItemIds,
            requirement = Bound(requirement, MaximumNarrativeLength),
            requirementVerbatim = Bound(group?.RequirementVerbatim, MaximumNarrativeLength),
            reasoning = Bound(reasoning, MaximumNarrativeLength),
            evidence
        };
    }

    private static string BuildGapId(JdCriticalGap gap) =>
        string.Equals(gap.Scope, "item", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(gap.ItemId)
            ? $"{gap.Code}:item:{gap.GroupId}:{gap.ItemId}"
            : $"{gap.Code}:group:{gap.GroupId}:{string.Join(',', gap.AffectedItemIds)}";

    private static string ItemLabel(ProjectedJdRequirementItem item, string? groupVerbatim)
    {
        if (!string.IsNullOrWhiteSpace(item.SkillName)) return Bound(item.SkillName, MaximumNarrativeLength);
        if (!string.IsNullOrWhiteSpace(item.RawMention)) return Bound(item.RawMention, MaximumNarrativeLength);
        return Bound(groupVerbatim, MaximumNarrativeLength);
    }

    private static decimal? RoundUnit(decimal? value) =>
        value.HasValue
            ? Math.Round(Math.Clamp(value.Value, 0m, 1m), 4, MidpointRounding.AwayFromZero)
            : null;

    private static string ToContractValue(MatchingCompletionDisposition disposition) => disposition switch
    {
        MatchingCompletionDisposition.ScoredBillable => "scored_billable",
        MatchingCompletionDisposition.UnscoredRefundable => "unscored_refundable",
        _ => throw new ArgumentOutOfRangeException(nameof(disposition), disposition, null)
    };

    private static string Bound(string? value, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
