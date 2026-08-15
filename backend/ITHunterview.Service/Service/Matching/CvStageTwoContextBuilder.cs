using System.Text.Json;
using System.Text.Json.Nodes;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed record CvStageTwoContext(
    string Json,
    CvAnalysisQuality Quality,
    CvAnalysisCoverage? Coverage,
    IReadOnlyList<CvAnalysisDiagnostic> Diagnostics);

/// <summary>
/// Builds the Stage 2 view from stored canonical CV JSON without clipping,
/// deduplicating, or normalizing provider-owned content.
/// </summary>
public sealed class CvStageTwoContextBuilder
{
    public const string InvalidCvMatchingContext = "INVALID_CV_MATCHING_CONTEXT";
    private readonly CvAnalysisResponseValidator _validator;

    private static readonly JsonSerializerOptions MetadataOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public CvStageTwoContextBuilder()
        : this(new CvAnalysisResponseValidator())
    {
    }

    public CvStageTwoContextBuilder(CvAnalysisResponseValidator validator)
    {
        _validator = validator;
    }

    public CvStageTwoContext Build(string canonicalCvJson)
    {
        if (string.IsNullOrWhiteSpace(canonicalCvJson))
        {
            throw Invalid();
        }

        try
        {
            var validation = _validator.ValidateStoredCanonical(canonicalCvJson);
            if (!validation.IsUsable)
            {
                throw Invalid();
            }

            var root = JsonNode.Parse(validation.CanonicalJson)?.AsObject() ?? throw Invalid();
            if (root["schema_version"]?.GetValue<string>() != "cv-analysis/v2")
            {
                throw Invalid();
            }

            var candidate = root["verbatim_sections"]?.DeepClone() as JsonObject ?? new JsonObject();
            if (candidate["personal_info"] is JsonObject personalInfo)
            {
                personalInfo.Remove("name");
            }

            var warningCodes = new JsonArray(validation.Diagnostics
                .Select(diagnostic => diagnostic.Code)
                .Distinct(StringComparer.Ordinal)
                .Take(20)
                .Select(code => (JsonNode?)JsonValue.Create(code))
                .ToArray());

            var analysis = new JsonObject
            {
                ["quality"] = validation.Quality.ToString(),
                ["coverage"] = validation.Coverage is null
                    ? null
                    : JsonSerializer.SerializeToNode(validation.Coverage, MetadataOptions),
                ["warning_codes"] = warningCodes
            };

            var context = new JsonObject
            {
                ["schema_version"] = "matching-context/v1",
                ["source_cv_schema_version"] = "cv-analysis/v2",
                ["cv_analysis"] = analysis,
                ["candidate"] = candidate,
                ["matching_metrics"] = root["matching_metrics"]?.DeepClone() ?? new JsonObject(),
                ["matching_evidence"] = root["matching_evidence"]?.DeepClone() ?? new JsonObject()
            };

            return new CvStageTwoContext(
                context.ToJsonString(),
                validation.Quality,
                validation.Coverage,
                validation.Diagnostics);
        }
        catch (InvalidOperationException exception) when (exception.Message == InvalidCvMatchingContext)
        {
            throw;
        }
        catch (Exception)
        {
            throw Invalid();
        }
    }

    private static InvalidOperationException Invalid() => new(InvalidCvMatchingContext);
}
