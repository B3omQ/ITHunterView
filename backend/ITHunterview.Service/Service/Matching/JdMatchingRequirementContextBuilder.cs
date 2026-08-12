using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdMatchingRequirementContext(
    string Json,
    int GroupCount,
    int RequirementCount);

/// <summary>
/// Creates the JD context consumed by Stage 2. The input is deliberately
/// item-level so a group containing different categories is representable
/// without changing the approved provider output contract.
/// </summary>
public sealed class JdMatchingRequirementContextBuilder
{
    private const int MaxGroups = 50;
    private const int MaxItems = 100;

    public JdMatchingRequirementContext Build(
        JdRequirementProjection projection,
        IReadOnlySet<string>? includedItemIds = null)
    {
        ArgumentNullException.ThrowIfNull(projection);

        if (projection.Groups.Count is 0 or > MaxGroups || projection.Groups.Any(group => group.Items.Count == 0))
        {
            throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis);
        }

        var entries = new List<object>();
        var itemIds = new HashSet<string>(StringComparer.Ordinal);
        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in projection.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.GroupId) ||
                !groupIds.Add(group.GroupId) ||
                !IsValidGroup(group))
            {
                throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis);
            }

            foreach (var item in group.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId) ||
                    !itemIds.Add(item.ItemId) ||
                    !MatchingScorePolicy.SupportedCategories.Contains(item.Category, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis);
                }

                if (includedItemIds is not null && !includedItemIds.Contains(item.ItemId))
                {
                    continue;
                }

                entries.Add(new
                {
                    ReqId = item.ItemId,
                    GroupId = group.GroupId,
                    NormalizedText = item.SkillName,
                    Category = item.Category,
                    Importance = group.Importance,
                    DetailVerbatim = item.DetailVerbatim,
                    RawMention = item.RawMention,
                    SourceSection = item.SourceSection,
                    RequirementVerbatim = group.RequirementVerbatim,
                    Operator = group.Operator,
                    MinSatisfied = group.MinSatisfied,
                    MinYears = item.MinYears,
                    MaxYears = item.MaxYears,
                    Evidence = item.Evidences
                });
            }
        }

        if (itemIds.Count > MaxItems ||
            includedItemIds is not null && includedItemIds.Any(id => !itemIds.Contains(id)) ||
            entries.Count == 0)
        {
            throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis);
        }

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new JdMatchingRequirementContext(
            json,
            projection.Groups.Count(group => group.Items.Any(item =>
                includedItemIds is null || includedItemIds.Contains(item.ItemId))),
            entries.Count);
    }

    private static bool IsValidGroup(ProjectedJdRequirementGroup group)
    {
        if (group.Importance is not "must_have" and not "nice_to_have")
        {
            return false;
        }

        return group.Operator switch
        {
            "all_of" => group.MinSatisfied == group.Items.Count,
            "one_of" => group.MinSatisfied == 1,
            "at_least_n" => group.MinSatisfied >= 1 && group.MinSatisfied <= group.Items.Count,
            _ => false
        };
    }
}
