using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Mechanically maps the approved provider response to the existing item-score
/// model. It does not infer missing values or repair model semantics.
/// </summary>
public sealed class JdMatchingResponseAdapter
{
    public JdStageTwoValidatedResponse Adapt(
        JsonDocument response,
        JdRequirementProjection projection)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(projection);

        JdMatchingResponseValidator.Validate(response, projection);

        var scores = new Dictionary<string, JdStageTwoItemScore>(StringComparer.Ordinal);
        foreach (var element in response.RootElement.GetProperty("scores").EnumerateArray())
        {
            var itemId = element.GetProperty("reqId").GetString()!.Trim();
            scores[itemId] = new JdStageTwoItemScore(
                itemId,
                element.GetProperty("handlerCode").GetString()!.Trim(),
                element.GetProperty("handlerScore").GetDecimal(),
                ReadOptionalString(element, "reasoning"),
                ReadOptionalString(element, "confidence", "unknown"),
                ReadStringArray(element, "evidence"));
        }

        var root = response.RootElement;
        var narrative = ReadOptionalString(root, "narrative");
        var improvements = root.TryGetProperty("improvements", out var improvementElement) &&
                           improvementElement.ValueKind == JsonValueKind.Array
            ? improvementElement.Clone()
            : JsonSerializer.SerializeToElement(Array.Empty<object>());

        return new JdStageTwoValidatedResponse(
            scores,
            narrative,
            improvements,
            JdMatchingResponseValidator.ReadPenalties(root));
    }

    private static string ReadOptionalString(JsonElement element, string property, string fallback = "") =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? fallback
            : fallback;

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }
}
