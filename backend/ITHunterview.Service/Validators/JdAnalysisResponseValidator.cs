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

                // Titles
                if (metricsProp.TryGetProperty("job_titles_normalized", out var titlesProp) &&
                    titlesProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var titleElem in titlesProp.EnumerateArray())
                    {
                        if (titleElem.ValueKind == JsonValueKind.String)
                        {
                            string t = titleElem.GetString()?.Trim() ?? string.Empty;
                            if (!string.IsNullOrEmpty(t)) validated.JobTitlesNormalized.Add(t.ToLowerInvariant());
                        }
                    }
                }

                // Years exp
                if (metricsProp.TryGetProperty("total_years_exp", out var expProp) &&
                    expProp.ValueKind == JsonValueKind.Number && expProp.TryGetInt32(out int years))
                {
                    validated.TotalYearsExp = Math.Max(0, years);
                }

                // Domains
                if (metricsProp.TryGetProperty("domains", out var domProp) &&
                    domProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var dElem in domProp.EnumerateArray())
                    {
                        if (dElem.ValueKind == JsonValueKind.String)
                        {
                            string d = dElem.GetString()?.Trim() ?? string.Empty;
                            if (!string.IsNullOrEmpty(d)) validated.Domains.Add(d.ToLowerInvariant());
                        }
                    }
                }

                // Skills
                if (metricsProp.TryGetProperty("skills_normalized", out var skillsProp) &&
                    skillsProp.ValueKind == JsonValueKind.Array)
                {
                    int skillCount = 0;
                    foreach (var sElem in skillsProp.EnumerateArray())
                    {
                        if (skillCount >= 40) break;
                        if (sElem.ValueKind != JsonValueKind.Object) continue;

                        string name = sElem.TryGetProperty("name", out var np) ? np.GetString()?.Trim() ?? "" : "";
                        string category = sElem.TryGetProperty("category", out var cp) ? cp.GetString()?.Trim() ?? "tech_skill" : "tech_skill";
                        string rawMention = sElem.TryGetProperty("raw_mention", out var rp) ? rp.GetString()?.Trim() ?? "" : "";
                        string section = sElem.TryGetProperty("source_section", out var sp) ? sp.GetString()?.Trim() ?? "requirements" : "requirements";
                        string evidence = sElem.TryGetProperty("evidence", out var ep) ? ep.GetString()?.Trim() ?? "" : "";
                        decimal confidence = 0.95m;

                        if (sElem.TryGetProperty("confidence", out var confP) && confP.ValueKind == JsonValueKind.Number)
                        {
                            if (confP.TryGetDecimal(out var confDec)) confidence = Math.Clamp(confDec, 0.0m, 1.0m);
                        }

                        if (string.IsNullOrWhiteSpace(name)) continue;

                        if (!AllowedCategories.Contains(category))
                        {
                            category = "tech_skill";
                        }

                        if (!string.IsNullOrEmpty(evidence) && !IsEvidencePresent(evidence, fullInputText))
                        {
                            result.Errors.Add($"Evidence for skill '{name}' not found in input text.");
                            // We allow soft fallback or reject depending on severity; if evidence missing, flag error
                        }

                        validated.SkillsNormalized.Add(new ValidatedSkillMention
                        {
                            Name = name.ToLowerInvariant(),
                            Category = category.ToLowerInvariant(),
                            RawMention = string.IsNullOrEmpty(rawMention) ? name : rawMention,
                            SourceSection = section.ToLowerInvariant(),
                            Evidence = evidence,
                            Confidence = confidence
                        });
                        skillCount++;
                    }
                }

                // Requirements list
                if (metricsProp.TryGetProperty("requirements_list", out var reqsProp) &&
                    reqsProp.ValueKind == JsonValueKind.Array)
                {
                    int reqCount = 0;
                    foreach (var rElem in reqsProp.EnumerateArray())
                    {
                        if (reqCount >= 50) break;
                        if (rElem.ValueKind != JsonValueKind.Object) continue;

                        string category = rElem.TryGetProperty("category", out var cp) ? cp.GetString()?.Trim() ?? "tech_skill" : "tech_skill";
                        string importance = rElem.TryGetProperty("importance", out var ip) ? ip.GetString()?.Trim() ?? "must_have" : "must_have";
                        string skillName = rElem.TryGetProperty("skill_name", out var snp) ? snp.GetString()?.Trim() ?? "" : "";
                        string detail = rElem.TryGetProperty("detail_verbatim", out var dp) ? dp.GetString()?.Trim() ?? "" : "";
                        string rawMention = rElem.TryGetProperty("raw_mention", out var rp) ? rp.GetString()?.Trim() ?? "" : "";
                        string section = rElem.TryGetProperty("source_section", out var sp) ? sp.GetString()?.Trim() ?? "requirements" : "requirements";
                        string evidence = rElem.TryGetProperty("evidence", out var ep) ? ep.GetString()?.Trim() ?? "" : "";
                        decimal confidence = 0.95m;

                        if (rElem.TryGetProperty("confidence", out var confP) && confP.ValueKind == JsonValueKind.Number)
                        {
                            if (confP.TryGetDecimal(out var confDec)) confidence = Math.Clamp(confDec, 0.0m, 1.0m);
                        }

                        if (!AllowedCategories.Contains(category)) category = "tech_skill";
                        if (!AllowedImportances.Contains(importance)) importance = "must_have";

                        validated.RequirementsList.Add(new ValidatedRequirementItem
                        {
                            Category = category.ToLowerInvariant(),
                            Importance = importance.ToLowerInvariant(),
                            SkillName = string.IsNullOrEmpty(skillName) ? rawMention : skillName.ToLowerInvariant(),
                            DetailVerbatim = detail,
                            RawMention = string.IsNullOrEmpty(rawMention) ? skillName : rawMention,
                            SourceSection = section.ToLowerInvariant(),
                            Evidence = evidence,
                            Confidence = confidence
                        });
                        reqCount++;
                    }
                }

                result.IsValid = true;
                result.Data = validated;
                return result;
            }
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
