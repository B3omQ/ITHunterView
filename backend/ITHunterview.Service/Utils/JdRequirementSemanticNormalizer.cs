using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ITHunterview.Service.Utils;

/// <summary>
/// Applies only deterministic JD v3 corrections that are directly supported by
/// evidenced text. It never invents a requirement or changes ambiguous wording.
/// </summary>
public static class JdRequirementSemanticNormalizer
{
    private static readonly HashSet<string> TechnicalPractices = new(StringComparer.Ordinal)
    {
        "performance optimization", "scalability", "caching", "caching strategies",
        "job queue", "job queues", "asynchronous processing", "ci/cd", "deployment",
        "security review", "system design", "testing", "testing practice"
    };

    private static readonly Regex AtLeastYears = new(
        @"(?:at\s+least|minimum|at\s+minimum|t\u1ed1i\s+thi\u1ec3u|\u00edt\s+nh\u1ea5t|t\u1eeb)\s+(?<years>\d+)\s*(?:\+\s*)?(?:years?|n\u0103m)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PlusYears = new(
        @"(?<years>\d+)\s*\+\s*(?:years?|n\u0103m)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RangeYears = new(
        @"(?<min>\d+)\s*(?:-|\u2013|to)\s*(?<max>\d+)\s*(?:years?|n\u0103m)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex MandatoryLanguage = new(
        @"\b(required|must|mandatory|proficient|experience\s+with)\b|y\u00eau\s+c\u1ea7u|th\u00e0nh\s+th\u1ea1o",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AlternativeLanguage = new(
        @"\b(?:or(?!\s+similar\b)|ho\u1eb7c)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ExampleLanguage = new(
        @"\b(?:e\.?g\.?|for\s+example|such\s+as|v\u00ed\s+d\u1ee5)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryNormalize(
        ValidatedJobAnalysis analysis,
        JobAnalysisInputSnapshot input,
        out string failureCode,
        out string failureMessage)
    {
        failureCode = string.Empty;
        failureMessage = string.Empty;

        foreach (var group in analysis.RequirementGroups)
        {
            foreach (var item in group.Items)
            {
                item.Category = NormalizeCategory(item.Category, item.SkillName);
                var years = ParseYears(item.Evidences.Append(item.DetailVerbatim));
                if (years.MinYears.HasValue)
                {
                    item.MinYears = years.MinYears;
                    item.MaxYears = years.MaxYears;
                }
            }

            if (group.Items.Select(item => item.Category).Distinct(StringComparer.Ordinal).Skip(1).Any())
            {
                failureCode = "JD_ANALYSIS_MIXED_GROUP_CATEGORY";
                failureMessage = "A jd-analysis/v3 requirement group must have one canonical category.";
                return false;
            }

            if (group.Operator == "all_of" &&
                group.Items.Count > 1 &&
                group.Items.SelectMany(item => item.Evidences.Append(item.DetailVerbatim))
                    .Any(evidence => AlternativeLanguage.IsMatch(evidence)))
            {
                failureCode = "JD_ANALYSIS_OPERATOR_CONFLICT";
                failureMessage = "An all_of group cannot be grounded only by alternative wording.";
                return false;
            }

            if (group.Items.Any(IsStandaloneExample))
            {
                failureCode = "JD_ANALYSIS_EXAMPLE_PROMOTED_TO_REQUIREMENT";
                failureMessage = "An illustrative example cannot become a standalone requirement item.";
                return false;
            }

            // If the author supplied a real requirements section, unsupported
            // duties must not silently become mandatory gates.
            if (!string.IsNullOrWhiteSpace(input.Requirements) &&
                group.Importance == "must_have" &&
                group.Items.All(item => item.SourceSection == "description") &&
                group.Items.All(item => !MandatoryLanguage.IsMatch(string.Join(" ", item.Evidences.Append(item.DetailVerbatim)))))
            {
                group.Importance = "nice_to_have";
                foreach (var item in group.Items)
                {
                    item.Importance = "nice_to_have";
                }
            }
        }

        analysis.TotalYearsExp = Math.Max(
            analysis.TotalYearsExp,
            analysis.RequirementGroups.SelectMany(group => group.Items).Max(item => item.MinYears ?? 0));
        return true;
    }

    public static string CreateItemToken(string category, string skillName, int? minYears, int? maxYears)
        => "itm-" + Hash($"{category}|{skillName}|{minYears}|{maxYears}", 16);

    public static string CreateGroupId(string importance, string @operator, int minSatisfied, IEnumerable<string> itemTokens)
        => "grp-" + Hash(
            $"{importance}|{@operator}|{minSatisfied}|{string.Join(",", itemTokens.OrderBy(value => value, StringComparer.Ordinal))}",
            16);

    private static string NormalizeCategory(string category, string skillName)
    {
        var normalizedCategory = (category ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedSkill = Normalize(skillName);
        return TechnicalPractices.Contains(normalizedSkill) ? "tech_skill" : normalizedCategory;
    }

    private static (int? MinYears, int? MaxYears) ParseYears(IEnumerable<string> sources)
    {
        var text = string.Join(" ", sources.Where(source => !string.IsNullOrWhiteSpace(source)));
        var range = RangeYears.Match(text);
        if (range.Success)
        {
            return (int.Parse(range.Groups["min"].Value), int.Parse(range.Groups["max"].Value));
        }

        var lowerBound = AtLeastYears.Match(text);
        if (!lowerBound.Success) lowerBound = PlusYears.Match(text);
        return lowerBound.Success ? (int.Parse(lowerBound.Groups["years"].Value), null) : (null, null);
    }

    private static string Normalize(string value) => string.Join(
        " ",
        (value ?? string.Empty).Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool IsStandaloneExample(ValidatedRequirementItem item)
    {
        var mention = string.IsNullOrWhiteSpace(item.RawMention) ? item.SkillName : item.RawMention;
        if (string.IsNullOrWhiteSpace(mention))
            return false;

        var evidences = item.Evidences.Append(item.DetailVerbatim)
            .Where(evidence => !string.IsNullOrWhiteSpace(evidence))
            .ToArray();
        return evidences.Length > 0 && evidences.All(evidence =>
        {
            var marker = ExampleLanguage.Match(evidence);
            var mentionIndex = evidence.IndexOf(mention, StringComparison.OrdinalIgnoreCase);
            return marker.Success && mentionIndex >= marker.Index + marker.Length;
        });
    }

    private static string Hash(string value, int length)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..length];
}
