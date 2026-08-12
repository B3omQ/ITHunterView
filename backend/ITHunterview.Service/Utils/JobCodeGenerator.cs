using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ITHunterview.Service.Utils
{
    public static class JobCodeGenerator
    {
        public static string GenerateSmartJobCode(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return $"JOB-{DateTime.UtcNow:yyMMdd}";
            }

            // 1. Remove diacritics and convert to uppercase
            string cleanTitle = RemoveDiacritics(title).ToUpper();

            // 2. Keep letters, numbers, spaces, plus (+), and sharp (#)
            cleanTitle = Regex.Replace(cleanTitle, @"[^A-Z0-9\s\+#]", " ");

            // 3. Filter common generic stop words (English & Vietnamese)
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DEVELOPER", "ENGINEER", "SPECIALIST", "OFFICER", "EXPERT", "SENIOR", "JUNIOR",
                "MIDDLE", "INTERN", "FRESHER", "STAFF", "MANAGER", "CONSULTANT", "POSITION",
                "LAP", "TRINH", "VIEN", "CHUYEN", "KY", "SU", "NHAN", "TRUONG", "PHONG",
                "TUYEN", "DUNG", "CAN", "FOR", "AT", "WITH", "AND", "OR", "IN", "THE"
            };

            var words = cleanTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !stopWords.Contains(w))
                .ToList();

            // Fallback if all words were filtered out
            if (words.Count == 0)
            {
                words = cleanTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            string prefix;
            if (words.Count == 1)
            {
                // Single core term (e.g. "DEVOPS", "QA", "FLUTTER", "CYBERSECURITY")
                string w = words[0];
                prefix = w.Length > 6 ? w.Substring(0, 5) : w;
            }
            else if (words.Count == 2)
            {
                // Two core terms (e.g. "JAVA", "BACKEND" -> "JAVA-BAC", "REACT", "NATIVE" -> "REACT-NAT")
                string w1 = words[0].Length > 5 ? words[0].Substring(0, 4) : words[0];
                string w2 = words[1].Length > 4 ? words[1].Substring(0, 3) : words[1];
                prefix = $"{w1}-{w2}";
            }
            else
            {
                // 3+ core terms: take top 3 terms abbreviated
                var parts = words.Take(3).Select(w => w.Length <= 4 ? w : w.Substring(0, 3));
                prefix = string.Join("-", parts);
            }

            prefix = Regex.Replace(prefix, @"\-+", "-").Trim('-');

            return $"{prefix}-{DateTime.UtcNow:yyMMdd}";
        }

        public static string GenerateUniqueSeededCode(string? title, HashSet<string> existingCodes)
        {
            string baseCode = GenerateSmartJobCode(title);
            if (!existingCodes.Contains(baseCode))
            {
                existingCodes.Add(baseCode);
                return baseCode;
            }

            int counter = 2;
            while (existingCodes.Contains($"{baseCode}-{counter}"))
            {
                counter++;
            }

            string uniqueCode = $"{baseCode}-{counter}";
            existingCodes.Add(uniqueCode);
            return uniqueCode;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D');
        }
    }
}
