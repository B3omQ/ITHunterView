using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Domain.Entities;

namespace ITHunterview.Service.Utils
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
        JobAnalysisInputSnapshot BuildFromPastedText(string? title, string? rawJdText);
        JobAnalysisInputSnapshot BuildFromCanonicalJson(string canonicalJson);
        JobAnalysisInputSnapshot BuildFromSavedSnapshotText(string? title, string? originalText);
        string SerializeCanonical(JobAnalysisInputSnapshot snapshot);
        string ComputeSemanticHash(JobAnalysisInputSnapshot snapshot);
        string ComputeAnalysisHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion);
        string ComputeHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion);
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

        public JobAnalysisInputSnapshot BuildFromPastedText(string? title, string? rawJdText)
        {
            var normalizedRawText = NormalizeAnalysisSourceText(rawJdText);
            var sections = JdSectionSplitter.Split(normalizedRawText);
            return new JobAnalysisInputSnapshot
            {
                Title = NormalizeAnalysisSourceText(title),
                Description = NormalizeAnalysisSourceText(sections.Description),
                Requirements = NormalizeAnalysisSourceText(sections.Requirements)
            };
        }

        public JobAnalysisInputSnapshot BuildFromCanonicalJson(string canonicalJson)
        {
            if (string.IsNullOrWhiteSpace(canonicalJson))
            {
                throw InvalidCanonicalInput();
            }

            try
            {
                using var document = JsonDocument.Parse(canonicalJson, new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 8
                });
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw InvalidCanonicalInput();
                }

                var propertyNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in root.EnumerateObject())
                {
                    if (!propertyNames.Add(property.Name) ||
                        property.Name is not ("title" or "description" or "requirements") ||
                        property.Value.ValueKind != JsonValueKind.String)
                    {
                        throw InvalidCanonicalInput();
                    }
                }

                if (propertyNames.Count != 3 ||
                    !root.TryGetProperty("title", out var title) ||
                    !root.TryGetProperty("description", out var description) ||
                    !root.TryGetProperty("requirements", out var requirements))
                {
                    throw InvalidCanonicalInput();
                }

                return new JobAnalysisInputSnapshot
                {
                    Title = NormalizeAnalysisSourceText(title.GetString()),
                    Description = NormalizeAnalysisSourceText(description.GetString()),
                    Requirements = NormalizeAnalysisSourceText(requirements.GetString())
                };
            }
            catch (JsonException)
            {
                throw InvalidCanonicalInput();
            }
        }

        public JobAnalysisInputSnapshot BuildFromSavedSnapshotText(string? title, string? originalText)
        {
            var parsed = SavedJdSnapshotInputParser.Parse(originalText);
            return new JobAnalysisInputSnapshot
            {
                Title = NormalizeAnalysisSourceText(
                    string.IsNullOrWhiteSpace(title) ? parsed.Title : title),
                Description = NormalizeAnalysisSourceText(
                    parsed.HasRecognizedLabels ? parsed.Description : originalText),
                Requirements = NormalizeAnalysisSourceText(parsed.Requirements)
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

        public string ComputeAnalysisHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion)
        {
            if (string.IsNullOrWhiteSpace(schemaVersion)) throw new ArgumentException("A prompt-pair contract is required.", nameof(schemaVersion));
            string canonicalJson = SerializeCanonical(snapshot);
            string rawPayload = $"{schemaVersion}:{systemPromptVersionId}:{userPromptVersionId}:{canonicalJson}";

            byte[] bytes = Encoding.UTF8.GetBytes(rawPayload);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        public string ComputeHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion)
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

        private static InvalidOperationException InvalidCanonicalInput() =>
            new("INVALID_CANONICAL_JD_INPUT");
    }
}
