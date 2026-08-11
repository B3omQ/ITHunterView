using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdMatchingRecoveredOutput(
    JsonDocument? Document,
    bool IsCompleteJson,
    bool WasTruncated,
    IReadOnlyList<string> WarningCodes)
    : IDisposable
{
    public void Dispose() => Document?.Dispose();
}

/// <summary>
/// Recovers only mechanically complete score objects. It never appends missing
/// delimiters, changes requirement IDs, or invents scores.
/// </summary>
public static class JdMatchingOutputRecovery
{
    private const int MaxProviderCharacters = 1_000_000;
    private static readonly JsonDocumentOptions TolerantOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 64
    };

    public static JdMatchingRecoveredOutput Recover(string? providerOutput)
    {
        var candidate = StripMarkdownFence(providerOutput);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Invalid("EMPTY_MODEL_OUTPUT", wasTruncated: false);
        }

        if (candidate.Length > MaxProviderCharacters)
        {
            return Invalid("PAYLOAD_TOO_LARGE", wasTruncated: false);
        }

        if (TryParse(candidate, out var complete))
        {
            return new JdMatchingRecoveredOutput(
                complete,
                IsCompleteJson: true,
                WasTruncated: false,
                WarningCodes: Array.Empty<string>());
        }

        if (TryExtractBalancedRootObject(candidate, out var rootObject) && TryParse(rootObject, out var extracted))
        {
            return new JdMatchingRecoveredOutput(
                extracted,
                IsCompleteJson: false,
                WasTruncated: false,
                WarningCodes: new[] { "EXTRACTED_COMPLETE_JSON_OBJECT" });
        }

        if (!ContainsSupportedSchemaVersion(candidate))
        {
            return Invalid("SCHEMA_VERSION_MISSING_OR_UNSUPPORTED", wasTruncated: true);
        }

        var scores = ExtractCompleteScoreObjects(candidate);
        if (scores.Count == 0)
        {
            return Invalid("JSON_PARSE_FAILED", wasTruncated: true);
        }

        var recoveredJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schemaVersion"] = JdMatchingResponseValidator.SchemaVersion,
            ["scores"] = scores
        });
        return new JdMatchingRecoveredOutput(
            JsonDocument.Parse(recoveredJson),
            IsCompleteJson: false,
            WasTruncated: true,
            WarningCodes: new[] { "OUTPUT_TRUNCATED", "RECOVERED_COMPLETE_SCORE_OBJECTS" });
    }

    private static bool TryParse(string value, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(value, TolerantOptions);
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }
    }

    private static List<JsonElement> ExtractCompleteScoreObjects(string value)
    {
        var results = new List<JsonElement>();
        var arrayStart = FindScoresArrayStart(value);
        if (arrayStart < 0)
        {
            return results;
        }

        var inString = false;
        var escaped = false;
        var objectDepth = 0;
        var objectStart = -1;
        for (var index = arrayStart + 1; index < value.Length; index++)
        {
            var character = value[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }
                continue;
            }

            if (character == '"')
            {
                inString = true;
                continue;
            }

            if (character == '{')
            {
                if (objectDepth++ == 0)
                {
                    objectStart = index;
                }
                continue;
            }

            if (character == '}' && objectDepth > 0 && --objectDepth == 0 && objectStart >= 0)
            {
                var objectText = value[objectStart..(index + 1)];
                if (TryParse(objectText, out var item))
                {
                    using (item)
                    {
                        results.Add(item.RootElement.Clone());
                    }
                }
                objectStart = -1;
                continue;
            }

            if (character == ']' && objectDepth == 0)
            {
                break;
            }
        }

        return results;
    }

    private static bool ContainsSupportedSchemaVersion(string value)
    {
        const string property = "\"schemaVersion\"";
        var propertyIndex = value.IndexOf(property, StringComparison.Ordinal);
        if (propertyIndex < 0)
        {
            return false;
        }

        var index = propertyIndex + property.Length;
        while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
        if (index >= value.Length || value[index++] != ':') return false;
        while (index < value.Length && char.IsWhiteSpace(value[index])) index++;

        var expected = $"\"{JdMatchingResponseValidator.SchemaVersion}\"";
        return index + expected.Length <= value.Length &&
               value.AsSpan(index, expected.Length).SequenceEqual(expected.AsSpan());
    }

    private static int FindScoresArrayStart(string value)
    {
        const string property = "\"scores\"";
        var searchFrom = 0;
        while (searchFrom < value.Length)
        {
            var propertyIndex = value.IndexOf(property, searchFrom, StringComparison.Ordinal);
            if (propertyIndex < 0)
            {
                return -1;
            }

            var index = propertyIndex + property.Length;
            while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
            if (index < value.Length && value[index] == ':') index++;
            while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
            if (index < value.Length && value[index] == '[')
            {
                return index;
            }
            searchFrom = propertyIndex + property.Length;
        }

        return -1;
    }

    private static bool TryExtractBalancedRootObject(string value, out string rootObject)
    {
        var start = value.IndexOf('{');
        if (start < 0)
        {
            rootObject = string.Empty;
            return false;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = start; index < value.Length; index++)
        {
            var character = value[index];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') inString = false;
                continue;
            }

            if (character == '"') inString = true;
            else if (character == '{') depth++;
            else if (character == '}' && --depth == 0)
            {
                rootObject = value[start..(index + 1)];
                return true;
            }
        }

        rootObject = string.Empty;
        return false;
    }

    private static string StripMarkdownFence(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (!candidate.StartsWith("```", StringComparison.Ordinal))
        {
            return candidate;
        }

        var firstNewLine = candidate.IndexOf('\n');
        if (firstNewLine < 0)
        {
            return candidate;
        }

        candidate = candidate[(firstNewLine + 1)..];
        if (candidate.TrimEnd().EndsWith("```", StringComparison.Ordinal))
        {
            candidate = candidate.TrimEnd();
            candidate = candidate[..^3];
        }
        return candidate.Trim();
    }

    private static JdMatchingRecoveredOutput Invalid(string code, bool wasTruncated) =>
        new(null, IsCompleteJson: false, wasTruncated, new[] { code });
}
