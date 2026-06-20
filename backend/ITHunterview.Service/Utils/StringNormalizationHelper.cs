using System;
using System.Text.RegularExpressions;

namespace ITHunterview.Service.Utils
{
    public static class StringNormalizationHelper
    {
        public static string NormalizeITTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return string.Empty;

            var normalized = term.Trim().ToLowerInvariant();

            // Specific IT mappings (replace special names before stripping non-alphanumeric characters)
            normalized = normalized.Replace("c#", "csharp");
            normalized = normalized.Replace("c++", "cplusplus");
            normalized = normalized.Replace(".net", "dotnet");

            // Remove all whitespace and non-alphanumeric characters
            normalized = Regex.Replace(normalized, @"[^a-z0-9]", "");

            return normalized;
        }

        public static bool IsDuplicate(string value1, string value2)
        {
            return NormalizeITTerm(value1) == NormalizeITTerm(value2);
        }
    }
}
