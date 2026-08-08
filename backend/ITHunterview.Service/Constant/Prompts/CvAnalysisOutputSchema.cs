using System;
using System.Collections.Generic;
using System.Linq;

namespace ITHunterview.Service.Constant.Prompts;

public sealed record CvAnalysisPromptNormalization(
    string SemanticContent,
    bool RemovedKnownSchema);

public static class CvAnalysisOutputSchema
{
    public const string SchemaVersion = "cv-analysis/v2";
    public const string BeginMarker = "--- BEGIN LOCKED CV ANALYSIS OUTPUT SCHEMA ---";
    public const string EndMarker = "--- END LOCKED CV ANALYSIS OUTPUT SCHEMA ---";

    private const string SchemaInstruction =
        "This output format is managed by the application and overrides conflicting output-format instructions above.";

    private const string LegacyV1SchemaSentence =
        "The JSON MUST have the exact following schema with two main branches (`verbatim_sections` and `matching_metrics`):";

    private const string LockedSchemaJson = """
        {
          "schema_version": "cv-analysis/v2",
          "verbatim_sections": {
            "personal_info": {
              "name": "",
              "title": "",
              "summary": ""
            },
            "education": [
              {
                "institution": "",
                "degree": "",
                "major": "",
                "timeline": ""
              }
            ],
            "languages": [
              {
                "language": "",
                "certifications_or_level": ""
              }
            ],
            "skills_section": [
              "exact skill phrase from a standalone skills section"
            ],
            "professional_experience_and_projects": [
              {
                "company_or_project_name": "",
                "role": "",
                "timeline": "",
                "entry_type": "professional_experience",
                "details_and_responsibilities": [
                  "exact responsibility, achievement, or project bullet"
                ],
                "technologies_used": [
                  "normalized technology name"
                ]
              }
            ],
            "certifications_and_awards": [
              "exact certification or award text"
            ],
            "other_information": ""
          },
          "matching_metrics": {
            "job_titles_normalized": [
              "normalized job title"
            ],
            "skills_normalized": [
              "normalized skill, domain, or human-language name"
            ],
            "total_years_exp": 0,
            "domains": [
              "normalized domain name"
            ]
          },
          "matching_evidence": {
            "requirement_signals": [
              {
                "name": "normalized signal name",
                "category": "tech_skill",
                "evidence_strength": "listed",
                "source_type": "skills_section",
                "source_index": 0,
                "evidence": [
                  "exact verbatim substring from raw_text"
                ]
              }
            ],
            "experience_summary": {
              "total_professional_months": 0,
              "calculation_basis": "insufficient_timeline",
              "periods": [
                {
                  "source_index": 0,
                  "entry_type": "professional_experience",
                  "organization": "",
                  "role": "",
                  "timeline_raw": "",
                  "start_year": null,
                  "start_month": null,
                  "end_year": null,
                  "end_month": null,
                  "is_current": false,
                  "evidence": ""
                }
              ]
            },
            "seniority_signals": [
              {
                "name": "normalized seniority signal",
                "source_type": "professional_experience",
                "source_index": 0,
                "evidence": "exact verbatim substring from raw_text"
              }
            ]
          }
        }
        """;

    public const string LockedBlock = BeginMarker + "\n" +
        SchemaInstruction + "\n\nOUTPUT SCHEMA\n\n" +
        LockedSchemaJson + "\n" + EndMarker;

    private const string HistoricalV1SchemaJson = """
        {
          "verbatim_sections": {
            "personal_info": {
              "name": "",
              "title": "",
              "summary": ""
            },
            "education": [
              {
                "institution": "",
                "degree": "",
                "major": "",
                "timeline": ""
              }
            ],
            "languages": [
              {
                "language": "",
                "certifications_or_level": ""
              }
            ],
            "skills_section": [
              "A list of skills that are ONLY listed in a standalone 'Skills' section. Do not include skills that only appear in project descriptions."
            ],
            "professional_experience_and_projects": [
              {
                "company_or_project_name": "",
                "role": "",
                "timeline": "",
                "details_and_responsibilities": [
                  "Copy verbatim bullet point 1",
                  "Copy verbatim bullet point 2"
                ],
                "technologies_used": ["List of technologies explicitly mentioned within this specific project/role"]
              }
            ],
            "certifications_and_awards": [
              "Award 1", "Cert 2"
            ],
            "other_information": "Any leftover text that doesn't fit above"
          },
          "matching_metrics": {
            "job_titles_normalized": ["Primary job title 1", "Job title 2"],
            "skills_normalized": ["Skill 1", "Skill 2", "Tool 3"],
            "total_years_exp": 0,
            "domains": ["Finance", "E-commerce"]
          }
        }
        """;

    private const string CurrentUserSchemaVersionInstruction =
        "schema_version must be exactly \"cv-analysis/v2\".";

    private const string CurrentUserSchemaHeading =
        "Required top-level structure (all fields are mandatory):";

    private const string CurrentUserSchemaJson = """
        {
          "schema_version": "cv-analysis/v2",
          "verbatim_sections": { ... },
          "matching_metrics": {
            "job_titles_normalized": [],
            "skills_normalized": [],
            "total_years_exp": 0,
            "domains": []
          },
          "matching_evidence": {
            "requirement_signals": [],
            "experience_summary": {
              "total_professional_months": 0,
              "calculation_basis": "insufficient_timeline",
              "periods": []
            },
            "seniority_signals": []
          }
        }
        """;

    public static CvAnalysisPromptNormalization NormalizeManagedContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("CV_ANALYSIS_PROMPT_EMPTY", nameof(content));
        }

        var working = NormalizeLineEndings(content);
        var removedKnownSchema = false;

        working = RemoveManagedBlock(working, ref removedKnownSchema);
        working = RemoveCurrentEmbeddedBlock(working, ref removedKnownSchema);
        working = RemoveCurrentUserEmbeddedBlock(working, ref removedKnownSchema);
        working = RemoveHistoricalV1Block(working, ref removedKnownSchema);

        if (!removedKnownSchema && HasEmbeddedSchemaSignature(working))
        {
            throw SchemaMutation();
        }

        var semanticContent = NormalizeSemanticContent(working);
        if (semanticContent.Length == 0 ||
            semanticContent.Equals("VERBATIM SECTION RULES", StringComparison.Ordinal))
        {
            throw new ArgumentException("CV_ANALYSIS_PROMPT_EMPTY_AFTER_SCHEMA", nameof(content));
        }

        return new CvAnalysisPromptNormalization(semanticContent, removedKnownSchema);
    }

    public static string ComposeSystemPrompt(string content)
    {
        var normalized = NormalizeManagedContent(content);
        return normalized.SemanticContent + "\n\n" + LockedBlock.Trim();
    }

    private static string RemoveManagedBlock(string content, ref bool removed)
    {
        var beginCount = Count(content, BeginMarker);
        var endCount = Count(content, EndMarker);
        if (beginCount == 0 && endCount == 0)
        {
            return content;
        }

        if (beginCount != 1 || endCount != 1)
        {
            throw SchemaMutation();
        }

        var begin = content.IndexOf(BeginMarker, StringComparison.Ordinal);
        var end = content.IndexOf(EndMarker, begin + BeginMarker.Length, StringComparison.Ordinal);
        if (begin < 0 || end < 0 || end < begin)
        {
            throw SchemaMutation();
        }

        var managedBlock = content[begin..(end + EndMarker.Length)];
        if (!Equivalent(managedBlock, LockedBlock))
        {
            throw SchemaMutation();
        }

        removed = true;
        return content.Remove(begin, end + EndMarker.Length - begin);
    }

    private static string RemoveCurrentEmbeddedBlock(string content, ref bool removed)
    {
        var heading = "OUTPUT SCHEMA";
        var endHeading = "VERBATIM SECTION RULES";
        var start = content.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0)
        {
            return content;
        }

        var endHeadingIndex = content.IndexOf(endHeading, start + heading.Length, StringComparison.Ordinal);
        if (endHeadingIndex < 0)
        {
            if (HasEmbeddedSchemaSignature(content[start..]))
            {
                throw SchemaMutation();
            }

            return content;
        }

        var open = content.IndexOf('{', start + heading.Length);
        if (open < 0 || open >= endHeadingIndex || !TryExtractBalancedObject(content, open, out var json, out var close))
        {
            if (HasEmbeddedSchemaSignature(content[start..endHeadingIndex]))
            {
                throw SchemaMutation();
            }

            return content;
        }

        if (!Equivalent(json, LockedSchemaJson))
        {
            throw SchemaMutation();
        }

        removed = true;
        return content.Remove(start, close - start);
    }

    private static string RemoveHistoricalV1Block(string content, ref bool removed)
    {
        var start = content.IndexOf(LegacyV1SchemaSentence, StringComparison.Ordinal);
        if (start < 0)
        {
            return content;
        }

        var open = content.IndexOf('{', start + LegacyV1SchemaSentence.Length);
        if (open < 0 || !TryExtractBalancedObject(content, open, out var json, out var close) ||
            !Equivalent(json, HistoricalV1SchemaJson))
        {
            throw SchemaMutation();
        }

        removed = true;
        return content.Remove(start, close - start);
    }

    private static string RemoveCurrentUserEmbeddedBlock(string content, ref bool removed)
    {
        var instructionCount = Count(content, CurrentUserSchemaVersionInstruction);
        var headingCount = Count(content, CurrentUserSchemaHeading);
        if (instructionCount == 0 && headingCount == 0)
        {
            return content;
        }

        if (instructionCount != 1 || headingCount != 1)
        {
            throw SchemaMutation();
        }

        var start = content.IndexOf(CurrentUserSchemaVersionInstruction, StringComparison.Ordinal);
        var heading = content.IndexOf(CurrentUserSchemaHeading, StringComparison.Ordinal);
        if (start < 0 || heading <= start)
        {
            throw SchemaMutation();
        }

        var between = content[start..heading].Trim();
        if (!between.Equals(CurrentUserSchemaVersionInstruction, StringComparison.Ordinal))
        {
            throw SchemaMutation();
        }

        var open = content.IndexOf('{', heading + CurrentUserSchemaHeading.Length);
        if (open < 0 ||
            !TryExtractBalancedObject(content, open, out var json, out var close) ||
            !Equivalent(json, CurrentUserSchemaJson))
        {
            throw SchemaMutation();
        }

        removed = true;
        return content.Remove(start, close - start);
    }

    private static bool HasEmbeddedSchemaSignature(string content) =>
        content.Contains("\"verbatim_sections\"", StringComparison.Ordinal) &&
        content.Contains("\"matching_metrics\"", StringComparison.Ordinal) &&
        (content.Contains("\"schema_version\"", StringComparison.Ordinal) ||
         content.Contains("\"professional_experience_and_projects\"", StringComparison.Ordinal));

    private static bool TryExtractBalancedObject(
        string content,
        int open,
        out string json,
        out int closeExclusive)
    {
        json = string.Empty;
        closeExclusive = open;
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = open; i < content.Length; i++)
        {
            var character = content[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
            }
            else if (character == '{')
            {
                depth++;
            }
            else if (character == '}' && --depth == 0)
            {
                closeExclusive = i + 1;
                json = content[open..closeExclusive];
                return true;
            }
            else if (character == '}' && depth < 0)
            {
                return false;
            }
        }

        return false;
    }

    private static bool Equivalent(string left, string right) =>
        NormalizeForComparison(left) == NormalizeForComparison(right);

    private static string NormalizeForComparison(string value)
    {
        var normalized = NormalizeLineEndings(value).Trim();
        var builder = new System.Text.StringBuilder(normalized.Length);
        var inString = false;
        var escaped = false;

        foreach (var character in normalized)
        {
            if (inString)
            {
                builder.Append(character);
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
                builder.Append(character);
            }
            else if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string NormalizeSemanticContent(string content)
    {
        var lines = NormalizeLineEndings(content).Split('\n');
        var nonEmpty = lines.Where(line => line.Trim().Length > 0).ToArray();
        var commonIndent = nonEmpty.Length == 0
            ? 0
            : nonEmpty.Min(line => line.TakeWhile(char.IsWhiteSpace).Count());

        return string.Join("\n", lines.Select(line =>
                line.Length >= commonIndent ? line[commonIndent..].TrimEnd() : string.Empty))
            .Trim();
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;

    private static ArgumentException SchemaMutation() =>
        new("CV_ANALYSIS_PROMPT_SCHEMA_MUTATION", "content");
}
