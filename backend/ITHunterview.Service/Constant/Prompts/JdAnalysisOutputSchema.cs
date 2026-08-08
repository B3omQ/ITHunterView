using System;

namespace ITHunterview.Service.Constant.Prompts;

public static class JdAnalysisOutputSchema
{
    public const string BeginMarker = "--- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---";
    public const string EndMarker = "--- END LOCKED JD ANALYSIS OUTPUT SCHEMA ---";

    private const string LegacyStartMarker = "OUTPUT CONTRACT";
    private const string LegacyEndMarker = "EVIDENCE AND SOURCE RULES";

    public const string LockedBlock = """
        --- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---
        This output format is managed by the application. It overrides any conflicting output-format instruction above.
        Return exactly one JSON object without Markdown, comments, headings, or surrounding text.

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
                    "skill_name": "normalized requirement name",
                    "raw_mention": "exact phrase from requirement_verbatim",
                    "min_years": null,
                    "max_years": null
                  }
                ]
              }
            ]
          }
        }

        Fixed shape rules:
        - schema_version is exactly "jd-analysis/v4".
        - matching_metrics contains exactly job_titles_normalized, total_years_exp, domains, and requirement_groups.
        - operator is all_of, one_of, or at_least_n.
        - importance is must_have or nice_to_have.
        - source_section is title, description, or requirements.
        - requirement_verbatim is required and non-empty for every group.
        - every group contains at least one item.
        - category is tech_skill, experience, domain_knowledge, language, education, or soft_skill.
        - skill_name and raw_mention are non-empty strings.
        - min_years and max_years are non-negative integers or null.
        - min_satisfied appears only for at_least_n and is an integer from 1 through the item count.
        - do not output detail_verbatim, evidence, evidences, confidence, group_id, requirements_list, or skills_normalized.
        --- END LOCKED JD ANALYSIS OUTPUT SCHEMA ---
        """;

    public static string ComposeSystemPrompt(string semanticContent)
    {
        if (string.IsNullOrWhiteSpace(semanticContent))
        {
            throw new ArgumentException("JD analysis system prompt content is required.", nameof(semanticContent));
        }

        return $"{RemoveEmbeddedSchemaBlock(semanticContent).Trim()}\n\n{LockedBlock}";
    }

    public static string RemoveEmbeddedSchemaBlock(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        var lockedStart = content.IndexOf(BeginMarker, StringComparison.Ordinal);
        var lockedEnd = content.IndexOf(EndMarker, StringComparison.Ordinal);
        if (HasSingleOrderedPair(content, BeginMarker, EndMarker, lockedStart, lockedEnd))
        {
            return JoinSections(
                content[..lockedStart],
                content[(lockedEnd + EndMarker.Length)..]);
        }

        var legacyStart = content.IndexOf(LegacyStartMarker, StringComparison.Ordinal);
        var legacyEnd = content.IndexOf(LegacyEndMarker, StringComparison.Ordinal);
        if (HasSingleOrderedPair(content, LegacyStartMarker, LegacyEndMarker, legacyStart, legacyEnd))
        {
            return JoinSections(content[..legacyStart], content[legacyEnd..]);
        }

        return content;
    }

    private static bool HasSingleOrderedPair(
        string content,
        string startMarker,
        string endMarker,
        int startIndex,
        int endIndex) =>
        startIndex >= 0 &&
        endIndex > startIndex &&
        content.IndexOf(startMarker, startIndex + startMarker.Length, StringComparison.Ordinal) < 0 &&
        content.IndexOf(endMarker, endIndex + endMarker.Length, StringComparison.Ordinal) < 0;

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

        return $"{normalizedBefore}\n\n{normalizedAfter}";
    }
}
