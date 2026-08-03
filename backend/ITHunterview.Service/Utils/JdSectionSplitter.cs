using System;
using System.Collections.Generic;
using System.Text;

namespace ITHunterview.Service.Utils;

/// <summary>
/// Deterministically separates pasted job-description text into the three
/// analysis sources accepted by the JD contract. It intentionally recognises
/// headings only when they occupy a whole line, so ordinary prose cannot move
/// between sections by accident.
/// </summary>
public static class JdSectionSplitter
{
    private enum Section
    {
        Description,
        Requirements,
        Ignored
    }

    public sealed record Result(string Description, string Requirements, bool HasRecognizedSections);

    private static readonly HashSet<string> DescriptionHeadings = new(StringComparer.OrdinalIgnoreCase)
    {
        "m\u00f4 t\u1ea3 c\u00f4ng vi\u1ec7c", "tr\u00e1ch nhi\u1ec7m", "job description", "responsibilities",
        "duties", "what you will do", "role responsibilities"
    };

    private static readonly HashSet<string> RequirementHeadings = new(StringComparer.OrdinalIgnoreCase)
    {
        "y\u00eau c\u1ea7u \u1ee9ng vi\u00ean", "y\u00eau c\u1ea7u c\u00f4ng vi\u1ec7c", "requirements", "qualifications",
        "candidate requirements", "must have", "nice to have", "preferred qualifications"
    };

    private static readonly HashSet<string> IgnoredHeadings = new(StringComparer.OrdinalIgnoreCase)
    {
        "benefits", "quy\u1ec1n l\u1ee3i", "compensation", "work location", "about company"
    };

    public static Result Split(string? rawText)
    {
        var description = new List<string>();
        var requirements = new List<string>();
        var currentSection = Section.Description;
        var recognizedAny = false;

        foreach (var sourceLine in (rawText ?? string.Empty).Split('\n'))
        {
            if (TryReadHeading(sourceLine, out var heading, out var nextSection))
            {
                recognizedAny = true;
                currentSection = nextSection;
                if (currentSection == Section.Requirements)
                {
                    // Keep the source heading so the model can distinguish
                    // "Nice to have" from mandatory requirements.
                    requirements.Add(heading);
                }
                else if (currentSection == Section.Description)
                {
                    description.Add(heading);
                }

                continue;
            }

            switch (currentSection)
            {
                case Section.Description:
                    description.Add(sourceLine);
                    break;
                case Section.Requirements:
                    requirements.Add(sourceLine);
                    break;
                case Section.Ignored:
                    break;
            }
        }

        return new Result(
            JoinLines(description),
            JoinLines(requirements),
            recognizedAny);
    }

    private static bool TryReadHeading(string sourceLine, out string heading, out Section section)
    {
        heading = string.Empty;
        section = Section.Description;

        var candidate = (sourceLine ?? string.Empty).Trim();
        if (candidate.Length == 0 || candidate.Length > 160)
        {
            return false;
        }

        candidate = candidate.TrimStart('#', '-', '>', ' ')
            .Trim()
            .Trim('*', '_', ' ')
            .TrimEnd(':', '\uff1a', '*', '_', ' ')
            .Trim();

        if (candidate.Length == 0 || candidate.Length > 80)
        {
            return false;
        }

        if (DescriptionHeadings.Contains(candidate))
        {
            heading = candidate;
            section = Section.Description;
            return true;
        }

        if (RequirementHeadings.Contains(candidate))
        {
            heading = candidate;
            section = Section.Requirements;
            return true;
        }

        if (IgnoredHeadings.Contains(candidate))
        {
            heading = candidate;
            section = Section.Ignored;
            return true;
        }

        return false;
    }

    private static string JoinLines(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        var previousWasBlank = true;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                if (!previousWasBlank && builder.Length > 0)
                {
                    builder.AppendLine();
                    previousWasBlank = true;
                }
                continue;
            }

            if (builder.Length > 0 && !previousWasBlank)
            {
                builder.AppendLine();
            }

            builder.Append(trimmed);
            previousWasBlank = false;
        }

        return builder.ToString();
    }
}
