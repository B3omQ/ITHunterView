using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ITHunterview.Service.Helpers
{
    /// <summary>
    /// Compatibility reader for persisted job/CV analysis documents.  V1 stored
    /// skill arrays as strings while V2 stores rich skill objects; callers must
    /// consume the canonical names rather than serializing JSON objects.
    /// </summary>
    public sealed class JobAnalysisMetrics
    {
        public List<string> Titles { get; init; } = new();
        public List<string> Skills { get; init; } = new();
        public List<string> Domains { get; init; } = new();
        public int TotalYearsExperience { get; init; }
    }

    public static class JobAnalysisMetricsReader
    {
        public static JobAnalysisMetrics Read(string? analysisJson)
        {
            if (string.IsNullOrWhiteSpace(analysisJson)) return new JobAnalysisMetrics();

            try
            {
                using var document = JsonDocument.Parse(analysisJson);
                if (!TryGetPath(document.RootElement, "matching_metrics", out var metrics) || metrics.ValueKind != JsonValueKind.Object)
                {
                    return new JobAnalysisMetrics();
                }

                return new JobAnalysisMetrics
                {
                    Titles = ReadStringArray(metrics, "job_titles_normalized"),
                    Skills = ReadStringArray(metrics, "skills_normalized"),
                    Domains = ReadStringArray(metrics, "domains"),
                    TotalYearsExperience = metrics.TryGetProperty("total_years_exp", out var years)
                        && years.ValueKind == JsonValueKind.Number
                        && years.TryGetInt32(out var value)
                        ? Math.Max(0, value)
                        : 0
                };
            }
            catch (JsonException)
            {
                return new JobAnalysisMetrics();
            }
        }

        public static List<string> ReadStringArray(JsonElement parent, string propertyName)
        {
            if (!parent.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            return ProjectArrayStrings(array);
        }

        public static List<string> ProjectArrayStrings(JsonElement array)
        {
            if (array.ValueKind != JsonValueKind.Array) return new List<string>();

            var values = new List<string>();
            foreach (var item in array.EnumerateArray())
            {
                string? value = item.ValueKind switch
                {
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Object when item.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String => name.GetString(),
                    JsonValueKind.Object when item.TryGetProperty("skill_name", out var skillName) && skillName.ValueKind == JsonValueKind.String => skillName.GetString(),
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(value)) values.Add(value.Trim());
            }

            return values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryGetPath(JsonElement root, string property, out JsonElement value)
        {
            value = default;
            return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out value);
        }
    }
}
