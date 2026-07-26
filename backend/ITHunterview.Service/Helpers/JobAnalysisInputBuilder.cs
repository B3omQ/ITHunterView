using System;
using System.Collections.Generic;
using System.Linq;
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

            var domains = job.JobDomain != null && job.JobDomain.Count > 0
                ? job.JobDomain.Where(d => !string.IsNullOrWhiteSpace(d))
                               .Select(d => NormalizeString(d))
                               .Distinct(StringComparer.OrdinalIgnoreCase)
                               .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                               .ToList()
                : null;

            return new JobAnalysisInputSnapshot
            {
                Title = NormalizeString(job.Title),
                Description = NormalizeString(job.Description),
                Requirements = NormalizeString(job.Requirements),
                Level = NormalizeNullableString(job.Level),
                WorkingModel = NormalizeNullableString(job.WorkingModel),
                JobExpertise = NormalizeNullableString(job.JobExpertise),
                JobDomain = domains
            };
        }

        public string ComputeHash(JobAnalysisInputSnapshot snapshot, Guid systemPromptVersionId, Guid userPromptVersionId, string schemaVersion = "jd-analysis/v2")
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            string canonicalJson = JsonSerializer.Serialize(snapshot, JsonOptions);
            string rawPayload = $"{schemaVersion}:{systemPromptVersionId}:{userPromptVersionId}:{canonicalJson}";

            byte[] bytes = Encoding.UTF8.GetBytes(rawPayload);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        private static string NormalizeString(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string normalizedLines = value.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
            return normalizedLines.Normalize(NormalizationForm.FormKC);
        }

        private static string? NormalizeNullableString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return NormalizeString(value);
        }
    }
}
