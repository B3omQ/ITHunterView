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
    public JdMatchingRequirementContext Build(JdRequirementProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        if (projection.Groups.Count == 0 || projection.Groups.Any(group => group.Items.Count == 0))
        {
            throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis);
        }

        var entries = new List<object>();
        var itemIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in projection.Groups)
        {
            foreach (var item in group.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId) || !itemIds.Add(item.ItemId))
                {
                    throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis);
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

        if (entries.Count == 0)
        {
            throw new InvalidOperationException(JdRequirementProjector.InvalidEffectiveJdAnalysis);
        }

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new JdMatchingRequirementContext(json, projection.Groups.Count, entries.Count);
    }
}
