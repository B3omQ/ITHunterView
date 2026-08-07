using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.DTOs.JobAnalysis;

namespace ITHunterview.Service.Utils;

public static class JdAnalysisMetadataReader
{
    private static readonly JsonSerializerOptions CaseInsensitiveJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string? ReadQuality(string? effectiveJson)
    {
        if (string.IsNullOrWhiteSpace(effectiveJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(effectiveJson);
            return document.RootElement.TryGetProperty("analysis_quality", out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static JdAnalysisCoverage? ReadCoverage(string? effectiveJson)
    {
        if (string.IsNullOrWhiteSpace(effectiveJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(effectiveJson);
            if (!document.RootElement.TryGetProperty("analysis_coverage", out var value) ||
                value.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new JdAnalysisCoverage(
                ReadInt(value, "input_group_count"),
                ReadInt(value, "accepted_group_count"),
                ReadInt(value, "discarded_group_count"),
                ReadInt(value, "input_item_count"),
                ReadInt(value, "accepted_item_count"),
                ReadInt(value, "discarded_item_count"),
                value.TryGetProperty("requirement_set_complete", out var complete) &&
                complete.ValueKind == JsonValueKind.True);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static List<JdAnalysisDiagnostic> ReadDiagnostics(string? effectiveJson)
    {
        var diagnostics = new List<JdAnalysisDiagnostic>();
        if (string.IsNullOrWhiteSpace(effectiveJson)) return diagnostics;
        try
        {
            using var document = JsonDocument.Parse(effectiveJson);
            if (!document.RootElement.TryGetProperty("analysis_diagnostics", out var values) ||
                values.ValueKind != JsonValueKind.Array)
            {
                return diagnostics;
            }

            foreach (var value in values.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.Object ||
                    !value.TryGetProperty("code", out var code) ||
                    !value.TryGetProperty("json_path", out var path) ||
                    code.ValueKind != JsonValueKind.String ||
                    path.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                diagnostics.Add(new JdAnalysisDiagnostic(code.GetString()!, path.GetString()!));
                if (diagnostics.Count == 100) break;
            }
        }
        catch (JsonException)
        {
            // Historical effective JSON predates quality metadata.
        }

        return diagnostics;
    }

    public static JdAnalysisCoverage? ReadCoverageJson(string? coverageJson)
    {
        if (string.IsNullOrWhiteSpace(coverageJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<JdAnalysisCoverage>(coverageJson, CaseInsensitiveJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static List<JdAnalysisDiagnostic> ReadDiagnosticsJson(string? diagnosticsJson)
    {
        if (string.IsNullOrWhiteSpace(diagnosticsJson)) return new List<JdAnalysisDiagnostic>();
        try
        {
            return JsonSerializer.Deserialize<List<JdAnalysisDiagnostic>>(diagnosticsJson, CaseInsensitiveJson)?
                .Take(100)
                .ToList() ?? new List<JdAnalysisDiagnostic>();
        }
        catch (JsonException)
        {
            return new List<JdAnalysisDiagnostic>();
        }
    }

    private static int ReadInt(JsonElement value, string property) =>
        value.TryGetProperty(property, out var element) &&
        element.ValueKind == JsonValueKind.Number &&
        element.TryGetInt32(out var number)
            ? number
            : 0;
}
