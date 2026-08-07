using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Converts the legacy flat response into the common item-score contract.
/// It performs only transport validation and field mapping.
/// </summary>
public static class LegacyStageTwoResponseAdapter
{
    public static JdStageTwoValidatedResponse Adapt(
        JsonDocument response,
        JdRequirementProjection projection)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(projection);

        var expectedItems = projection.Groups
            .SelectMany(group => group.Items)
            .ToArray();
        var categoryByItem = expectedItems.ToDictionary(item => item.ItemId, item => item.Category, StringComparer.Ordinal);
        LegacyJdStageTwoResponseValidator.Validate(
            response,
            expectedItems.Select(item => item.ItemId).ToArray(),
            categoryByItem);

        var scores = new Dictionary<string, JdStageTwoItemScore>(StringComparer.Ordinal);
        foreach (var element in response.RootElement.GetProperty("scores").EnumerateArray())
        {
            var itemId = element.GetProperty("reqId").GetString()!;
            var score = element.GetProperty("handlerScore").GetDecimal();
            var handlerCode = element.GetProperty("handlerCode").GetString()!;
            scores[itemId] = new JdStageTwoItemScore(
                itemId,
                handlerCode,
                score,
                ReadOptional(element, "reasoning"),
                ReadOptional(element, "confidence", "unknown"),
                ReadStringArray(element, "evidence"));
        }

        var root = response.RootElement;
        var narrative = ReadOptional(root, "narrative");
        var improvements = root.TryGetProperty("improvements", out var improvementElement) &&
                            improvementElement.ValueKind == JsonValueKind.Array
            ? improvementElement.Clone()
            : JsonSerializer.SerializeToElement(Array.Empty<object>());

        return new JdStageTwoValidatedResponse(
            scores,
            narrative,
            improvements,
            Array.Empty<JdStageTwoPenalty>());
    }

    private static string ReadOptional(JsonElement element, string property, string fallback = "")
    {
        return element.TryGetProperty(property, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .Take(5)
            .ToArray();
    }
}
