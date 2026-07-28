using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ITHunterview.Service.Utils
{
    public sealed record JobPostingRichTextValue(string StoredMarkdown, string PlainText);

    /// <summary>
    /// Canonicalizes the intentionally small Markdown subset accepted for Job Posting text fields.
    /// This is a storage and AI-input boundary, not an HTML renderer.
    /// </summary>
    public static class JobPostingRichText
    {
        private static readonly Regex UnorderedListRegex = new(
            @"^[*+]\s+(.+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        private static readonly Regex OrderedListRegex = new(
            @"^(\d+)\.\s+(.+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        private static readonly Regex RawHtmlTagRegex = new(
            @"</?[A-Za-z][A-Za-z0-9:-]*(?:\s+[^<>]*)?\s*/?>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        public static JobPostingRichTextValue NormalizeForStorage(string? value)
        {
            var normalized = NormalizeLineEndingsAndUnicode(value);
            EnsureNoUnsafeControls(normalized);
            EnsureNoRawHtml(normalized, "Job posting rich text");

            var lines = new List<string>();
            var previousWasBlank = false;

            foreach (var rawLine in normalized.Split('\n'))
            {
                var line = CanonicalizeLine(rawLine);
                var isBlank = line.Length == 0;

                if (isBlank && (previousWasBlank || lines.Count == 0))
                {
                    previousWasBlank = true;
                    continue;
                }

                lines.Add(line);
                previousWasBlank = isBlank;
            }

            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            var storedMarkdown = string.Join("\n", lines);
            return new JobPostingRichTextValue(storedMarkdown, ToPlainText(storedMarkdown));
        }

        public static string ToPlainText(string? markdown)
        {
            var normalized = NormalizeLineEndingsAndUnicode(markdown);
            EnsureNoUnsafeControls(normalized);

            // Formatting can legitimately wrap a recruiter selection across line breaks.
            // Strip list markers per line first, then strip delimiters from the complete
            // document so **first line\nsecond line** remains formatting-only for AI hashes.
            var withoutListMarkers = new List<string>();
            foreach (var rawLine in normalized.Split('\n'))
            {
                withoutListMarkers.Add(StripListMarker(rawLine.Trim()));
            }

            var withoutInlineFormatting = StripInlineFormatting(string.Join("\n", withoutListMarkers));
            var lines = new List<string>();
            var previousWasBlank = false;
            foreach (var rawLine in withoutInlineFormatting.Split('\n'))
            {
                var line = CollapseHorizontalWhitespace(rawLine).Trim();

                var isBlank = line.Length == 0;
                if (isBlank && (previousWasBlank || lines.Count == 0))
                {
                    previousWasBlank = true;
                    continue;
                }

                lines.Add(line);
                previousWasBlank = isBlank;
            }

            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines);
        }

        public static bool HasVisibleText(string? markdown)
        {
            foreach (var line in ToPlainText(markdown).Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed is "-" or "*" or "+") continue;
                if (IsFormattingOnlyTokenSequence(trimmed)) continue;

                return true;
            }

            return false;
        }

        public static void EnsureNoRawHtml(string? markdown, string fieldName)
        {
            if (!string.IsNullOrEmpty(markdown) && RawHtmlTagRegex.IsMatch(markdown))
            {
                throw new ArgumentException($"{fieldName} must not contain raw HTML.", fieldName);
            }
        }

        private static string CanonicalizeLine(string rawLine)
        {
            var line = NormalizeMalformedUnorderedListMarker(CollapseHorizontalWhitespace(rawLine).Trim());
            if (line.Length == 0) return string.Empty;

            var unordered = UnorderedListRegex.Match(line);
            if (unordered.Success)
            {
                return $"- {unordered.Groups[1].Value.Trim()}";
            }

            var ordered = OrderedListRegex.Match(line);
            if (ordered.Success && int.TryParse(ordered.Groups[1].Value, out var number) && number > 0)
            {
                return $"{number}. {ordered.Groups[2].Value.Trim()}";
            }

            return line;
        }

        private static string StripListMarker(string value)
        {
            value = NormalizeMalformedUnorderedListMarker(value);
            if (value.StartsWith("- ", StringComparison.Ordinal) ||
                value.StartsWith("* ", StringComparison.Ordinal) ||
                value.StartsWith("+ ", StringComparison.Ordinal))
            {
                return value[2..];
            }

            var ordered = OrderedListRegex.Match(value);
            return ordered.Success ? ordered.Groups[2].Value : value;
        }

        private static string NormalizeMalformedUnorderedListMarker(string value)
        {
            // Preserve legacy content created by applying inline formatting right
            // after a manually typed hyphen: "-**text**" becomes "- **text**".
            if (value.Length < 2 || value[0] != '-')
            {
                return value;
            }

            return value.AsSpan(1).StartsWith("**", StringComparison.Ordinal) ||
                   value.AsSpan(1).StartsWith("++", StringComparison.Ordinal) ||
                   value[1] == '_'
                ? $"- {value[1..]}"
                : value;
        }

        private static string StripInlineFormatting(string value)
        {
            var result = new StringBuilder(value.Length);
            for (var index = 0; index < value.Length;)
            {
                if (TryReadDelimited(value, index, "**", out var boldContent, out var nextBoldIndex))
                {
                    result.Append(StripInlineFormatting(boldContent));
                    index = nextBoldIndex;
                    continue;
                }

                if (TryReadDelimited(value, index, "++", out var underlineContent, out var nextUnderlineIndex))
                {
                    result.Append(StripInlineFormatting(underlineContent));
                    index = nextUnderlineIndex;
                    continue;
                }

                if (value[index] == '_' &&
                    CanOpenUnderscoreDelimiter(value, index) &&
                    TryReadDelimited(value, index, "_", out var italicContent, out var nextItalicIndex))
                {
                    result.Append(StripInlineFormatting(italicContent));
                    index = nextItalicIndex;
                    continue;
                }

                result.Append(value[index]);
                index++;
            }

            return result.ToString();
        }

        private static bool CanOpenUnderscoreDelimiter(string value, int index)
        {
            var hasWordBefore = index > 0 && char.IsLetterOrDigit(value[index - 1]);
            var hasWordAfter = index + 1 < value.Length && char.IsLetterOrDigit(value[index + 1]);

            // Preserve identifier-like text such as some_text_here.
            return !(hasWordBefore && hasWordAfter);
        }

        private static bool TryReadDelimited(string value, int startIndex, string delimiter, out string content, out int nextIndex)
        {
            content = string.Empty;
            nextIndex = startIndex;

            if (!value.AsSpan(startIndex).StartsWith(delimiter, StringComparison.Ordinal))
            {
                return false;
            }

            var contentStart = startIndex + delimiter.Length;
            var closingIndex = value.IndexOf(delimiter, contentStart, StringComparison.Ordinal);
            if (closingIndex < contentStart || closingIndex == contentStart)
            {
                return false;
            }

            content = value.Substring(contentStart, closingIndex - contentStart);
            nextIndex = closingIndex + delimiter.Length;
            return true;
        }

        private static string NormalizeLineEndingsAndUnicode(string? value)
        {
            return (value ?? string.Empty)
                .Normalize(NormalizationForm.FormKC)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace('\u00A0', ' ')
                .Replace('\t', ' ');
        }

        private static void EnsureNoUnsafeControls(string value)
        {
            foreach (var character in value)
            {
                if (char.IsControl(character) && character != '\n')
                {
                    throw new ArgumentException("Job posting rich text contains an unsupported control character.");
                }
            }
        }

        private static bool IsFormattingOnlyTokenSequence(string value)
        {
            if (value.Length == 0) return false;

            foreach (var character in value)
            {
                if (character is not '*' and not '+' and not '_') return false;
            }

            return true;
        }

        private static string CollapseHorizontalWhitespace(string value)
        {
            var builder = new StringBuilder(value.Length);
            var previousWasWhitespace = false;

            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasWhitespace) builder.Append(' ');
                    previousWasWhitespace = true;
                    continue;
                }

                builder.Append(character);
                previousWasWhitespace = false;
            }

            return builder.ToString();
        }
    }
}
