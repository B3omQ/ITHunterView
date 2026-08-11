using System;
using System.Collections.Generic;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.Utils;

public static class JdMatchMetadataReader
{
    public static JdAnalysisResultDto? Read(string? matchDetails)
    {
        if (string.IsNullOrWhiteSpace(matchDetails)) return null;
        try
        {
            using var document = JsonDocument.Parse(matchDetails);
            if (!TryGetPropertyIgnoreCase(document.RootElement, "jdAnalysis", out var metadata) ||
                metadata.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new JdAnalysisResultDto
            {
                Quality = ReadString(metadata, "quality"),
                ScoreBasis = ReadString(metadata, "scoreBasis"),
                RequirementSetComplete = !TryGetPropertyIgnoreCase(metadata, "requirementSetComplete", out var complete) ||
                                         complete.ValueKind != JsonValueKind.False
            };
            if (TryGetPropertyIgnoreCase(metadata, "coverage", out var coverage) && coverage.ValueKind == JsonValueKind.Object)
            {
                result.Coverage = new JdAnalysisCoverage(
                    ReadInt(coverage, "inputGroupCount"),
                    ReadInt(coverage, "acceptedGroupCount"),
                    ReadInt(coverage, "discardedGroupCount"),
                    ReadInt(coverage, "inputItemCount"),
                    ReadInt(coverage, "acceptedItemCount"),
                    ReadInt(coverage, "discardedItemCount"),
                    TryGetPropertyIgnoreCase(coverage, "requirementSetComplete", out var completeValue) && completeValue.ValueKind == JsonValueKind.True);
            }
            if (TryGetPropertyIgnoreCase(metadata, "warningCodes", out var warnings) &&
                warnings.ValueKind == JsonValueKind.Array)
            {
                foreach (var warning in warnings.EnumerateArray())
                {
                    if (warning.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(warning.GetString()))
                    {
                        result.WarningCodes.Add(warning.GetString()!);
                    }
                    if (result.WarningCodes.Count == 100) break;
                }
            }
            return result;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? ReadQuality(string? matchDetails) => Read(matchDetails)?.Quality;

    private static string ReadString(JsonElement element, string property) =>
        TryGetPropertyIgnoreCase(element, property, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static int ReadInt(JsonElement element, string property) =>
        TryGetPropertyIgnoreCase(element, property, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number)
            ? number
            : 0;

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string property,
        out JsonElement value)
    {
        if (element.TryGetProperty(property, out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, property, StringComparison.OrdinalIgnoreCase))
                {
                    value = candidate.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
