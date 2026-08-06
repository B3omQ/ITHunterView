using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdStageTwoContext(string Json, int RequirementGroupCount, int RequirementItemCount);

public sealed record JdStageTwoItemScore(
    string ItemId,
    string HandlerCode,
    decimal HandlerScore,
    string Reasoning,
    string Confidence,
    IReadOnlyList<string> Evidence);

public sealed record JdStageTwoPenalty(string Code, bool Triggered, string Evidence);

public sealed record JdStageTwoValidatedResponse(
    IReadOnlyDictionary<string, JdStageTwoItemScore> ItemScores,
    string Narrative,
    JsonElement Improvements,
    IReadOnlyList<JdStageTwoPenalty> Penalties);

public sealed record JdFitScoreCalculation(decimal FinalScore, string JsonString);

/// <summary>Builds the stage-two input without flattening v3 JD groups.</summary>
public sealed class JdStageTwoContextBuilder
{
    public const string Contract = "jd-matching/v3";

    public JdStageTwoContext Build(JdRequirementProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var groups = projection.Groups.Select(group => new
        {
            groupId = group.GroupId,
            @operator = group.Operator,
            minSatisfied = group.MinSatisfied,
            importance = group.Importance,
            items = group.Items.Select(item => new
            {
                itemId = item.ItemId,
                category = item.Category,
                skillName = item.SkillName,
                detailVerbatim = item.DetailVerbatim,
                rawMention = item.RawMention,
                sourceSection = item.SourceSection,
                evidences = item.Evidences,
                minYears = item.MinYears,
                maxYears = item.MaxYears
            })
        }).ToList();

        var json = JsonSerializer.Serialize(new
        {
            contract = Contract,
            sourceJdSchemaVersion = projection.SourceSchemaVersion,
            requirementGroups = groups
        }, new JsonSerializerOptions { WriteIndented = true });

        return new JdStageTwoContext(json, groups.Count, groups.Sum(group => group.items.Count()));
    }
}

/// <summary>
/// Rejects incomplete or out-of-range model scores. A missing item must never be
/// silently converted to a zero, because that would fabricate a critical gap.
/// </summary>
public sealed class JdStageTwoResponseValidator
{
    public const string InvalidStageTwoResponse = "INVALID_STAGE_TWO_RESPONSE";
    private const string CredibilityPenaltyCode = "PNL_TC1_01";

    public JdStageTwoValidatedResponse Validate(JsonDocument response, JdRequirementProjection projection)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(projection);

        try
        {
            var root = response.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("itemScores", out var itemScoresElement) ||
                itemScoresElement.ValueKind != JsonValueKind.Array)
            {
                throw Invalid();
            }

            var expectedIds = projection.Groups
                .SelectMany(group => group.Items)
                .Select(item => item.ItemId)
                .ToHashSet(StringComparer.Ordinal);
            if (expectedIds.Count == 0)
            {
                throw Invalid();
            }

            var scores = new Dictionary<string, JdStageTwoItemScore>(StringComparer.Ordinal);
            foreach (var itemScore in itemScoresElement.EnumerateArray())
            {
                if (itemScore.ValueKind != JsonValueKind.Object)
                {
                    throw Invalid();
                }

                var itemId = RequiredString(itemScore, "itemId");
                var handlerCode = RequiredString(itemScore, "handlerCode");
                var score = RequiredDecimal(itemScore, "handlerScore");
                if (score is < 0m or > 1m || !expectedIds.Contains(itemId) || !scores.TryAdd(itemId,
                    new JdStageTwoItemScore(
                        itemId,
                        handlerCode,
                        score,
                        OptionalString(itemScore, "reasoning"),
                        OptionalString(itemScore, "confidence", "unknown"),
                        ReadStrings(itemScore, "evidence"))))
                {
                    throw Invalid();
                }
            }

            if (!scores.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedIds))
            {
                throw Invalid();
            }

            var improvements = root.TryGetProperty("improvements", out var improvementsElement)
                && improvementsElement.ValueKind == JsonValueKind.Array
                    ? improvementsElement.Clone()
                    : EmptyArray();
            return new JdStageTwoValidatedResponse(
                scores,
                OptionalString(root, "narrative", ""),
                improvements,
                ReadPenalties(root));
        }
        catch (InvalidOperationException exception) when (exception.Message == InvalidStageTwoResponse)
        {
            throw;
        }
        catch (Exception)
        {
            throw Invalid();
        }
    }

    private static IReadOnlyList<JdStageTwoPenalty> ReadPenalties(JsonElement root)
    {
        if (!root.TryGetProperty("penalties", out var penaltiesElement))
        {
            return Array.Empty<JdStageTwoPenalty>();
        }
        if (penaltiesElement.ValueKind != JsonValueKind.Array)
        {
            throw Invalid();
        }

        var penalties = new List<JdStageTwoPenalty>();
        foreach (var penalty in penaltiesElement.EnumerateArray())
        {
            if (penalty.ValueKind != JsonValueKind.Object ||
                !string.Equals(RequiredString(penalty, "code"), CredibilityPenaltyCode, StringComparison.Ordinal) ||
                !penalty.TryGetProperty("triggered", out var triggered) ||
                (triggered.ValueKind is not JsonValueKind.True and not JsonValueKind.False))
            {
                throw Invalid();
            }
            penalties.Add(new JdStageTwoPenalty(
                CredibilityPenaltyCode,
                triggered.GetBoolean(),
                OptionalString(penalty, "evidence")));
        }

        return penalties;
    }

    private static string RequiredString(JsonElement element, string property)
    {
        var value = OptionalString(element, property);
        return value.Length > 0 ? value : throw Invalid();
    }

    private static string OptionalString(JsonElement element, string property, string fallback = "") =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? fallback
            : fallback;

    private static decimal RequiredDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var score))
        {
            throw Invalid();
        }
        return score;
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var values)) return Array.Empty<string>();
        if (values.ValueKind != JsonValueKind.Array) throw Invalid();
        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString()?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static JsonElement EmptyArray()
    {
        using var empty = JsonDocument.Parse("[]");
        return empty.RootElement.Clone();
    }

    private static InvalidOperationException Invalid() => new(InvalidStageTwoResponse);
}

/// <summary>Owns Pool A/B, critical-gap, credibility and KSW decisions.</summary>
public sealed class JdFitScoreCalculator
{
    private const decimal PoolAMaximum = 70m;
    private const decimal PoolBMaximum = 30m;
    private const decimal CredibilityDeduction = 15m;

    public JdFitScoreCalculation Calculate(JdRequirementProjection projection, JdStageTwoValidatedResponse response)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(response);

        decimal poolAActual = 0m;
        decimal poolAMax = 0m;
        decimal poolBActual = 0m;
        decimal poolBMax = 0m;
        var calculatedGroups = new List<GroupCalculation>();

        foreach (var group in projection.Groups)
        {
            var itemScores = group.Items
                .Select(item => new ItemCalculation(item, response.ItemScores[item.ItemId]))
                .ToList();
            var selectedItems = SelectScoredItems(group, itemScores);
            var groupScore = WeightedAverage(selectedItems);
            var groupWeight = group.Items.Average(item => item.CategoryWeight);
            var criticalGap = group.Importance == "must_have" && groupScore == 0m;
            var calculation = new GroupCalculation(group, itemScores, selectedItems, groupScore, groupWeight, criticalGap);
            calculatedGroups.Add(calculation);

            if (group.Importance == "must_have")
            {
                poolAActual += groupScore * groupWeight;
                poolAMax += groupWeight;
            }
            else
            {
                poolBActual += groupScore * groupWeight;
                poolBMax += groupWeight;
            }
        }

        var poolA = poolAMax == 0m ? PoolAMaximum : poolAActual / poolAMax * PoolAMaximum;
        var poolB = poolBMax == 0m ? PoolBMaximum : poolBActual / poolBMax * PoolBMaximum;
        var criticalGroups = calculatedGroups.Where(group => group.CriticalGap).ToList();
        var poolACapped = criticalGroups.Count >= 2;
        if (poolACapped)
        {
            poolA = Math.Min(poolA, 28m);
        }

        var coreTechnicalGroups = calculatedGroups
            .Where(group => group.Group.Importance == "must_have" &&
                            group.Group.Items.Count > 0 &&
                            group.Group.Items.All(item => item.Category == "tech_skill"))
            .ToList();
        var ksw01Triggered = coreTechnicalGroups.Count > 0 &&
            coreTechnicalGroups.All(group => group.ItemScores.All(item => item.Score.HandlerScore == 0m));

        var penalties = new List<object>();
        if (poolACapped)
        {
            penalties.Add(new
            {
                code = "RULE_TC1_02",
                triggered = true,
                deduction = 0m,
                evidence = ">= 2 CRITICAL_GAP groups found. Pool A capped at 28 points."
            });
        }

        var credibilityTriggered = response.Penalties.Any(penalty => penalty.Code == "PNL_TC1_01" && penalty.Triggered);
        if (credibilityTriggered)
        {
            penalties.Add(new
            {
                code = "PNL_TC1_01",
                triggered = true,
                deduction = CredibilityDeduction,
                evidence = response.Penalties.First(penalty => penalty.Code == "PNL_TC1_01" && penalty.Triggered).Evidence
            });
        }
        if (ksw01Triggered)
        {
            penalties.Add(new
            {
                code = "KSW_01",
                triggered = true,
                deduction = 0m,
                evidence = "Every item in every core must-have technical group received a zero score."
            });
        }

        var finalScore = Math.Clamp(poolA + poolB - (credibilityTriggered ? CredibilityDeduction : 0m), 0m, 100m);
        if (ksw01Triggered)
        {
            finalScore = 15m;
        }

        var requirementGroups = calculatedGroups.Select(group => new
        {
            groupId = group.Group.GroupId,
            @operator = group.Group.Operator,
            minSatisfied = group.Group.MinSatisfied,
            importance = group.Group.Importance,
            categoryWeight = Math.Round(group.GroupWeight, 4),
            handlerScore = Math.Round(group.GroupScore, 4),
            selectedItemIds = group.SelectedItems.Select(item => item.Item.ItemId).ToList(),
            flag = group.CriticalGap ? "CRITICAL_GAP" : (string?)null,
            items = group.ItemScores.Select(item => new
            {
                itemId = item.Item.ItemId,
                normalizedText = item.Item.SkillName,
                detailVerbatim = item.Item.DetailVerbatim,
                importance = group.Group.Importance,
                category = item.Item.Category,
                categoryWeight = item.Item.CategoryWeight,
                handlerUsed = item.Item.Category,
                handlerCode = item.Score.HandlerCode,
                handlerScore = item.Score.HandlerScore,
                reasoning = item.Score.Reasoning,
                confidence = item.Score.Confidence,
                evidence = item.Score.Evidence
            })
        }).ToList();

        var requirementScores = calculatedGroups.SelectMany(group => group.ItemScores.Select(item => new
        {
            reqId = item.Item.ItemId,
            groupId = group.Group.GroupId,
            normalizedText = item.Item.SkillName,
            detailVerbatim = item.Item.DetailVerbatim,
            importance = group.Group.Importance,
            category = item.Item.Category,
            categoryWeight = item.Item.CategoryWeight,
            handlerUsed = item.Item.Category,
            handlerCode = item.Score.HandlerCode,
            handlerScore = item.Score.HandlerScore,
            reasoning = item.Score.Reasoning,
            confidence = item.Score.Confidence,
            evidence = item.Score.Evidence,
            flag = group.CriticalGap ? "CRITICAL_GAP" : (string?)null
        })).ToList();

        var json = JsonSerializer.Serialize(new
        {
            mode = "jd_fit",
            contract = JdStageTwoContextBuilder.Contract,
            sourceJdSchemaVersion = projection.SourceSchemaVersion,
            jdFit = new
            {
                score = Math.Round(finalScore, 1),
                result = Classify(finalScore),
                killSwitchTriggered = ksw01Triggered,
                poolACapped,
                poolA = new { score = Math.Round(poolA, 1), max = PoolAMaximum },
                poolB = new { score = Math.Round(poolB, 1), max = PoolBMaximum },
                requirementGroups,
                requirementScores,
                criticalGaps = criticalGroups.Select(group => new
                {
                    groupId = group.Group.GroupId,
                    requirement = string.Join(" OR ", group.Group.Items.Select(item => item.SkillName)),
                    importance = group.Group.Importance,
                    category = string.Join(", ", group.Group.Items.Select(item => item.Category).Distinct()),
                    flag = "CRITICAL_GAP"
                }).ToList(),
                penalties,
                narrative = response.Narrative
            },
            improvements = response.Improvements,
            processingTime = 1000
        }, new JsonSerializerOptions { WriteIndented = true });

        return new JdFitScoreCalculation(finalScore, json);
    }

    private static IReadOnlyList<ItemCalculation> SelectScoredItems(
        ProjectedJdRequirementGroup group,
        IReadOnlyList<ItemCalculation> items) => group.Operator switch
        {
            "all_of" => items,
            "one_of" => new[] { items.OrderByDescending(item => item.Score.HandlerScore).ThenBy(item => item.Item.ItemId, StringComparer.Ordinal).First() },
            "at_least_n" => items.OrderByDescending(item => item.Score.HandlerScore).ThenBy(item => item.Item.ItemId, StringComparer.Ordinal).Take(group.MinSatisfied).ToList(),
            _ => throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis)
        };

    private static decimal WeightedAverage(IReadOnlyList<ItemCalculation> items)
    {
        var maximum = items.Sum(item => item.Item.CategoryWeight);
        return maximum == 0m
            ? 0m
            : items.Sum(item => item.Score.HandlerScore * item.Item.CategoryWeight) / maximum;
    }

    private static string Classify(decimal finalScore) => finalScore switch
    {
        >= 80m => "Highly Suitable",
        >= 60m => "Suitable",
        >= 40m => "Partially Suitable",
        _ => "Not Suitable"
    };

    private sealed record ItemCalculation(ProjectedJdRequirementItem Item, JdStageTwoItemScore Score);
    private sealed record GroupCalculation(
        ProjectedJdRequirementGroup Group,
        IReadOnlyList<ItemCalculation> ItemScores,
        IReadOnlyList<ItemCalculation> SelectedItems,
        decimal GroupScore,
        decimal GroupWeight,
        bool CriticalGap);
}
