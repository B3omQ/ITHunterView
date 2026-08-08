using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Utils;

public static class CvAnalysisMetadataReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static CvAnalysisQuality? ReadQuality(string? canonicalJson)
    {
        if (!TryParseObject(canonicalJson, out var document)) return null;
        using (document)
        {
            if (!document.RootElement.TryGetProperty("analysis_quality", out var value) || value.ValueKind != JsonValueKind.String)
                return null;
            return Enum.TryParse<CvAnalysisQuality>(value.GetString(), ignoreCase: false, out var quality)
                ? quality
                : null;
        }
    }

    public static CvAnalysisCoverage? ReadCoverage(string? canonicalJson)
    {
        if (!TryParseObject(canonicalJson, out var document)) return null;
        using (document)
        {
            if (!document.RootElement.TryGetProperty("analysis_coverage", out var value) || value.ValueKind != JsonValueKind.Object)
                return null;
            return DeserializeCoverage(value);
        }
    }

    public static List<CvAnalysisDiagnostic> ReadDiagnostics(string? canonicalJson)
    {
        if (!TryParseObject(canonicalJson, out var document)) return new List<CvAnalysisDiagnostic>();
        using (document)
        {
            return document.RootElement.TryGetProperty("analysis_diagnostics", out var value)
                ? ReadDiagnosticArray(value)
                : new List<CvAnalysisDiagnostic>();
        }
    }

    public static CvAnalysisCoverage? ReadCoverageJson(string? json)
    {
        if (!TryParseObject(json, out var document)) return null;
        using (document)
        {
            return DeserializeCoverage(document.RootElement);
        }
    }

    public static List<CvAnalysisDiagnostic> ReadDiagnosticsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<CvAnalysisDiagnostic>();
        try
        {
            using var document = JsonDocument.Parse(json);
            return ReadDiagnosticArray(document.RootElement);
        }
        catch (JsonException)
        {
            return new List<CvAnalysisDiagnostic>();
        }
    }

    public static string? SerializeCoverage(CvAnalysisCoverage? coverage) =>
        coverage is null ? null : JsonSerializer.Serialize(coverage, Options);

    public static string? SerializeDiagnostics(IReadOnlyCollection<CvAnalysisDiagnostic>? diagnostics) =>
        diagnostics is null ? null : JsonSerializer.Serialize(diagnostics.Take(100), Options);

    private static CvAnalysisCoverage? DeserializeCoverage(JsonElement value)
    {
        try
        {
            return value.Deserialize<CvAnalysisCoverage>(Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<CvAnalysisDiagnostic> ReadDiagnosticArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array) return new List<CvAnalysisDiagnostic>();
        var result = new List<CvAnalysisDiagnostic>();
        foreach (var item in value.EnumerateArray())
        {
            if (result.Count >= 100 || item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String ||
                !item.TryGetProperty("json_path", out var path) || path.ValueKind != JsonValueKind.String)
                continue;
            var codeValue = code.GetString()?.Trim();
            var pathValue = path.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(codeValue) || codeValue.Length > 100 ||
                string.IsNullOrWhiteSpace(pathValue) || pathValue.Length > 300)
                continue;
            if (result.Any(existing => existing.Code == codeValue && existing.JsonPath == pathValue))
                continue;
            result.Add(new CvAnalysisDiagnostic(codeValue, pathValue));
        }
        return result;
    }

    private static bool TryParseObject(string? json, out JsonDocument document)
    {
        document = null!;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            if (document.RootElement.ValueKind == JsonValueKind.Object) return true;
            document.Dispose();
            document = null!;
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
