using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ITHunterview.Service.Utils
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
        public bool TitleAvailable { get; init; }
        public bool SkillsAvailable { get; init; }
        public bool ExperienceAvailable { get; init; }
        public bool DomainsAvailable { get; init; }
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

                var hasCoverage = TryGetPath(document.RootElement, "analysis_coverage", out var coverage)
                                  && coverage.ValueKind == JsonValueKind.Object;
                return new JobAnalysisMetrics
                {
                    Titles = ReadStringArray(metrics, "job_titles_normalized"),
                    Skills = ReadStringArray(metrics, "skills_normalized"),
                    Domains = ReadStringArray(metrics, "domains"),
                    TotalYearsExperience = metrics.TryGetProperty("total_years_exp", out var years)
                        && years.ValueKind == JsonValueKind.Number
                        && years.TryGetInt32(out var value)
                        ? Math.Max(0, value)
                        : 0,
                    TitleAvailable = ReadAvailability(
                        hasCoverage ? coverage : default,
                        "title_metrics_available",
                        metrics,
                        "job_titles_normalized",
                        JsonValueKind.Array),
                    SkillsAvailable = ReadAvailability(
                        hasCoverage ? coverage : default,
                        "skill_metrics_available",
                        metrics,
                        "skills_normalized",
                        JsonValueKind.Array),
                    ExperienceAvailable = ReadAvailability(
                        hasCoverage ? coverage : default,
                        "experience_metric_available",
                        metrics,
                        "total_years_exp",
                        JsonValueKind.Number),
                    DomainsAvailable = ReadAvailability(
                        hasCoverage ? coverage : default,
                        "domain_metrics_available",
                        metrics,
                        "domains",
                        JsonValueKind.Array)
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

        private static bool ReadAvailability(
            JsonElement coverage,
            string coverageProperty,
            JsonElement metrics,
            string metricProperty,
            JsonValueKind requiredKind)
        {
            if (coverage.ValueKind == JsonValueKind.Object
                && coverage.TryGetProperty(coverageProperty, out var availability)
                && availability.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return availability.GetBoolean();
            }

            return metrics.TryGetProperty(metricProperty, out var metric)
                   && metric.ValueKind == requiredKind;
        }
    }
}
