using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

internal static class JdMatchCriticalGapEnricher
{
    private const int MaximumTextLength = 4_000;
    private const int MaximumEvidenceCount = 50;

    internal static void Enrich(MatchReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var groupsById = report.RequirementGroups
            .Where(group => !string.IsNullOrWhiteSpace(group.GroupId))
            .GroupBy(group => group.GroupId!, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);

        foreach (var gap in report.CriticalGaps)
        {
            groupsById.TryGetValue(gap.GroupId ?? string.Empty, out var group);
            EnrichGap(gap, group);
            var stableGapId = BuildStableGapId(gap);
            if (!string.IsNullOrWhiteSpace(stableGapId))
            {
                // Older reports sometimes reused one coarse group ID for several
                // item gaps. Rebuild the ID from the structural identity before
                // deduplicating so distinct requirements cannot be discarded.
                gap.GapId = stableGapId;
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        report.CriticalGaps = report.CriticalGaps
            .Where(gap => string.IsNullOrWhiteSpace(gap.GapId) || seen.Add(gap.GapId))
            .ToList();
    }

    internal static string BuildStableGapId(MatchCriticalGapReportDto gap)
    {
        ArgumentNullException.ThrowIfNull(gap);
        var code = string.IsNullOrWhiteSpace(gap.Code) ? "CRITICAL_GAP" : gap.Code;
        if (string.Equals(gap.Scope, "item", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(gap.GroupId) &&
            !string.IsNullOrWhiteSpace(gap.ItemId))
        {
            return $"{code}:item:{gap.GroupId}:{gap.ItemId}";
        }

        if (string.Equals(gap.Scope, "group", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(gap.GroupId) &&
            gap.AffectedItemIds.Count > 0)
        {
            return $"{code}:group:{gap.GroupId}:{string.Join(',', gap.AffectedItemIds)}";
        }

        return string.Empty;
    }

    private static void EnrichGap(MatchCriticalGapReportDto gap, MatchRequirementGroupReportDto? group)
    {
        if (group is null)
        {
            return;
        }

        FillGroupFields(gap, group);
        if (string.Equals(gap.Scope, "item", StringComparison.Ordinal))
        {
            var matchingItems = group.Items
                .Where(item => !string.IsNullOrWhiteSpace(item.ItemId) &&
                               string.Equals(item.ItemId, gap.ItemId, StringComparison.Ordinal))
                .ToArray();
            if (matchingItems.Length == 1)
            {
                FillItemFields(gap, matchingItems[0], group.RequirementVerbatim);
            }
            return;
        }

        if (!string.Equals(gap.Scope, "group", StringComparison.Ordinal) || gap.AffectedItemIds.Count == 0)
        {
            return;
        }

        var requestedIds = gap.AffectedItemIds.ToHashSet(StringComparer.Ordinal);
        var affectedItems = group.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.ItemId) && requestedIds.Contains(item.ItemId!))
            .ToArray();
        if (affectedItems.Length != requestedIds.Count ||
            affectedItems.Select(item => item.ItemId!).Distinct(StringComparer.Ordinal).Count() != requestedIds.Count)
        {
            return;
        }

        gap.AffectedItemIds = affectedItems.Select(item => item.ItemId!).ToList();
        if (string.IsNullOrWhiteSpace(gap.Requirement))
        {
            gap.Requirement = Bound(string.Join(" | ", affectedItems.Select(item => ItemLabel(item, group.RequirementVerbatim))));
        }

        if (string.IsNullOrWhiteSpace(gap.Category))
        {
            var categories = affectedItems
                .Select(item => item.Category)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            gap.Category = categories.Length == 1 ? categories[0] : null;
        }

        if (string.IsNullOrWhiteSpace(gap.Reasoning))
        {
            gap.Reasoning = Bound(string.Join(" ", affectedItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Reasoning))
                .Select(item => $"{ItemLabel(item, group.RequirementVerbatim)}: {item.Reasoning.Trim()}")));
        }

        if (gap.Evidence.Count == 0)
        {
            gap.Evidence = affectedItems
                .SelectMany(item => item.Evidence)
                .Where(evidence => !string.IsNullOrWhiteSpace(evidence.Quotation))
                .DistinctBy(evidence => (evidence.Quotation, evidence.Section))
                .Take(MaximumEvidenceCount)
                .Select(CloneEvidence)
                .ToList();
        }
    }

    private static void FillGroupFields(MatchCriticalGapReportDto gap, MatchRequirementGroupReportDto group)
    {
        gap.SourceRequirementId ??= group.SourceRequirementId;
        gap.SourceSection ??= group.SourceSection;
        gap.Importance ??= group.Importance;
        if (string.IsNullOrWhiteSpace(gap.RequirementVerbatim))
        {
            gap.RequirementVerbatim = Bound(group.RequirementVerbatim);
        }
    }

    private static void FillItemFields(
        MatchCriticalGapReportDto gap,
        MatchRequirementItemReportDto item,
        string? groupVerbatim)
    {
        if (string.IsNullOrWhiteSpace(gap.Requirement))
        {
            gap.Requirement = ItemLabel(item, groupVerbatim);
        }
        gap.Category ??= item.Category;
        if (string.IsNullOrWhiteSpace(gap.Reasoning))
        {
            gap.Reasoning = Bound(item.Reasoning);
        }
        if (gap.Evidence.Count == 0)
        {
            gap.Evidence = item.Evidence
                .Where(evidence => !string.IsNullOrWhiteSpace(evidence.Quotation))
                .Take(MaximumEvidenceCount)
                .Select(CloneEvidence)
                .ToList();
        }
    }

    private static string ItemLabel(MatchRequirementItemReportDto item, string? groupVerbatim)
    {
        if (!string.IsNullOrWhiteSpace(item.NormalizedText)) return Bound(item.NormalizedText);
        if (!string.IsNullOrWhiteSpace(item.RawMention)) return Bound(item.RawMention);
        return Bound(groupVerbatim);
    }

    private static MatchEvidenceReportDto CloneEvidence(MatchEvidenceReportDto evidence) => new()
    {
        Quotation = Bound(evidence.Quotation),
        Section = string.IsNullOrWhiteSpace(evidence.Section) ? null : Bound(evidence.Section)
    };

    private static string Bound(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= MaximumTextLength ? normalized : normalized[..MaximumTextLength];
    }
}
