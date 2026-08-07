using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ITHunterview.Service.Utils;

/// <summary>
/// Creates deterministic identities from fields already present in the
/// validated payload. It never classifies or rewrites requirement meaning.
/// </summary>
public static class JdRequirementIdentity
{
    public static string CreateItemToken(
        string category,
        string skillName,
        int? minYears,
        int? maxYears,
        string? rawMention = null)
    {
        var canonical = string.Join("|", new[]
        {
            Normalize(category),
            Normalize(skillName),
            Normalize(rawMention),
            minYears?.ToString() ?? "-",
            maxYears?.ToString() ?? "-"
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"itm-{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }

    /// <summary>
    /// Keeps the historical v3 item identity format for effective snapshots
    /// created before the quality envelope was introduced. This is a format
    /// compatibility helper only; it does not classify or rewrite meaning.
    /// </summary>
    public static string CreateLegacyItemToken(
        string category,
        string skillName,
        int? minYears,
        int? maxYears)
    {
        var canonical = $"{Normalize(category)}|{Normalize(skillName)}|{minYears?.ToString() ?? ""}|{maxYears?.ToString() ?? ""}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"itm-{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }

    public static string CreateGroupId(
        string importance,
        string @operator,
        int minSatisfied,
        string sourceSection,
        string requirementVerbatim,
        IEnumerable<string> itemTokens)
    {
        var canonical = string.Join("|", new[]
        {
            Normalize(importance),
            Normalize(@operator),
            minSatisfied.ToString(),
            Normalize(sourceSection),
            Normalize(requirementVerbatim),
            string.Join(",", itemTokens.OrderBy(value => value, StringComparer.Ordinal))
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"grp-{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }

    private static string Normalize(string? value) =>
        string.Join(" ", (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Trim()
            .ToLowerInvariant();
}
