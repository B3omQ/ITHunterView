using System.Text.Json;

namespace ITHunterview.Service.Service.Matching;

internal enum JdStageTwoPropertyReadStatus
{
    Missing,
    Found,
    Ambiguous
}

/// <summary>
/// Reads only mechanically equivalent Stage 2 property spellings. This helper
/// deliberately does not recognize semantic aliases such as skill or resultCode.
/// </summary>
internal static class JdStageTwoJsonPropertyReader
{
    public static JdStageTwoPropertyReadStatus Read(
        JsonElement element,
        string canonicalName,
        string? alternateName,
        out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return JdStageTwoPropertyReadStatus.Missing;
        }

        var found = false;
        foreach (var property in element.EnumerateObject())
        {
            if (!IsApprovedName(property.Name, canonicalName, alternateName))
            {
                continue;
            }

            if (found)
            {
                value = default;
                return JdStageTwoPropertyReadStatus.Ambiguous;
            }

            found = true;
            value = property.Value;
        }

        return found
            ? JdStageTwoPropertyReadStatus.Found
            : JdStageTwoPropertyReadStatus.Missing;
    }

    public static bool IsApprovedName(
        string actualName,
        string canonicalName,
        string? alternateName) =>
        string.Equals(actualName, canonicalName, StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrEmpty(alternateName) &&
         string.Equals(actualName, alternateName, StringComparison.OrdinalIgnoreCase));
}
