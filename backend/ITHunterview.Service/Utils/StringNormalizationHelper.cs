using System;
using System.Text.RegularExpressions;

namespace ITHunterview.Service.Utils
{
    public static class StringNormalizationHelper
    {
        private static readonly Regex PunctuationRegex = new Regex(@"[^\w\s\+#\-\.\/&]", RegexOptions.Compiled);

        public static string NormalizeITTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return string.Empty;

            string cleaned = PunctuationRegex.Replace(term.Trim(), "").ToLowerInvariant();
            return string.Join(" ", cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        public static bool IsDuplicate(string value1, string value2)
        {
            return NormalizeITTerm(value1) == NormalizeITTerm(value2);
        }
    }
}

