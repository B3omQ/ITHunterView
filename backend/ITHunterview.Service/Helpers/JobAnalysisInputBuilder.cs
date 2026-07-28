using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Helpers
{
    public sealed class JobAnalysisInputSnapshot
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("requirements")]
        public string Requirements { get; set; } = string.Empty;

        // Legacy snapshots may contain context fields. New snapshots deliberately
        // omit them: V2 analysis requirements are evidenced only by title,
        // description and requirements, so metadata edits must not spend AI credit.
        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("workingModel")]
        public string? WorkingModel { get; set; }

        [JsonPropertyName("jobExpertise")]
        public string? JobExpertise { get; set; }

        [JsonPropertyName("jobDomain")]
        public List<string>? JobDomain { get; set; }
    }

    public interface IJobAnalysisInputBuilder
    {
        JobAnalysisInputSnapshot Build(JobPostings job);
        string SerializeCanonical(JobAnalysisInputSnapshot snapshot);
        string ComputeSemanticHash(JobAnalysisInputSnapshot snapshot);
        string ComputeAnalysisHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion = "jd-analysis/v2");
        string ComputeHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion = "jd-analysis/v2");
    }

    public class JobAnalysisInputBuilder : IJobAnalysisInputBuilder
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public JobAnalysisInputSnapshot Build(JobPostings job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            return new JobAnalysisInputSnapshot
            {
                Title = NormalizeAnalysisSourceText(job.Title),
                Description = NormalizeAnalysisSourceText(JobPostingRichText.ToPlainText(job.Description)),
                Requirements = NormalizeAnalysisSourceText(JobPostingRichText.ToPlainText(job.Requirements))
            };
        }

        public string SerializeCanonical(JobAnalysisInputSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return JsonSerializer.Serialize(snapshot, JsonOptions);
        }

        public string ComputeSemanticHash(JobAnalysisInputSnapshot snapshot)
        {
            string canonicalJson = SerializeCanonical(snapshot);
            byte[] bytes = Encoding.UTF8.GetBytes(canonicalJson);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        public string ComputeAnalysisHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion = "jd-analysis/v2")
        {
            string canonicalJson = SerializeCanonical(snapshot);
            string rawPayload = $"{schemaVersion}:{systemPromptVersionId}:{userPromptVersionId}:{canonicalJson}";

            byte[] bytes = Encoding.UTF8.GetBytes(rawPayload);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        public string ComputeHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion = "jd-analysis/v2")
        {
            return ComputeAnalysisHash(snapshot, systemPromptVersionId, userPromptVersionId, schemaVersion);
        }

        private static string NormalizeAnalysisSourceText(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var normalized = value.Normalize(NormalizationForm.FormKC)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace('\u00A0', ' ')
                .Replace('\t', ' ');

            var lines = new List<string>();
            bool previousWasBlank = false;
            foreach (var rawLine in normalized.Split('\n'))
            {
                var line = CollapseHorizontalWhitespace(rawLine).Trim();
                bool isBlank = line.Length == 0;
                if (isBlank && (previousWasBlank || lines.Count == 0))
                {
                    previousWasBlank = true;
                    continue;
                }

                lines.Add(line);
                previousWasBlank = isBlank;
            }

            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines);
        }

        private static string CollapseHorizontalWhitespace(string value)
        {
            var builder = new StringBuilder(value.Length);
            bool previousWasWhitespace = false;
            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasWhitespace) builder.Append(' ');
                    previousWasWhitespace = true;
                    continue;
                }

                builder.Append(character);
                previousWasWhitespace = false;
            }
            return builder.ToString();
        }
    }
}
