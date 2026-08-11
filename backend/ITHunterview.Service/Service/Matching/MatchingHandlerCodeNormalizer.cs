namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Performs only mechanical handler-code normalization. It never derives a
/// handler from category, evidence, reasoning, or provider numeric fields.
/// </summary>
public static class MatchingHandlerCodeNormalizer
{
    public const int MaximumHandlerCodeLength = 100;

    public static bool TryNormalize(
        string? rawHandlerCode,
        out MatchingHandlerResolution resolution,
        out string diagnosticCode)
    {
        resolution = null!;
        diagnosticCode = "UNKNOWN_HANDLER_CODE";
        if (string.IsNullOrWhiteSpace(rawHandlerCode) ||
            rawHandlerCode.Length > MaximumHandlerCodeLength)
        {
            diagnosticCode = "INVALID_HANDLER_CODE";
            return false;
        }

        var trimmed = rawHandlerCode.Trim();
        if (MatchingScorePolicy.TryResolveHandlerCode(trimmed, out resolution))
        {
            diagnosticCode = string.Equals(trimmed, resolution.HandlerCode, StringComparison.Ordinal)
                ? string.Empty
                : "HANDLER_CODE_CASE_NORMALIZED";
            return true;
        }

        var matches = MatchingScorePolicy.SupportedHandlerCodes
            .Where(code => ContainsAtAlphanumericBoundaries(trimmed, code))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (matches.Length != 1)
        {
            diagnosticCode = matches.Length > 1
                ? "AMBIGUOUS_HANDLER_CODE"
                : "UNKNOWN_HANDLER_CODE";
            return false;
        }

        if (!MatchingScorePolicy.TryResolveHandlerCode(matches[0], out resolution))
        {
            diagnosticCode = "UNKNOWN_HANDLER_CODE";
            return false;
        }

        diagnosticCode = "HANDLER_CODE_DECORATION_NORMALIZED";
        return true;
    }

    private static bool ContainsAtAlphanumericBoundaries(string value, string canonicalCode)
    {
        var startIndex = 0;
        while (startIndex <= value.Length - canonicalCode.Length)
        {
            var index = value.IndexOf(canonicalCode, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var beforeIsAlphanumeric = index > 0 && char.IsLetterOrDigit(value[index - 1]);
            var afterIndex = index + canonicalCode.Length;
            var afterIsAlphanumeric = afterIndex < value.Length && char.IsLetterOrDigit(value[afterIndex]);
            if (!beforeIsAlphanumeric && !afterIsAlphanumeric)
            {
                return true;
            }

            startIndex = index + 1;
        }

        return false;
    }
}
