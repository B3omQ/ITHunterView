using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Service.Matching;

public sealed class JdRequirementProjector : IJdRequirementProjector
{
    public const string InvalidEffectiveJdAnalysis = "INVALID_EFFECTIVE_JD_ANALYSIS";

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
    {
        "tech_skill", "experience", "seniority_fit", "domain_knowledge", "language", "education", "soft_skill"
    };

    private static readonly HashSet<string> AllowedImportances = new(StringComparer.Ordinal)
    {
        "must_have", "nice_to_have"
    };

    private static readonly HashSet<string> AllowedOperators = new(StringComparer.Ordinal)
    {
        "all_of", "one_of", "at_least_n"
    };

    public JdRequirementProjection Project(string? effectiveJdJson)
    {
        if (string.IsNullOrWhiteSpace(effectiveJdJson))
        {
            throw Invalid();
        }

        try
        {
            using var document = JsonDocument.Parse(effectiveJdJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("matching_metrics", out var metrics) ||
                metrics.ValueKind != JsonValueKind.Object)
            {
                throw Invalid();
            }

            var sourceSchemaVersion = ReadOptionalString(root, "schema_version") ?? "legacy";
            var quality = ReadQuality(root);
            var coverage = JdAnalysisMetadataReader.ReadCoverage(effectiveJdJson);
            var diagnostics = JdAnalysisMetadataReader.ReadDiagnostics(effectiveJdJson);
            if (string.Equals(sourceSchemaVersion, "jd-analysis/v3", StringComparison.Ordinal))
            {
                return new JdRequirementProjection(sourceSchemaVersion, ReadV3Groups(metrics), false, quality, coverage, diagnostics);
            }

            return new JdRequirementProjection(sourceSchemaVersion, ReadLegacyGroups(metrics), true, quality, coverage, diagnostics);
        }
        catch (JsonException)
        {
            throw Invalid();
        }
    }

    private static IReadOnlyList<ProjectedJdRequirementGroup> ReadV3Groups(JsonElement metrics)
    {
        if (!metrics.TryGetProperty("requirement_groups", out var groupArray) || groupArray.ValueKind != JsonValueKind.Array)
        {
            throw Invalid();
        }

        var groups = new List<ProjectedJdRequirementGroup>();
        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var groupElement in groupArray.EnumerateArray())
        {
            if (groupElement.ValueKind != JsonValueKind.Object)
            {
                throw Invalid();
            }

            var groupId = RequiredString(groupElement, "group_id");
            var @operator = RequiredString(groupElement, "operator").ToLowerInvariant();
            var importance = RequiredString(groupElement, "importance").ToLowerInvariant();
            if (!groupIds.Add(groupId) || !AllowedOperators.Contains(@operator) || !AllowedImportances.Contains(importance) ||
                !groupElement.TryGetProperty("items", out var itemArray) || itemArray.ValueKind != JsonValueKind.Array || itemArray.GetArrayLength() == 0)
            {
                throw Invalid();
            }

            var minSatisfied = RequiredInt(groupElement, "min_satisfied");
            ValidateCardinality(@operator, minSatisfied, itemArray.GetArrayLength());

            var items = new List<ProjectedJdRequirementItem>();
            var itemKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var itemElement in itemArray.EnumerateArray())
            {
                var item = ReadItem(itemElement, string.Empty);
                if (!itemKeys.Add($"{item.Category}|{item.SkillName}|{item.MinYears}|{item.MaxYears}"))
                {
                    throw Invalid();
                }
                items.Add(item with
                {
                    ItemId = $"{groupId}:{JdRequirementSemanticNormalizer.CreateItemToken(item.Category, item.SkillName, item.MinYears, item.MaxYears)}"
                });
            }

            groups.Add(new ProjectedJdRequirementGroup(
                groupId,
                @operator,
                minSatisfied,
                importance,
                items,
                ReadOptionalString(groupElement, "source_section") ?? string.Empty,
                ReadOptionalString(groupElement, "requirement_verbatim") ?? string.Empty));
        }

        return groups;
    }

    private static IReadOnlyList<ProjectedJdRequirementGroup> ReadLegacyGroups(JsonElement metrics)
    {
        if (!metrics.TryGetProperty("requirements_list", out var requirementArray) || requirementArray.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ProjectedJdRequirementGroup>();
        }

        var groups = new List<ProjectedJdRequirementGroup>();
        var index = 0;
        foreach (var requirementElement in requirementArray.EnumerateArray())
        {
            index++;
            var groupId = $"legacy-{index:000}";
            var item = ReadItem(requirementElement, $"{groupId}:item-001");
            var importance = RequiredString(requirementElement, "importance").ToLowerInvariant();
            if (!AllowedImportances.Contains(importance))
            {
                throw Invalid();
            }
            groups.Add(new ProjectedJdRequirementGroup(groupId, "all_of", 1, importance, new[] { item }));
        }

        return groups;
    }

    private static ProjectedJdRequirementItem ReadItem(JsonElement element, string itemId)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Invalid();
        }

        var category = RequiredString(element, "category").ToLowerInvariant();
        if (!AllowedCategories.Contains(category))
        {
            throw Invalid();
        }

        var skillName = RequiredString(element, "skill_name");
        var detail = ReadOptionalString(element, "detail_verbatim") ?? string.Empty;
        var rawMention = ReadOptionalString(element, "raw_mention") ?? string.Empty;
        var sourceSection = ReadOptionalString(element, "source_section") ?? string.Empty;
        var evidences = ReadEvidence(element);
        var minYears = ReadOptionalNonNegativeInt(element, "min_years");
        var maxYears = ReadOptionalNonNegativeInt(element, "max_years");
        if (minYears.HasValue && maxYears.HasValue && minYears > maxYears)
        {
            throw Invalid();
        }

        return new ProjectedJdRequirementItem(
            itemId,
            category,
            skillName,
            detail,
            rawMention,
            sourceSection,
            evidences,
            minYears,
            maxYears,
            JdRequirementCategoryWeights.Get(category));
    }

    private static IReadOnlyList<string> ReadEvidence(JsonElement element)
    {
        var values = new List<string>();
        if (element.TryGetProperty("evidences", out var evidences) && evidences.ValueKind == JsonValueKind.Array)
        {
            values.AddRange(evidences.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString()?.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))!
                .Select(value => value!));
        }
        else if (ReadOptionalString(element, "evidence") is { Length: > 0 } evidence)
        {
            values.Add(evidence);
        }

        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void ValidateCardinality(string @operator, int minSatisfied, int itemCount)
    {
        var isValid = @operator switch
        {
            "all_of" => minSatisfied == itemCount,
            "one_of" => minSatisfied == 1,
            "at_least_n" => minSatisfied >= 1 && minSatisfied <= itemCount,
            _ => false
        };
        if (!isValid)
        {
            throw Invalid();
        }
    }

    private static string RequiredString(JsonElement element, string property) =>
        ReadOptionalString(element, property) is { Length: > 0 } value ? value : throw Invalid();

    private static string? ReadOptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    private static int RequiredInt(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
        {
            throw Invalid();
        }
        return result;
    }

    private static int? ReadOptionalNonNegativeInt(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result) || result < 0)
        {
            throw Invalid();
        }
        return result;
    }

    private static JdAnalysisQuality ReadQuality(JsonElement root)
    {
        var value = ReadOptionalString(root, "analysis_quality");
        if (value is null)
        {
            // Historical structured analyses did not carry three-state metadata.
            // Their structural validity is the only available signal, so preserve
            // the established COMPLETE default for those legacy payloads.
            return JdAnalysisQuality.COMPLETE;
        }

        return Enum.TryParse<JdAnalysisQuality>(value, ignoreCase: false, out var quality)
            ? quality
            : throw Invalid();
    }

    private static InvalidOperationException Invalid() => new(InvalidEffectiveJdAnalysis);
}
