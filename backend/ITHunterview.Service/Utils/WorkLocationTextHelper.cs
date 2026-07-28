using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ITHunterview.Service.Utils
{
    public static class WorkLocationTextHelper
    {
        private class WorkLocationJsonModel
        {
            public int version { get; set; }
            public string? workLocation { get; set; }
            public string? workingHours { get; set; }
            public string? howToApply { get; set; }
        }

        public static string FormatForAi(string? rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            var trimmed = rawText.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var parsed = JsonSerializer.Deserialize<WorkLocationJsonModel>(trimmed, options);
                    if (parsed != null && parsed.version >= 1)
                    {
                        var sb = new StringBuilder();
                        if (!string.IsNullOrWhiteSpace(parsed.workLocation))
                            sb.AppendLine($"Work Location: {parsed.workLocation.Trim()}");

                        if (!string.IsNullOrWhiteSpace(parsed.workingHours))
                            sb.AppendLine($"Working Hours: {parsed.workingHours.Trim()}");

                        if (!string.IsNullOrWhiteSpace(parsed.howToApply))
                            sb.AppendLine($"How to Apply: {parsed.howToApply.Trim()}");

                        var result = sb.ToString().TrimEnd();
                        if (!string.IsNullOrWhiteSpace(result))
                            return result;
                    }
                }
                catch
                {
                    // Fallback to legacy raw text if JSON parsing fails
                }
            }

            var legacyFormatted = FormatLegacyText(trimmed);
            if (!string.IsNullOrWhiteSpace(legacyFormatted))
                return legacyFormatted;

            return $"Work Location: {trimmed}";
        }

        private static string FormatLegacyText(string text)
        {
            var lines = text.Split('\n');
            var locationLines = new List<string>();
            var hoursLines = new List<string>();
            var applyLines = new List<string>();

            string currentSection = "location";
            bool foundHeaders = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var lower = line.ToLowerInvariant().Replace(":", "").Replace("：", "").Trim();

                if (lower == "địa điểm và thời gian" || lower == "địa điểm & thời gian" || lower == "thông tin làm việc")
                {
                    foundHeaders = true;
                    continue;
                }
                if (lower == "địa điểm làm việc" || lower == "địa điểm" || lower == "work location")
                {
                    currentSection = "location";
                    foundHeaders = true;
                    continue;
                }
                if (lower == "thời gian làm việc" || lower == "thời gian" || lower == "working hours" || lower == "working hour")
                {
                    currentSection = "hours";
                    foundHeaders = true;
                    continue;
                }
                if (lower == "cách thức ứng tuyển" || lower == "hướng dẫn ứng tuyển" || lower == "how to apply")
                {
                    currentSection = "apply";
                    foundHeaders = true;
                    continue;
                }

                if (currentSection == "location") locationLines.Add(line);
                else if (currentSection == "hours") hoursLines.Add(line);
                else if (currentSection == "apply") applyLines.Add(line);
            }

            if (!foundHeaders) return string.Empty;

            var sb = new StringBuilder();
            if (locationLines.Count > 0)
                sb.AppendLine($"Work Location: {string.Join(" ", locationLines)}");
            if (hoursLines.Count > 0)
                sb.AppendLine($"Working Hours: {string.Join(" ", hoursLines)}");
            if (applyLines.Count > 0)
                sb.AppendLine($"How to Apply: {string.Join(" ", applyLines)}");

            return sb.ToString().TrimEnd();
        }
    }
}
