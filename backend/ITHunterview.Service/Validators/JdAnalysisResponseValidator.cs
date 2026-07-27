using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Service.Helpers;

namespace ITHunterview.Service.Validators
{
    public sealed class ValidatedJobAnalysis
    {
        public string SchemaVersion { get; set; } = "jd-analysis/v2";
        public List<string> JobTitlesNormalized { get; set; } = new();
        public List<ValidatedSkillMention> SkillsNormalized { get; set; } = new();
        public int TotalYearsExp { get; set; }
        public List<string> Domains { get; set; } = new();
        public List<ValidatedRequirementItem> RequirementsList { get; set; } = new();
    }

        public sealed class ValidatedSkillMention
        {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "tech_skill";
        public string RawMention { get; set; } = string.Empty;
        public string SourceSection { get; set; } = "requirements";
        public string Evidence { get; set; } = string.Empty;
            public decimal Confidence { get; set; } = 1.0m;
            public string Importance { get; set; } = "nice_to_have";
    }

    public sealed class ValidatedRequirementItem
    {
        public string Category { get; set; } = string.Empty;
        public string Importance { get; set; } = "must_have";
        public string SkillName { get; set; } = string.Empty;
        public string DetailVerbatim { get; set; } = string.Empty;
        public string RawMention { get; set; } = string.Empty;
        public string SourceSection { get; set; } = "requirements";
        public string Evidence { get; set; } = string.Empty;
        public decimal Confidence { get; set; } = 1.0m;
    }

    public sealed class ValidationResult<T>
    {
        public bool IsValid { get; set; }
        public string? FailureCode { get; set; }
        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }
    }

    public interface IJdAnalysisResponseValidator
    {
        ValidationResult<ValidatedJobAnalysis> Validate(string providerOutput, JobAnalysisInputSnapshot input);
    }

        public class JdAnalysisResponseValidator : IJdAnalysisResponseValidator
        {
        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "tech_skill", "experience", "domain_knowledge", "language", "education", "soft_skill"
        };

            private static readonly HashSet<string> AllowedImportances = new(StringComparer.OrdinalIgnoreCase)
        {
            "must_have", "nice_to_have"
            };

            private static readonly HashSet<string> AllowedSourceSections = new(StringComparer.OrdinalIgnoreCase)
            {
                "title", "description", "requirements"
            };

            private static readonly HashSet<string> SkillCandidateCategories = new(StringComparer.OrdinalIgnoreCase)
            {
                "tech_skill", "domain_knowledge", "language"
            };

        public ValidationResult<ValidatedJobAnalysis> Validate(string providerOutput, JobAnalysisInputSnapshot input)
        {
            var result = new ValidationResult<ValidatedJobAnalysis>();
            if (string.IsNullOrWhiteSpace(providerOutput))
            {
                result.IsValid = false;
                result.FailureCode = "EMPTY_MODEL_OUTPUT";
                result.Errors.Add("AI provider returned empty response.");
                return result;
            }

            string jsonContent = ExtractJsonPayload(providerOutput);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.IsValid = false;
                result.FailureCode = "INVALID_JSON_FORMAT";
                result.Errors.Add("Could not find a valid JSON object in AI provider output.");
                return result;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(jsonContent);
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.FailureCode = "INVALID_JSON_FORMAT";
                result.Errors.Add($"JSON parsing failed: {ex.Message}");
                return result;
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    result.IsValid = false;
                    result.FailureCode = "INVALID_JSON_ROOT";
                    result.Errors.Add("Root element must be a JSON object.");
                    return result;
                }

                if (!root.TryGetProperty("schema_version", out var schemaProp) ||
                    schemaProp.GetString() != "jd-analysis/v2")
                {
                    result.IsValid = false;
                    result.FailureCode = "SCHEMA_VERSION_MISMATCH";
                    result.Errors.Add("Missing or invalid schema_version. Expected 'jd-analysis/v2'.");
                    return result;
                }

                if (!root.TryGetProperty("matching_metrics", out var metricsProp) ||
                    metricsProp.ValueKind != JsonValueKind.Object)
                {
                    result.IsValid = false;
                    result.FailureCode = "MISSING_MATCHING_METRICS";
                    result.Errors.Add("Missing 'matching_metrics' object.");
                    return result;
                }

                var validated = new ValidatedJobAnalysis();
                string fullInputText = CombineInputText(input);

                if (!TryGetRequiredArray(metricsProp, "job_titles_normalized", result, out var titlesProp) ||
                    !TryGetRequiredArray(metricsProp, "skills_normalized", result, out var skillsProp) ||
                    !TryGetRequiredArray(metricsProp, "domains", result, out var domProp) ||
                    !TryGetRequiredArray(metricsProp, "requirements_list", result, out var reqsProp) ||
                    !metricsProp.TryGetProperty("total_years_exp", out var expProp) ||
                    expProp.ValueKind != JsonValueKind.Number || !expProp.TryGetInt32(out var years))
                {
                    result.IsValid = false;
                    result.FailureCode ??= "INVALID_MATCHING_METRICS";
                    if (!metricsProp.TryGetProperty("total_years_exp", out _)) result.Errors.Add("Missing required numeric field 'total_years_exp'.");
                    else if (result.Errors.Count == 0) result.Errors.Add("'total_years_exp' must be an integer.");
                    return result;
                }

                foreach (var titleElem in titlesProp.EnumerateArray())
                {
                    if (titleElem.ValueKind != JsonValueKind.String)
                    {
                        return Invalid(result, "INVALID_TITLE", "Every job title must be a string.");
                    }
                    AddCanonical(validated.JobTitlesNormalized, titleElem.GetString());
                }
                validated.TotalYearsExp = Math.Max(0, years);

                foreach (var dElem in domProp.EnumerateArray())
                {
                    if (dElem.ValueKind != JsonValueKind.String)
                    {
                        return Invalid(result, "INVALID_DOMAIN", "Every domain must be a string.");
                    }
                    AddCanonical(validated.Domains, dElem.GetString());
                }

                // The model must provide a structurally valid skill projection, but
                // requirements_list is canonical to prevent two divergent AI lists.
                foreach (var sElem in skillsProp.EnumerateArray())
                {
                    if (sElem.ValueKind != JsonValueKind.Object) return Invalid(result, "INVALID_SKILL", "Every skill must be an object.");

                    string category = ReadString(sElem, "category");
                    string name = ReadString(sElem, "name");
                    string rawMention = ReadString(sElem, "raw_mention");
                    string section = ReadString(sElem, "source_section");
                    string evidence = ReadString(sElem, "evidence");
                    if (!SkillCandidateCategories.Contains(category) ||
                        !AllowedSourceSections.Contains(section) ||
                        string.IsNullOrWhiteSpace(name) ||
                        string.IsNullOrWhiteSpace(rawMention) ||
                        string.IsNullOrWhiteSpace(evidence))
                    {
                        return Invalid(result, "INVALID_SKILL", "Every skill must use a skill category and include name, raw_mention, source_section, and evidence.");
                    }
                    if (!IsEvidencePresent(evidence, fullInputText))
                    {
                        return Invalid(result, "EVIDENCE_NOT_IN_INPUT", $"Evidence for skill '{name}' is not present verbatim in the job input.");
                    }
                }

                foreach (var rElem in reqsProp.EnumerateArray())
                {
                    if (validated.RequirementsList.Count >= 50) break;
                    if (rElem.ValueKind != JsonValueKind.Object) return Invalid(result, "INVALID_REQUIREMENT", "Every requirement must be an object.");

                    string category = ReadString(rElem, "category");
                    string importance = ReadString(rElem, "importance");
                    string skillName = ReadString(rElem, "skill_name");
                    string detail = ReadString(rElem, "detail_verbatim");
                    string rawMention = ReadString(rElem, "raw_mention");
                    string section = ReadString(rElem, "source_section");
                    string evidence = ReadString(rElem, "evidence");
                    decimal confidence = ReadConfidence(rElem);

                    if (!AllowedCategories.Contains(category) || !AllowedImportances.Contains(importance) || !AllowedSourceSections.Contains(section))
                    {
                        return Invalid(result, "INVALID_REQUIREMENT_ENUM", "Requirement category, importance, or source_section is invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(skillName) || string.IsNullOrWhiteSpace(rawMention) || string.IsNullOrWhiteSpace(detail) || string.IsNullOrWhiteSpace(evidence))
                    {
                        return Invalid(result, "MISSING_REQUIREMENT_EVIDENCE", "Every requirement needs skill_name, raw_mention, detail_verbatim, and evidence.");
                    }
                    if (!IsEvidencePresent(evidence, fullInputText) || !IsEvidencePresent(detail, fullInputText))
                    {
                        return Invalid(result, "EVIDENCE_NOT_IN_INPUT", $"Evidence for requirement '{skillName}' is not present verbatim in the job input.");
                    }

                    validated.RequirementsList.Add(new ValidatedRequirementItem
                    {
                        Category = category.ToLowerInvariant(),
                        Importance = importance.ToLowerInvariant(),
                        SkillName = NormalizeToken(skillName),
                        DetailVerbatim = detail.Trim(),
                        RawMention = rawMention.Trim(),
                        SourceSection = section.ToLowerInvariant(),
                        Evidence = evidence.Trim(),
                        Confidence = confidence
                    });
                }

                Canonicalize(validated);

                result.IsValid = true;
                result.Data = validated;
                return result;
            }
        }

        private static bool TryGetRequiredArray(JsonElement metrics, string property, ValidationResult<ValidatedJobAnalysis> result, out JsonElement value)
        {
            value = default;
            if (metrics.TryGetProperty(property, out value) && value.ValueKind == JsonValueKind.Array) return true;
            result.FailureCode = "MISSING_REQUIRED_ARRAY";
            result.Errors.Add($"Missing required array '{property}'.");
            return false;
        }

        private static ValidationResult<ValidatedJobAnalysis> Invalid(ValidationResult<ValidatedJobAnalysis> result, string code, string message)
        {
            result.IsValid = false;
            result.FailureCode = code;
            result.Errors.Add(message);
            return result;
        }

        private static string ReadString(JsonElement item, string property) =>
            item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim() ?? string.Empty
                : string.Empty;

        private static decimal ReadConfidence(JsonElement item)
        {
            return item.TryGetProperty("confidence", out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var confidence)
                ? Math.Clamp(confidence, 0m, 1m)
                : 0.95m;
        }

        private static void AddCanonical(List<string> values, string? value)
        {
            var normalized = NormalizeToken(value);
            if (!string.IsNullOrWhiteSpace(normalized)) values.Add(normalized);
        }

        private static string NormalizeToken(string? value) => NormalizeWhitespace(value ?? string.Empty).ToLowerInvariant();

        private static void Canonicalize(ValidatedJobAnalysis validated)
        {
            validated.JobTitlesNormalized = validated.JobTitlesNormalized.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToList();
            validated.Domains = validated.Domains.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToList();

            validated.RequirementsList = validated.RequirementsList
                .GroupBy(r => $"{r.Category}|{r.Importance}|{r.SkillName}|{r.Evidence}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(r => r.SourceSection, StringComparer.Ordinal).ThenBy(r => r.DetailVerbatim, StringComparer.Ordinal).First())
                .OrderBy(r => r.SourceSection, StringComparer.Ordinal)
                .ThenBy(r => r.Category, StringComparer.Ordinal)
                .ThenBy(r => r.SkillName, StringComparer.Ordinal)
                .ToList();

            validated.SkillsNormalized = validated.RequirementsList
                .Where(r => SkillCandidateCategories.Contains(r.Category))
                .GroupBy(r => $"{r.Category}|{r.SkillName}", StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var selected = g.OrderByDescending(r => r.Importance == "must_have")
                        .ThenBy(r => r.SourceSection, StringComparer.Ordinal)
                        .ThenBy(r => r.Evidence, StringComparer.Ordinal)
                        .First();
                    return new ValidatedSkillMention
                    {
                        Name = selected.SkillName,
                        Category = selected.Category,
                        Importance = selected.Importance,
                        RawMention = selected.RawMention,
                        SourceSection = selected.SourceSection,
                        Evidence = selected.Evidence,
                        Confidence = selected.Confidence
                    };
                })
                .OrderBy(s => s.Category, StringComparer.Ordinal)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .Take(40)
                .ToList();
        }

        private static string ExtractJsonPayload(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            string text = rawText.Trim();
            // Handle markdown code block wrapper ```json ... ```
            if (text.StartsWith("```"))
            {
                int firstNewline = text.IndexOf('\n');
                int lastFence = text.LastIndexOf("```");
                if (firstNewline >= 0 && lastFence > firstNewline)
                {
                    text = text.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                }
            }

            int firstBrace = text.IndexOf('{');
            int lastBrace = text.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return text.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return text;
        }

        private static string CombineInputText(JobAnalysisInputSnapshot input)
        {
            return $"{input.Title} {input.Description} {input.Requirements}".ToLowerInvariant();
        }

        private static bool IsEvidencePresent(string evidence, string fullInputText)
        {
            if (string.IsNullOrWhiteSpace(evidence)) return true;
            string normalizedEvidence = NormalizeWhitespace(evidence).ToLowerInvariant();
            string normalizedInput = NormalizeWhitespace(fullInputText).ToLowerInvariant();
            return normalizedInput.Contains(normalizedEvidence);
        }

        private static string NormalizeWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return string.Join(" ", input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
