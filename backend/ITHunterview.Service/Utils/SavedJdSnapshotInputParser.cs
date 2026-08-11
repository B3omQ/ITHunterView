using System.Text;

namespace ITHunterview.Service.Utils;

/// <summary>
/// Reads only the deterministic labels emitted by <see cref="JdTextHelper.BuildRawText"/>.
/// It is a compatibility reader for persisted saved-JD snapshots, not a parser
/// for arbitrary user-pasted job text.
/// </summary>
internal static class SavedJdSnapshotInputParser
{
    internal sealed record Result(
        string Title,
        string Description,
        string Requirements,
        bool HasRecognizedLabels);

    private enum Section
    {
        None,
        Title,
        Description,
        Requirements,
        Ignored
    }

    private static readonly (string Label, Section Section)[] Labels =
    {
        ("Title:", Section.Title),
        ("Description:", Section.Description),
        ("Requirements:", Section.Requirements),
        ("Benefits:", Section.Ignored),
        ("Income:", Section.Ignored),
        ("Work Location:", Section.Ignored),
        ("Working Hours:", Section.Ignored),
        ("How to Apply:", Section.Ignored)
    };

    public static Result Parse(string? originalText)
    {
        var title = new List<string>();
        var description = new List<string>();
        var requirements = new List<string>();
        var section = Section.None;
        var recognized = false;
        var normalized = (originalText ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        foreach (var rawLine in normalized.Split('\n'))
        {
            if (TryReadLabel(rawLine, out var nextSection, out var firstValue))
            {
                recognized = true;
                section = nextSection;
                Append(section, firstValue, title, description, requirements);
                continue;
            }

            Append(section, rawLine, title, description, requirements);
        }

        return new Result(
            Join(title),
            Join(description),
            Join(requirements),
            recognized);
    }

    private static bool TryReadLabel(string line, out Section section, out string value)
    {
        var candidate = line.TrimStart();
        foreach (var (label, targetSection) in Labels)
        {
            if (!candidate.StartsWith(label, StringComparison.Ordinal))
            {
                continue;
            }

            section = targetSection;
            value = candidate[label.Length..].Trim();
            return true;
        }

        section = Section.None;
        value = string.Empty;
        return false;
    }

    private static void Append(
        Section section,
        string value,
        ICollection<string> title,
        ICollection<string> description,
        ICollection<string> requirements)
    {
        switch (section)
        {
            case Section.Title:
                title.Add(value);
                break;
            case Section.Description:
                description.Add(value);
                break;
            case Section.Requirements:
                requirements.Add(value);
                break;
        }
    }

    private static string Join(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }
            builder.Append(line);
        }
        return builder.ToString();
    }
}
