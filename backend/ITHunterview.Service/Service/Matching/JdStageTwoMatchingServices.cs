using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdStageTwoItemScore(
    string ItemId,
    string HandlerCode,
    decimal HandlerScore,
    string Reasoning,
    string Confidence,
    IReadOnlyList<string> Evidence);

public sealed record JdStageTwoPenalty(string Code, bool Triggered, string Evidence);

public enum JdStageTwoOutputQuality
{
    COMPLETE,
    PARTIAL,
    INVALID
}

public sealed record JdStageTwoOutputCoverage(
    int ExpectedScoreCount,
    int InputScoreCount,
    int AcceptedScoreCount,
    int DiscardedScoreCount,
    int MissingScoreCount,
    bool WasTruncated);

public sealed record JdStageTwoValidatedResponse(
    IReadOnlyDictionary<string, JdStageTwoItemScore> ItemScores,
    string Narrative,
    JsonElement Improvements,
    IReadOnlyList<JdStageTwoPenalty> Penalties,
    JdStageTwoOutputQuality Quality = JdStageTwoOutputQuality.COMPLETE,
    JdStageTwoOutputCoverage? Coverage = null,
    IReadOnlyList<string>? WarningCodes = null);

public sealed record JdFitScoreCalculation(decimal FinalScore, string JsonString);


/// <summary>Owns Pool A/B, critical-gap, credibility and KSW decisions.</summary>
public sealed class JdFitScoreCalculator
{
    private const decimal PoolAMaximum = 70m;
    private const decimal PoolBMaximum = 30m;

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
                .Where(item => response.ItemScores.ContainsKey(item.ItemId))
                .Select(item => new ItemCalculation(item, response.ItemScores[item.ItemId]))
                .ToList();
            if (itemScores.Count == 0)
            {
                continue;
            }
            var selectedItems = SelectScoredItems(group, itemScores);
            var groupScore = WeightedAverage(selectedItems);
            var groupWeight = group.Items.Average(item => item.CategoryWeight);
            var criticalGap = response.Quality == JdStageTwoOutputQuality.COMPLETE &&
                group.Importance == "must_have" && groupScore == 0m;
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

        var poolA = poolAMax == 0m
            ? response.Quality == JdStageTwoOutputQuality.COMPLETE ? PoolAMaximum : 0m
            : poolAActual / poolAMax * PoolAMaximum;
        var poolB = poolBMax == 0m
            ? response.Quality == JdStageTwoOutputQuality.COMPLETE ? PoolBMaximum : 0m
            : poolBActual / poolBMax * PoolBMaximum;
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
        var ksw01Triggered = response.Quality == JdStageTwoOutputQuality.COMPLETE &&
            coreTechnicalGroups.Count > 0 &&
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

        // Penalties are backend decisions. The model may return evidence, but a
        // model-controlled `triggered` flag must never deduct points by itself.
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

        var finalScore = Math.Clamp(poolA + poolB, 0m, 100m);
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
            contract = JdFitResultContract.Current,
            sourceJdSchemaVersion = projection.SourceSchemaVersion,
            stageTwoAnalysis = new
            {
                quality = response.Quality.ToString(),
                scoreBasis = response.Quality == JdStageTwoOutputQuality.COMPLETE
                    ? "complete_requirement_scores"
                    : "accepted_requirement_scores_only",
                coverage = response.Coverage == null ? null : new
                {
                    expectedScoreCount = response.Coverage.ExpectedScoreCount,
                    inputScoreCount = response.Coverage.InputScoreCount,
                    acceptedScoreCount = response.Coverage.AcceptedScoreCount,
                    discardedScoreCount = response.Coverage.DiscardedScoreCount,
                    missingScoreCount = response.Coverage.MissingScoreCount,
                    wasTruncated = response.Coverage.WasTruncated
                },
                warningCodes = response.WarningCodes ?? Array.Empty<string>()
            },
            jdAnalysis = new
            {
                quality = projection.AnalysisQuality,
                scoreBasis = projection.RequirementSetComplete
                    ? "complete_requirement_set"
                    : "accepted_requirements_only",
                requirementSetComplete = projection.RequirementSetComplete,
                coverage = projection.Coverage,
                warningCodes = projection.WarningCodes ?? Array.Empty<string>()
            },
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
                narrative = response.Quality == JdStageTwoOutputQuality.PARTIAL
                    ? BuildPartialNarrative(response)
                    : response.Narrative
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
            "at_least_n" => items.OrderByDescending(item => item.Score.HandlerScore).ThenBy(item => item.Item.ItemId, StringComparer.Ordinal).Take(Math.Min(group.MinSatisfied, items.Count)).ToList(),
            _ => throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis)
        };

    private static string BuildPartialNarrative(JdStageTwoValidatedResponse response)
    {
        var accepted = response.Coverage?.AcceptedScoreCount ?? response.ItemScores.Count;
        var expected = response.Coverage?.ExpectedScoreCount ?? response.ItemScores.Count;
        var notice = $"Partial matching result based on {accepted} of {expected} requirements.";
        return string.IsNullOrWhiteSpace(response.Narrative)
            ? notice
            : $"{notice} {response.Narrative}";
    }

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
