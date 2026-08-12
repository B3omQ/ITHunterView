using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.Utils;

/// <summary>
/// Recovers only JSON objects that the provider finished before a response was
/// truncated. It never appends braces, guesses missing fields, or rewrites the
/// meaning of a requirement.
/// </summary>
public static class JdAnalysisOutputRecovery
{
    private const int MaxProviderCharacters = 262_144;
    private const int MaxGroups = 50;
    private const int MaxItems = 100;
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64
    };

    public static JdAnalysisRecoveryResult Recover(string? providerOutput)
    {
        var text = providerOutput?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return JdAnalysisRecoveryResult.Invalid("EMPTY_MODEL_OUTPUT");
        }

        if (text.Length > MaxProviderCharacters)
        {
            return JdAnalysisRecoveryResult.Invalid("PAYLOAD_TOO_LARGE");
        }

        try
        {
            using var document = JsonDocument.Parse(text, DocumentOptions);
            return JdAnalysisRecoveryResult.Complete(text);
        }
        catch (JsonException)
        {
            // Continue with token-boundary recovery below.
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        var groups = new List<JsonElement>();
        var diagnostics = new List<JdAnalysisDiagnostic>
        {
            new("OUTPUT_TRUNCATED", "$")
        };
        var titles = new List<string>();
        var domains = new List<string>();
        var totalYears = 0;
        var inputGroupCount = 0;
        var inputItemCount = 0;
        var acceptedItemCount = 0;
        var depth = 0;
        var metricsDepth = -1;
        var groupsDepth = -1;
        var groupDepth = -1;
        var groupStart = -1;
        var groupArraySeen = false;
        var pendingMatchingMetrics = false;
        var pendingMetricsProperty = string.Empty;
        var prefixArrayProperty = string.Empty;
        var prefixArrayDepth = -1;

        try
        {
            var reader = new Utf8JsonReader(bytes, isFinalBlock: true, new JsonReaderState(new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64
            }));
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                    {
                        var name = reader.GetString() ?? string.Empty;
                        if (depth == 1 && string.Equals(name, "matching_metrics", StringComparison.OrdinalIgnoreCase))
                        {
                            pendingMatchingMetrics = true;
                        }
                        else if (metricsDepth == depth && depth > 0)
                        {
                            pendingMetricsProperty = name;
                        }
                        break;
                    }

                    case JsonTokenType.StartObject:
                        if (pendingMatchingMetrics && depth == 1)
                        {
                            metricsDepth = depth + 1;
                            pendingMatchingMetrics = false;
                        }

                        if (groupArraySeen && depth == groupsDepth)
                        {
                            inputGroupCount++;
                            groupDepth = depth + 1;
                            groupStart = checked((int)reader.TokenStartIndex);
                        }

                        depth++;
                        pendingMetricsProperty = string.Empty;
                        break;

                    case JsonTokenType.EndObject:
                        if (groupStart >= 0 && depth == groupDepth)
                        {
                            var end = checked((int)reader.BytesConsumed);
                            TryAddCompleteGroup(bytes, groupStart, end, groups, diagnostics, ref inputItemCount, ref acceptedItemCount);
                            groupStart = -1;
                            groupDepth = -1;
                        }

                        depth = Math.Max(0, depth - 1);
                        pendingMetricsProperty = string.Empty;
                        break;

                    case JsonTokenType.StartArray:
                        depth++;
                        if (metricsDepth == depth - 1 && string.Equals(pendingMetricsProperty, "requirement_groups", StringComparison.OrdinalIgnoreCase))
                        {
                            groupsDepth = depth;
                            groupArraySeen = true;
                        }
                        else if (metricsDepth == depth - 1 &&
                                 (string.Equals(pendingMetricsProperty, "job_titles_normalized", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(pendingMetricsProperty, "domains", StringComparison.OrdinalIgnoreCase)))
                        {
                            prefixArrayProperty = pendingMetricsProperty;
                            prefixArrayDepth = depth;
                        }

                        pendingMetricsProperty = string.Empty;
                        break;

                    case JsonTokenType.EndArray:
                        if (depth == groupsDepth)
                        {
                            groupArraySeen = false;
                            groupsDepth = -1;
                        }

                        if (depth == prefixArrayDepth)
                        {
                            prefixArrayProperty = string.Empty;
                            prefixArrayDepth = -1;
                        }

                        depth = Math.Max(0, depth - 1);
                        pendingMetricsProperty = string.Empty;
                        break;

                    case JsonTokenType.String:
                        if (depth == prefixArrayDepth)
                        {
                            var value = reader.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                if (string.Equals(prefixArrayProperty, "job_titles_normalized", StringComparison.OrdinalIgnoreCase))
                                {
                                    AddDistinct(titles, value);
                                }
                                else if (string.Equals(prefixArrayProperty, "domains", StringComparison.OrdinalIgnoreCase))
                                {
                                    AddDistinct(domains, value);
                                }
                            }
                        }
                        pendingMetricsProperty = string.Empty;
                        break;

                    case JsonTokenType.Number:
                        if (metricsDepth == depth &&
                            string.Equals(pendingMetricsProperty, "total_years_exp", StringComparison.OrdinalIgnoreCase) &&
                            reader.TryGetInt32(out var years) && years >= 0)
                        {
                            totalYears = years;
                        }

                        pendingMetricsProperty = string.Empty;
                        break;

                    default:
                        pendingMetricsProperty = string.Empty;
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // The reader reached the provider's incomplete token. Complete
            // groups collected before that point remain safe to persist.
        }
        catch (InvalidOperationException)
        {
            // Same safety rule for an invalid UTF-8/token boundary.
        }

        var discardedGroupCount = Math.Max(0, inputGroupCount - groups.Count);
        var discardedItemCount = Math.Max(0, inputItemCount - acceptedItemCount);
        if (groups.Count == 0 || !groupArraySeen && groupsDepth < 0 && inputGroupCount == 0)
        {
            AddDiagnostic(diagnostics, "NO_COMPLETE_GROUP_RECOVERED", "$.matching_metrics.requirement_groups");
            return new JdAnalysisRecoveryResult(
                true,
                null,
                inputGroupCount,
                0,
                discardedGroupCount,
                inputItemCount,
                acceptedItemCount,
                discardedItemCount,
                diagnostics);
        }

        if (discardedGroupCount > 0)
        {
            AddDiagnostic(diagnostics, "INCOMPLETE_REQUIREMENT_GROUP", "$.matching_metrics.requirement_groups");
        }

        AddDiagnostic(diagnostics, "RECOVERED_COMPLETE_GROUPS", "$.matching_metrics.requirement_groups");
        var json = JsonSerializer.Serialize(new
        {
            schema_version = "jd-analysis/v5",
            matching_metrics = new
            {
                job_titles_normalized = titles,
                total_years_exp = totalYears,
                domains,
                requirement_groups = groups
            }
        });

        return new JdAnalysisRecoveryResult(
            true,
            json,
            inputGroupCount,
            groups.Count,
            discardedGroupCount,
            inputItemCount,
            acceptedItemCount,
            discardedItemCount,
            diagnostics);
    }

    private static void TryAddCompleteGroup(
        byte[] bytes,
        int start,
        int end,
        List<JsonElement> groups,
        List<JdAnalysisDiagnostic> diagnostics,
        ref int inputItemCount,
        ref int acceptedItemCount)
    {
        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(bytes.AsMemory(start, end - start), DocumentOptions);
            var group = document.RootElement.Clone();
            var itemCount = group.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
                ? items.GetArrayLength()
                : 0;
            inputItemCount += itemCount;

            if (groups.Count >= MaxGroups)
            {
                AddDiagnostic(diagnostics, "REQUIREMENT_GROUP_LIMIT_EXCEEDED", "$.matching_metrics.requirement_groups");
                return;
            }

            if (acceptedItemCount + itemCount > MaxItems)
            {
                AddDiagnostic(diagnostics, "REQUIREMENT_ITEM_LIMIT_EXCEEDED", "$.matching_metrics.requirement_groups");
                return;
            }

            groups.Add(group);
            acceptedItemCount += itemCount;
        }
        catch (JsonException)
        {
            AddDiagnostic(diagnostics, "INVALID_COMPLETE_GROUP", "$.matching_metrics.requirement_groups");
        }
        finally
        {
            document?.Dispose();
        }
    }

    private static void AddDistinct(List<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value.Trim());
        }
    }

    private static void AddDiagnostic(List<JdAnalysisDiagnostic> diagnostics, string code, string path)
    {
        if (diagnostics.Count < 100 && !diagnostics.Any(item => item.Code == code && item.JsonPath == path))
        {
            diagnostics.Add(new JdAnalysisDiagnostic(code, path));
        }
    }
}

public sealed class JdAnalysisRecoveryResult
{
    public JdAnalysisRecoveryResult(
        bool wasTruncated,
        string? json,
        int inputGroupCount,
        int acceptedGroupCount,
        int discardedGroupCount,
        int inputItemCount,
        int acceptedItemCount,
        int discardedItemCount,
        IReadOnlyList<JdAnalysisDiagnostic> diagnostics)
    {
        WasTruncated = wasTruncated;
        Json = json;
        InputGroupCount = inputGroupCount;
        AcceptedGroupCount = acceptedGroupCount;
        DiscardedGroupCount = discardedGroupCount;
        InputItemCount = inputItemCount;
        AcceptedItemCount = acceptedItemCount;
        DiscardedItemCount = discardedItemCount;
        Diagnostics = diagnostics;
    }

    public bool WasTruncated { get; }
    public string? Json { get; }
    public int InputGroupCount { get; }
    public int AcceptedGroupCount { get; }
    public int DiscardedGroupCount { get; }
    public int InputItemCount { get; }
    public int AcceptedItemCount { get; }
    public int DiscardedItemCount { get; }
    public IReadOnlyList<JdAnalysisDiagnostic> Diagnostics { get; }

    public static JdAnalysisRecoveryResult Complete(string json) =>
        new(false, json, 0, 0, 0, 0, 0, 0, Array.Empty<JdAnalysisDiagnostic>());

    public static JdAnalysisRecoveryResult Invalid(string code) =>
        new(false, null, 0, 0, 0, 0, 0, 0, new[] { new JdAnalysisDiagnostic(code, "$") });
}
