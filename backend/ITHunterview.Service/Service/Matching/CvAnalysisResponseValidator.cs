using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Owns only the safe envelope gates for CV analysis. Local field degradation
/// is delegated to the conservative projector and results in PARTIAL whenever
/// matching-relevant content survives.
/// </summary>
public sealed class CvAnalysisResponseValidator : ICvAnalysisResponseValidator
{
    private const int MaxProviderCharacters = 1_000_000;
    private const int MaxDepth = 32;
    private readonly CvAnalysisDocumentProjector _projector;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };

    public CvAnalysisResponseValidator()
        : this(new CvAnalysisDocumentProjector())
    {
    }

    public CvAnalysisResponseValidator(CvAnalysisDocumentProjector projector)
    {
        _projector = projector;
    }

    public CvAnalysisValidationResult ValidateAndCanonicalize(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return CvAnalysisValidationResult.Invalid(
                "CV_ANALYSIS_EMPTY_OUTPUT",
                "EMPTY_MODEL_OUTPUT",
                "$");
        }
        if (responseJson.Length > MaxProviderCharacters)
        {
            return CvAnalysisValidationResult.Invalid(
                "CV_ANALYSIS_PAYLOAD_UNSAFE",
                "PAYLOAD_TOO_LARGE",
                "$");
        }

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(responseJson, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaxDepth
            });
        }
        catch (JsonException exception)
        {
            return CvAnalysisValidationResult.Invalid(
                "CV_ANALYSIS_INVALID_JSON",
                "JSON_PARSE_FAILED",
                exception.Path ?? "$");
        }

        using (json)
        {
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return CvAnalysisValidationResult.Invalid(
                    "CV_ANALYSIS_SCHEMA_INVALID",
                    "ROOT_NOT_OBJECT",
                    "$");
            }
            if (!root.TryGetProperty("schema_version", out var schema) || schema.ValueKind != JsonValueKind.String)
            {
                return CvAnalysisValidationResult.Invalid(
                    "CV_ANALYSIS_SCHEMA_INVALID",
                    "SCHEMA_VERSION_MISSING",
                    "$.schema_version");
            }
            if (!string.Equals(schema.GetString(), CvAnalysisPromptContract.OutputSchemaVersion, StringComparison.Ordinal))
            {
                return CvAnalysisValidationResult.Invalid(
                    "CV_ANALYSIS_SCHEMA_UNSUPPORTED",
                    "SCHEMA_VERSION_UNSUPPORTED",
                    "$.schema_version");
            }

            var projection = _projector.Project(root);
            if (!projection.HasUsableMatchingContent)
            {
                return CvAnalysisValidationResult.Invalid(
                    "CV_ANALYSIS_CONTENT_EMPTY",
                    "NO_USABLE_MATCHING_CONTENT",
                    "$");
            }

            var coverage = projection.Coverage;
            var diagnostics = projection.Diagnostics.ToList();
            PreservePriorPartialMetadata(root, ref coverage, diagnostics);
            diagnostics = diagnostics
                .DistinctBy(value => (value.Code, value.JsonPath))
                .OrderBy(value => value.JsonPath, StringComparer.Ordinal)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .Take(100)
                .ToList();

            var quality = diagnostics.Count == 0
                ? CvAnalysisQuality.COMPLETE
                : CvAnalysisQuality.PARTIAL;
            projection.Document.AnalysisQuality = quality;
            projection.Document.AnalysisCoverage = coverage;
            projection.Document.AnalysisDiagnostics = diagnostics;
            var canonicalJson = JsonSerializer.Serialize(projection.Document, SerializerOptions);

            return quality == CvAnalysisQuality.COMPLETE
                ? CvAnalysisValidationResult.Complete(canonicalJson, coverage)
                : CvAnalysisValidationResult.Partial(canonicalJson, coverage, diagnostics);
        }
    }

    private static void PreservePriorPartialMetadata(
        JsonElement root,
        ref CvAnalysisCoverage coverage,
        List<CvAnalysisDiagnostic> diagnostics)
    {
        if (!root.TryGetProperty("analysis_quality", out var quality) ||
            quality.ValueKind != JsonValueKind.String ||
            !string.Equals(quality.GetString(), nameof(CvAnalysisQuality.PARTIAL), StringComparison.Ordinal))
        {
            return;
        }

        if (root.TryGetProperty("analysis_coverage", out var coverageElement) && coverageElement.ValueKind == JsonValueKind.Object)
        {
            try
            {
                coverage = coverageElement.Deserialize<CvAnalysisCoverage>(SerializerOptions) ?? coverage;
            }
            catch (JsonException)
            {
                // Metadata never makes an otherwise usable document invalid.
            }
        }

        if (!root.TryGetProperty("analysis_diagnostics", out var diagnosticArray) || diagnosticArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        foreach (var item in diagnosticArray.EnumerateArray())
        {
            if (diagnostics.Count >= 100 || item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String ||
                !item.TryGetProperty("json_path", out var path) || path.ValueKind != JsonValueKind.String)
            {
                continue;
            }
            var boundedCode = code.GetString()?.Trim();
            var boundedPath = path.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(boundedCode) && boundedCode.Length <= 100 &&
                !string.IsNullOrWhiteSpace(boundedPath) && boundedPath.Length <= 300)
            {
                diagnostics.Add(new CvAnalysisDiagnostic(boundedCode, boundedPath));
            }
        }
    }
}
