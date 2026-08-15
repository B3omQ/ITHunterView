using System.Text.Json;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Owns document-level safety/contract gates and backend metadata. It never
/// rewrites provider-owned CV fields.
/// </summary>
public sealed class CvAnalysisResponseValidator : ICvAnalysisResponseValidator, ICvAnalysisRecoveryAwareValidator
{
    private const int MaxProviderCharacters = 1_000_000;
    private const int MaxDepth = 32;
    private const int MaxDiagnostics = 100;
    private readonly CvAnalysisDocumentProjector _projector;

    public CvAnalysisResponseValidator()
        : this(new CvAnalysisDocumentProjector())
    {
    }

    public CvAnalysisResponseValidator(CvAnalysisDocumentProjector projector)
    {
        _projector = projector;
    }

    public CvAnalysisValidationResult ValidateAndCanonicalize(string responseJson) =>
        Validate(responseJson, new CvAnalysisValidationContext(CvAnalysisValidationOrigin.ProviderOutput));

    public CvAnalysisValidationResult ValidateRecovered(CvAnalysisRecoveryResult recovery)
    {
        if (!recovery.HasCandidateJson)
        {
            var diagnostic = recovery.Diagnostics.FirstOrDefault() ?? new CvAnalysisDiagnostic("JSON_PARSE_FAILED", "$");
            return CvAnalysisValidationResult.Invalid(
                diagnostic.Code == "EMPTY_MODEL_OUTPUT" ? "CV_ANALYSIS_EMPTY_OUTPUT" : "CV_ANALYSIS_INVALID_JSON",
                diagnostic.Code,
                diagnostic.JsonPath);
        }

        return Validate(recovery.Json!, new CvAnalysisValidationContext(
            CvAnalysisValidationOrigin.RecoveredProviderOutput,
            recovery.WasTruncated,
            recovery.Mode,
            recovery.Coverage,
            recovery.Diagnostics));
    }

    public CvAnalysisValidationResult ValidateStoredCanonical(string canonicalJson) =>
        Validate(canonicalJson, ReadStoredContext(canonicalJson));

    private CvAnalysisValidationResult Validate(string responseJson, CvAnalysisValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return CvAnalysisValidationResult.Invalid("CV_ANALYSIS_EMPTY_OUTPUT", "EMPTY_MODEL_OUTPUT", "$");
        }
        if (responseJson.Length > MaxProviderCharacters)
        {
            return CvAnalysisValidationResult.Invalid("CV_ANALYSIS_PAYLOAD_UNSAFE", "PAYLOAD_TOO_LARGE", "$");
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
                return CvAnalysisValidationResult.Invalid("CV_ANALYSIS_SCHEMA_INVALID", "ROOT_NOT_OBJECT", "$");
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

            var coverage = MergeCoverage(projection.Coverage, context.RecoveryCoverage);
            var diagnostics = projection.Diagnostics
                .Concat(context.RecoveryDiagnostics ?? Array.Empty<CvAnalysisDiagnostic>())
                .Where(value => !string.IsNullOrWhiteSpace(value.Code) && !string.IsNullOrWhiteSpace(value.JsonPath))
                .Select(value => new CvAnalysisDiagnostic(
                    value.Code.Trim().Length <= 100 ? value.Code.Trim() : value.Code.Trim()[..100],
                    value.JsonPath.Trim().Length <= 300 ? value.JsonPath.Trim() : value.JsonPath.Trim()[..300]))
                .DistinctBy(value => (value.Code, value.JsonPath))
                .OrderBy(value => value.JsonPath, StringComparer.Ordinal)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .Take(MaxDiagnostics)
                .ToArray();

            var partial = context.WasTruncated || projection.HasStructuralDegradation;
            var quality = partial ? CvAnalysisQuality.PARTIAL : CvAnalysisQuality.COMPLETE;
            var canonicalJson = CvAnalysisCanonicalJsonWriter.Write(
                root,
                quality,
                coverage,
                diagnostics,
                context.WasTruncated,
                context.RecoveryMode);

            return quality == CvAnalysisQuality.COMPLETE
                ? CvAnalysisValidationResult.Complete(canonicalJson, coverage, diagnostics)
                : CvAnalysisValidationResult.Partial(canonicalJson, coverage, diagnostics);
        }
    }

    private static CvAnalysisValidationContext ReadStoredContext(string canonicalJson)
    {
        if (string.IsNullOrWhiteSpace(canonicalJson) || canonicalJson.Length > MaxProviderCharacters)
        {
            return new CvAnalysisValidationContext(CvAnalysisValidationOrigin.StoredCanonical);
        }

        try
        {
            using var document = JsonDocument.Parse(canonicalJson, new JsonDocumentOptions { MaxDepth = MaxDepth });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new CvAnalysisValidationContext(CvAnalysisValidationOrigin.StoredCanonical);
            }

            var wasTruncated = false;
            CvAnalysisRecoveryMode? mode = null;
            if (root.TryGetProperty("analysis_recovery", out var recovery) && recovery.ValueKind == JsonValueKind.Object)
            {
                wasTruncated = recovery.TryGetProperty("was_truncated", out var truncated) &&
                               truncated.ValueKind is JsonValueKind.True or JsonValueKind.False && truncated.GetBoolean();
                if (recovery.TryGetProperty("mode", out var modeElement) && modeElement.ValueKind == JsonValueKind.String &&
                    Enum.TryParse<CvAnalysisRecoveryMode>(modeElement.GetString(), ignoreCase: false, out var parsedMode))
                {
                    mode = parsedMode;
                }
            }

            var storedDiagnostics = new List<CvAnalysisDiagnostic>();
            if (root.TryGetProperty("analysis_diagnostics", out var diagnostics) && diagnostics.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in diagnostics.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object ||
                        !item.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String ||
                        !item.TryGetProperty("json_path", out var path) || path.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var diagnostic = new CvAnalysisDiagnostic(code.GetString() ?? string.Empty, path.GetString() ?? string.Empty);
                    if (diagnostic.Code == "OUTPUT_TRUNCATED")
                    {
                        wasTruncated = true;
                    }
                    storedDiagnostics.Add(diagnostic);
                }
            }

            var recoveryDiagnostics = wasTruncated || mode is not null
                ? storedDiagnostics
                : new List<CvAnalysisDiagnostic>();

            return new CvAnalysisValidationContext(
                CvAnalysisValidationOrigin.StoredCanonical,
                wasTruncated,
                mode,
                RecoveryDiagnostics: recoveryDiagnostics);
        }
        catch (JsonException)
        {
            return new CvAnalysisValidationContext(CvAnalysisValidationOrigin.StoredCanonical);
        }
    }

    private static CvAnalysisCoverage MergeCoverage(
        CvAnalysisCoverage inspected,
        CvAnalysisCoverage? recovered)
    {
        if (recovered is null)
        {
            return inspected;
        }

        var experienceInput = Math.Max(inspected.InputExperienceEntryCount, recovered.InputExperienceEntryCount);
        var signalInput = Math.Max(inspected.InputRequirementSignalCount, recovered.InputRequirementSignalCount);
        var periodInput = Math.Max(inspected.InputExperiencePeriodCount, recovered.InputExperiencePeriodCount);
        return inspected with
        {
            InputExperienceEntryCount = experienceInput,
            DiscardedExperienceEntryCount = Math.Max(0, experienceInput - inspected.AcceptedExperienceEntryCount),
            InputRequirementSignalCount = signalInput,
            DiscardedRequirementSignalCount = Math.Max(0, signalInput - inspected.AcceptedRequirementSignalCount),
            InputExperiencePeriodCount = periodInput,
            DiscardedExperiencePeriodCount = Math.Max(0, periodInput - inspected.AcceptedExperiencePeriodCount)
        };
    }
}
