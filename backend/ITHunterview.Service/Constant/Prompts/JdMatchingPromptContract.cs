namespace ITHunterview.Service.Constant.Prompts;

public static class JdMatchingPromptContract
{
    public const string PromptKey = BypassMatchingPrompt.Key;
    public const string CvPlaceholder = "[CV_TEXT]";
    public const string RequirementsPlaceholder = "[PARSED_JD_REQUIREMENTS]";

    private const string CvInputStart = "--- START CV ---";
    private const string CvInputEnd = "--- END CV ---";
    private const string RequirementsInputStart = "--- START JD REQUIREMENTS ---";
    private const string RequirementsInputEnd = "--- END JD REQUIREMENTS ---";

    /// <summary>
    /// Finds the one operational input slot for a matching placeholder. The
    /// active v2 prompt also mentions the JD placeholder once in an English
    /// task instruction; that explanatory mention is not an input slot and is
    /// intentionally preserved. When delimiters are absent, every occurrence
    /// is treated as a slot and therefore must be unique.
    /// </summary>
    public static int FindOperationalPlaceholderIndex(string content, string placeholder)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(placeholder);

        var (startMarker, endMarker) = placeholder switch
        {
            CvPlaceholder => (CvInputStart, CvInputEnd),
            RequirementsPlaceholder => (RequirementsInputStart, RequirementsInputEnd),
            _ => (string.Empty, string.Empty)
        };

        var allIndexes = FindAll(content, placeholder);
        if (allIndexes.Count == 0)
        {
            return -1;
        }

        if (startMarker.Length == 0)
        {
            return allIndexes.Count == 1 ? allIndexes[0] : -1;
        }

        var startIndexes = FindAll(content, startMarker);
        var endIndexes = FindAll(content, endMarker);
        if (startIndexes.Count > 1 || endIndexes.Count > 1)
        {
            return -1;
        }

        var start = startIndexes.Count == 1 ? startIndexes[0] : -1;
        var end = start < 0 || endIndexes.Count == 0 ? -1 : endIndexes[0];
        if (start < 0 || end <= start)
        {
            return allIndexes.Count == 1 ? allIndexes[0] : -1;
        }

        var slotIndexes = allIndexes
            .Where(index => index >= start + startMarker.Length && index < end)
            .ToList();
        return slotIndexes.Count == 1 ? slotIndexes[0] : -1;
    }

    private static List<int> FindAll(string content, string value)
    {
        var indexes = new List<int>();
        var offset = 0;
        while (offset <= content.Length - value.Length)
        {
            var index = content.IndexOf(value, offset, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            indexes.Add(index);
            offset = index + value.Length;
        }

        return indexes;
    }
}
