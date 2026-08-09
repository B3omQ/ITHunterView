using System;
using System.Linq;
using System.Text;

namespace ITHunterview.Service.Constant.Prompts;

public sealed record JdAnalysisPromptNormalization(
    string SemanticContent,
    bool RemovedKnownSchema);

/// <summary>
/// Owns the provider-facing JD analysis output contract. Prompt Management
/// stores semantic instructions only; this block is appended at the provider
/// boundary and cannot be selected through prompt-pair metadata.
/// </summary>
public static class JdAnalysisOutputSchema
{
    public const string ProviderSchemaVersion = "jd-analysis/v5";
    public const string BeginMarker = "--- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---";
    public const string EndMarker = "--- END LOCKED JD ANALYSIS OUTPUT SCHEMA ---";

    private const string LegacyStartMarker = "OUTPUT CONTRACT";
    private const string LegacyEndMarker = "EVIDENCE AND SOURCE RULES";

    private const string LegacyV4SchemaJson = """
        {
          "schema_version": "jd-analysis/v4",
          "matching_metrics": {
            "job_titles_normalized": [],
            "total_years_exp": 0,
            "domains": [],
            "requirement_groups": [
              {
                "operator": "all_of",
                "importance": "must_have",
                "source_section": "requirements",
                "requirement_verbatim": "exact complete source clause supporting this group",
                "items": [
                  {
                    "category": "tech_skill",
                    "skill_name": "normalized lowercase requirement name",
                    "raw_mention": "exact phrase from requirement_verbatim"
                  }
                ]
              }
            ]
          }
        }
        """;

    public const string LockedBlock = """
        --- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---
        This output format is managed by the application. It overrides any conflicting output-format instruction above.
        Return exactly one JSON object without Markdown, comments, headings, or surrounding text.

        {
          "schema_version": "jd-analysis/v5",
          "matching_metrics": {
            "job_titles_normalized": [],
            "total_years_exp": 0,
            "domains": [],
            "requirement_groups": [
              {
                "source_requirement_id": "req-001",
                "intent": "qualification",
                "operator": "all_of",
                "importance": "must_have",
                "source_section": "requirements",
                "requirement_verbatim": "exact complete source clause supporting this group",
                "items": [
                  {
                    "category": "tech_skill",
                    "skill_name": "normalized requirement name",
                    "raw_mention": "exact phrase from requirement_verbatim"
                  }
                ]
              }
            ]
          }
        }

        Fixed shape rules:
        - schema_version is exactly "jd-analysis/v5".
        - matching_metrics contains exactly job_titles_normalized, total_years_exp, domains, and requirement_groups.
        - source_requirement_id uses req-NNN in physical source-clause order; groups from the same clause reuse it.
        - intent is qualification or experience_duration.
        - operator is all_of, one_of, or at_least_n.
        - importance is must_have or nice_to_have.
        - source_section is title, description, or requirements.
        - requirement_verbatim is required and non-empty for every group.
        - every group contains at least one item.
        - category is tech_skill, experience, domain_knowledge, language, education, or soft_skill.
        - skill_name and raw_mention are non-empty strings.
        - min_years and max_years are optional non-negative integers; omit unsupported values instead of returning null.
        - min_satisfied appears only for at_least_n and is an integer from 1 through the item count.
        - output at most 50 requirement groups and at most 100 total group items.
        - do not output detail_verbatim, evidence, evidences, confidence, group_id, item_id, requirements_list, skills_normalized, or seniority_fit.
        --- END LOCKED JD ANALYSIS OUTPUT SCHEMA ---
        """;

    public static string ComposeSystemPrompt(string semanticContent)
    {
        var normalized = NormalizeManagedContent(semanticContent);
        return normalized.SemanticContent + "\n\n" + LockedBlock.Trim();
    }

    public static JdAnalysisPromptNormalization NormalizeManagedContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("JD_ANALYSIS_PROMPT_EMPTY", nameof(content));
        }

        var working = NormalizeLineEndings(content);
        var removedKnownSchema = false;

        working = RemoveManagedBlock(working, ref removedKnownSchema);
        working = RemoveLegacyV4Block(working, ref removedKnownSchema);

        if (HasEmbeddedSchemaSignature(working))
        {
            throw SchemaMutation();
        }

        var semanticContent = NormalizeSemanticContent(working);
        if (semanticContent.Length == 0 ||
            semanticContent.Equals(LegacyEndMarker, StringComparison.Ordinal))
        {
            throw new ArgumentException("JD_ANALYSIS_PROMPT_EMPTY_AFTER_SCHEMA", nameof(content));
        }

        return new JdAnalysisPromptNormalization(semanticContent, removedKnownSchema);
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
        if (begin < 0 || end <= begin)
        {
            throw SchemaMutation();
        }

        var managedBlock = content[begin..(end + EndMarker.Length)];
        if (!Equivalent(managedBlock, LockedBlock))
        {
            throw SchemaMutation();
        }

        removed = true;
        return JoinSections(
            content[..begin],
            content[(end + EndMarker.Length)..]);
    }

    private static string RemoveLegacyV4Block(string content, ref bool removed)
    {
        var startCount = Count(content, LegacyStartMarker);
        var endCount = Count(content, LegacyEndMarker);
        // The legacy end marker is also a valid semantic section heading. Once
        // the old output block has been removed, normalization must remain
        // idempotent and preserve that heading.
        if (startCount == 0)
        {
            return content;
        }

        if (startCount != 1 || endCount != 1)
        {
            throw SchemaMutation();
        }

        var start = content.IndexOf(LegacyStartMarker, StringComparison.Ordinal);
        var end = content.IndexOf(LegacyEndMarker, start + LegacyStartMarker.Length, StringComparison.Ordinal);
        var open = content.IndexOf('{', start + LegacyStartMarker.Length);
        if (start < 0 || end <= start || open < 0 || open >= end ||
            !TryExtractBalancedObject(content, open, out var schemaJson, out _))
        {
            throw SchemaMutation();
        }

        if (!Equivalent(schemaJson, LegacyV4SchemaJson))
        {
            throw SchemaMutation();
        }

        removed = true;
        return JoinSections(content[..start], content[end..]);
    }

    private static bool HasEmbeddedSchemaSignature(string content) =>
        content.Contains("\"schema_version\"", StringComparison.Ordinal) &&
        content.Contains("\"matching_metrics\"", StringComparison.Ordinal) &&
        content.Contains("\"requirement_groups\"", StringComparison.Ordinal);

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

        for (var index = open; index < content.Length; index++)
        {
            var character = content[index];
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
                closeExclusive = index + 1;
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
        string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), StringComparison.Ordinal);

    private static string NormalizeForComparison(string value)
    {
        var normalized = NormalizeLineEndings(value).Trim();
        var builder = new StringBuilder(normalized.Length);
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

    private static string JoinSections(string before, string after)
    {
        var normalizedBefore = before.TrimEnd();
        var normalizedAfter = after.TrimStart();

        if (normalizedBefore.Length == 0)
        {
            return normalizedAfter;
        }

        if (normalizedAfter.Length == 0)
        {
            return normalizedBefore;
        }

        return normalizedBefore + "\n\n" + normalizedAfter;
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;

    private static ArgumentException SchemaMutation() =>
        new("JD_ANALYSIS_PROMPT_SCHEMA_MUTATION", "content");
}
