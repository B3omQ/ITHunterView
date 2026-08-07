using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Provides a structural recovery boundary for CV model output. It never
/// invents fields or closes an incomplete token; it only retains JSON values
/// whose tokens were fully completed by the provider.
/// </summary>
public static class CvAnalysisOutputRecovery
{
    private const string SupportedSchema = "cv-analysis/v2";
    private const int MaxDiagnostics = 100;
    private const int MaxProviderCharacters = 1_000_000;

    private static readonly JsonDocumentOptions StrictDocumentOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32
    };

    private static readonly JsonDocumentOptions TolerantDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32
    };

    private static readonly JsonSerializerOptions EnvelopeSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    private static readonly JsonReaderOptions RecoveryReaderOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32
    };

    private static readonly HashSet<string> ObjectArrayPaths = new(StringComparer.Ordinal)
    {
        "$.verbatim_sections.education[]",
        "$.verbatim_sections.languages[]",
        "$.verbatim_sections.professional_experience_and_projects[]",
        "$.matching_evidence.requirement_signals[]",
        "$.matching_evidence.experience_summary.periods[]",
        "$.matching_evidence.seniority_signals[]"
    };

    private static readonly HashSet<string> SingularObjectPaths = new(StringComparer.Ordinal)
    {
        "$.verbatim_sections.personal_info"
    };

    private static readonly HashSet<string> ScalarPaths = new(StringComparer.Ordinal)
    {
        "$.schema_version",
        "$.verbatim_sections.other_information",
        "$.matching_metrics.total_years_exp",
        "$.matching_evidence.experience_summary.total_professional_months",
        "$.matching_evidence.experience_summary.calculation_basis"
    };

    private static readonly HashSet<string> StringArrayPaths = new(StringComparer.Ordinal)
    {
        "$.verbatim_sections.skills_section[]",
        "$.verbatim_sections.certifications_and_awards[]",
        "$.matching_metrics.job_titles_normalized[]",
        "$.matching_metrics.skills_normalized[]",
        "$.matching_metrics.domains[]"
    };

    private static readonly HashSet<string> TrackableArrayPaths = new(StringComparer.Ordinal)
    {
        "$.verbatim_sections.education",
        "$.verbatim_sections.languages",
        "$.verbatim_sections.skills_section",
        "$.verbatim_sections.professional_experience_and_projects",
        "$.verbatim_sections.certifications_and_awards",
        "$.matching_metrics.job_titles_normalized",
        "$.matching_metrics.skills_normalized",
        "$.matching_metrics.domains",
        "$.matching_evidence.requirement_signals",
        "$.matching_evidence.experience_summary.periods",
        "$.matching_evidence.seniority_signals"
    };

    public static CvAnalysisRecoveryResult Recover(string? providerOutput)
    {
        var candidate = StripMarkdownFence(providerOutput);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Invalid("EMPTY_MODEL_OUTPUT", "$", wasTruncated: false);
        }

        if (candidate.Length > MaxProviderCharacters)
        {
            return Invalid("PAYLOAD_TOO_LARGE", "$", wasTruncated: false);
        }

        if (TryParseStrict(candidate, out var strictDocument))
        {
            using (strictDocument)
            {
                return ValidateCompleteDocument(candidate, strictDocument.RootElement);
            }
        }

        if (TryParseTolerantAndNormalize(candidate, out var normalized, out var normalizedRoot))
        {
            using (normalizedRoot)
            {
                return ValidateCompleteDocument(normalized, normalizedRoot.RootElement,
                    CvAnalysisRecoveryMode.NORMALIZED_JSON);
            }
        }

        if (TryExtractBalancedRootObject(candidate, out var rootObject) &&
            TryParseTolerantAndNormalize(rootObject, out var extracted, out var extractedRoot))
        {
            using (extractedRoot)
            {
                var extractedResult = ValidateCompleteDocument(extracted, extractedRoot.RootElement,
                    CvAnalysisRecoveryMode.EXTRACTED_COMPLETE_OBJECT);
                if (!extractedResult.Diagnostics.Any(x => x.Code == "SCHEMA_VERSION_MISSING"))
                {
                    return extractedResult;
                }
            }
        }

        return RecoverCompletedUnits(candidate);
    }

    private static CvAnalysisRecoveryResult ValidateCompleteDocument(
        string json,
        JsonElement root,
        CvAnalysisRecoveryMode mode = CvAnalysisRecoveryMode.COMPLETE_JSON)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Invalid("ROOT_NOT_OBJECT", "$", wasTruncated: false);
        }

        if (!root.TryGetProperty("schema_version", out var schema) ||
            schema.ValueKind != JsonValueKind.String)
        {
            return Invalid("SCHEMA_VERSION_MISSING", "$.schema_version", wasTruncated: false);
        }

        if (!string.Equals(schema.GetString(), SupportedSchema, StringComparison.Ordinal))
        {
            return Invalid("SCHEMA_VERSION_UNSUPPORTED", "$.schema_version", wasTruncated: false);
        }

        return new CvAnalysisRecoveryResult(
            mode,
            WasTruncated: false,
            json,
            Coverage: null,
            Diagnostics: Array.Empty<CvAnalysisDiagnostic>());
    }

    private static CvAnalysisRecoveryResult RecoverCompletedUnits(string candidate)
    {
        foreach (var start in FindObjectStarts(candidate))
        {
            var jsonCandidate = candidate[start..];
            var collected = CollectCompletedTokens(jsonCandidate);
            if (collected.SchemaVersion is null)
            {
                continue;
            }

            if (!string.Equals(collected.SchemaVersion, SupportedSchema, StringComparison.Ordinal))
            {
                return Invalid("SCHEMA_VERSION_UNSUPPORTED", "$.schema_version", wasTruncated: true);
            }

            if (!collected.HasUsableContent)
            {
                continue;
            }

            var diagnostics = new List<CvAnalysisDiagnostic>
            {
                new("OUTPUT_TRUNCATED", "$"),
                new("RECOVERED_COMPLETE_CV_CONTENT", "$")
            };

            var coverage = collected.BuildCoverage();
            var json = collected.BuildEnvelope(coverage, diagnostics);
            return new CvAnalysisRecoveryResult(
                CvAnalysisRecoveryMode.RECOVERED_PARTIAL,
                WasTruncated: true,
                json,
                coverage,
                diagnostics);
        }

        return Invalid("JSON_PARSE_FAILED", "$", wasTruncated: true);
    }

    private static bool TryParseStrict(string value, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(value, StrictDocumentOptions);
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }
    }

    private static bool TryParseTolerantAndNormalize(
        string value,
        out string normalized,
        out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(value, TolerantDocumentOptions);
            normalized = JsonSerializer.Serialize(document.RootElement);
            return true;
        }
        catch (JsonException)
        {
            normalized = string.Empty;
            document = null!;
            return false;
        }
    }

    private static bool TryExtractBalancedRootObject(string value, out string rootObject)
    {
        foreach (var start in FindObjectStarts(value))
        {
            var depth = 0;
            var inString = false;
            var escaped = false;
            for (var index = start; index < value.Length; index++)
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
                }
                else if (character == '{')
                {
                    depth++;
                }
                else if (character == '}' && --depth == 0)
                {
                    rootObject = value[start..(index + 1)];
                    return true;
                }
            }
        }

        rootObject = string.Empty;
        return false;
    }

    private static IEnumerable<int> FindObjectStarts(string value)
    {
        var inString = false;
        var escaped = false;
        for (var index = 0; index < value.Length; index++)
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
            }
            else if (character == '"')
            {
                inString = true;
            }
            else if (character == '{')
            {
                yield return index;
            }
        }
    }

    private static string StripMarkdownFence(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (!candidate.StartsWith("```", StringComparison.Ordinal) ||
            !candidate.EndsWith("```", StringComparison.Ordinal))
        {
            return candidate;
        }

        var firstNewLine = candidate.IndexOf('\n');
        if (firstNewLine < 0)
        {
            return candidate;
        }

        return candidate[(firstNewLine + 1)..^3].Trim();
    }

    private static CvAnalysisRecoveryResult Invalid(string code, string path, bool wasTruncated) =>
        new(
            CvAnalysisRecoveryMode.INVALID,
            wasTruncated,
            Json: null,
            Coverage: null,
            Diagnostics: new[] { new CvAnalysisDiagnostic(code, path) });

    private sealed class TokenCollection
    {
        private readonly Dictionary<string, JsonElement> _objects = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<JsonElement>> _arrayValues = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonElement> _scalars = new(StringComparer.Ordinal);
        private readonly HashSet<string> _completedArrays = new(StringComparer.Ordinal);

        public string? SchemaVersion =>
            _scalars.TryGetValue("$.schema_version", out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        public int InputExperienceCount { get; private set; }
        public int AcceptedExperienceCount { get; private set; }
        public int InputRequirementSignalCount { get; private set; }
        public int AcceptedRequirementSignalCount { get; private set; }
        public int InputExperiencePeriodCount { get; private set; }
        public int AcceptedExperiencePeriodCount { get; private set; }

        public bool TitleMetricsAvailable =>
            _completedArrays.Contains("$.matching_metrics.job_titles_normalized") ||
            _arrayValues.ContainsKey("$.matching_metrics.job_titles_normalized[]");

        public bool SkillMetricsAvailable =>
            _completedArrays.Contains("$.matching_metrics.skills_normalized") ||
            _arrayValues.ContainsKey("$.matching_metrics.skills_normalized[]");

        public bool ExperienceMetricAvailable => _scalars.ContainsKey("$.matching_metrics.total_years_exp");

        public bool DomainMetricsAvailable =>
            _completedArrays.Contains("$.matching_metrics.domains") ||
            _arrayValues.ContainsKey("$.matching_metrics.domains[]");

        public bool HasUsableContent =>
            HasNonEmptyPersonalContent() ||
            HasAnyArrayValue("$.verbatim_sections.education[]") ||
            HasAnyArrayValue("$.verbatim_sections.languages[]") ||
            HasAnyArrayValue("$.verbatim_sections.skills_section[]") ||
            HasAnyArrayValue("$.verbatim_sections.professional_experience_and_projects[]") ||
            HasAnyArrayValue("$.verbatim_sections.certifications_and_awards[]") ||
            HasAnyArrayValue("$.matching_metrics.job_titles_normalized[]") ||
            HasAnyArrayValue("$.matching_metrics.skills_normalized[]") ||
            HasAnyArrayValue("$.matching_metrics.domains[]") ||
            HasAnyArrayValue("$.matching_evidence.requirement_signals[]") ||
            HasAnyArrayValue("$.matching_evidence.experience_summary.periods[]") ||
            HasAnyArrayValue("$.matching_evidence.seniority_signals[]") ||
            _scalars.ContainsKey("$.matching_metrics.total_years_exp") ||
            _scalars.ContainsKey("$.verbatim_sections.other_information");

        public void AddObject(string path, JsonElement value)
        {
            if (SingularObjectPaths.Contains(path))
            {
                _objects[path] = value;
            }
            else if (ObjectArrayPaths.Contains(path))
            {
                AddArrayValue(path, value);
                if (path == "$.verbatim_sections.professional_experience_and_projects[]")
                {
                    AcceptedExperienceCount++;
                }
                else if (path == "$.matching_evidence.requirement_signals[]")
                {
                    AcceptedRequirementSignalCount++;
                }
                else if (path == "$.matching_evidence.experience_summary.periods[]")
                {
                    AcceptedRequirementSignalCount += 0;
                    AcceptedExperiencePeriodCount++;
                }
            }
        }

        public void AddScalar(string path, JsonElement value)
        {
            if (ScalarPaths.Contains(path) || path == "$.schema_version")
            {
                _scalars[path] = value;
            }
            else if (StringArrayPaths.Contains(path))
            {
                AddArrayValue(path, value);
            }
        }

        public void ObserveObjectStart(string path)
        {
            if (path == "$.verbatim_sections.professional_experience_and_projects[]")
            {
                InputExperienceCount++;
            }
            else if (path == "$.matching_evidence.requirement_signals[]")
            {
                InputRequirementSignalCount++;
            }
            else if (path == "$.matching_evidence.experience_summary.periods[]")
            {
                InputExperiencePeriodCount++;
            }
        }

        public void MarkArrayComplete(string path) => _completedArrays.Add(path);

        public CvAnalysisCoverage BuildCoverage() => new(
            InputExperienceCount,
            AcceptedExperienceCount,
            Math.Max(0, InputExperienceCount - AcceptedExperienceCount),
            InputRequirementSignalCount,
            AcceptedRequirementSignalCount,
            Math.Max(0, InputRequirementSignalCount - AcceptedRequirementSignalCount),
            InputExperiencePeriodCount,
            AcceptedExperiencePeriodCount,
            Math.Max(0, InputExperiencePeriodCount - AcceptedExperiencePeriodCount),
            TitleMetricsAvailable,
            SkillMetricsAvailable,
            ExperienceMetricAvailable,
            DomainMetricsAvailable);

        public string BuildEnvelope(CvAnalysisCoverage coverage, IReadOnlyList<CvAnalysisDiagnostic> diagnostics)
        {
            var personalInfo = _objects.TryGetValue("$.verbatim_sections.personal_info", out var personal)
                ? personal
                : JsonSerializer.SerializeToElement(new { name = "", title = "", summary = "" });

            var verbatim = new Dictionary<string, object?>
            {
                ["personal_info"] = personalInfo,
                ["education"] = Values("$.verbatim_sections.education[]"),
                ["languages"] = Values("$.verbatim_sections.languages[]"),
                ["skills_section"] = Values("$.verbatim_sections.skills_section[]"),
                ["professional_experience_and_projects"] = Values("$.verbatim_sections.professional_experience_and_projects[]"),
                ["certifications_and_awards"] = Values("$.verbatim_sections.certifications_and_awards[]"),
                ["other_information"] = ScalarOrNull("$.verbatim_sections.other_information")
            };

            var metrics = new Dictionary<string, object?>
            {
                ["job_titles_normalized"] = Values("$.matching_metrics.job_titles_normalized[]"),
                ["skills_normalized"] = Values("$.matching_metrics.skills_normalized[]"),
                ["total_years_exp"] = ScalarOrNull("$.matching_metrics.total_years_exp"),
                ["domains"] = Values("$.matching_metrics.domains[]")
            };

            var summary = _objects.TryGetValue("$.matching_evidence.experience_summary", out var summaryObject)
                ? summaryObject
                : JsonSerializer.SerializeToElement(new Dictionary<string, object?>
                {
                    ["total_professional_months"] = ScalarOrNull("$.matching_evidence.experience_summary.total_professional_months"),
                    ["calculation_basis"] = ScalarOrNull("$.matching_evidence.experience_summary.calculation_basis") is { } basis
                        ? basis
                        : "insufficient_timeline",
                    ["periods"] = Values("$.matching_evidence.experience_summary.periods[]")
                });

            var evidence = new Dictionary<string, object?>
            {
                ["requirement_signals"] = Values("$.matching_evidence.requirement_signals[]"),
                ["experience_summary"] = summary,
                ["seniority_signals"] = Values("$.matching_evidence.seniority_signals[]")
            };

            var envelope = new Dictionary<string, object?>
            {
                ["schema_version"] = SupportedSchema,
                ["analysis_quality"] = "PARTIAL",
                ["analysis_coverage"] = coverage,
                ["analysis_diagnostics"] = diagnostics.Take(MaxDiagnostics).ToArray(),
                ["verbatim_sections"] = verbatim,
                ["matching_metrics"] = metrics,
                ["matching_evidence"] = evidence
            };

            return JsonSerializer.Serialize(envelope, EnvelopeSerializerOptions);
        }

        private bool HasNonEmptyPersonalContent()
        {
            if (!_objects.TryGetValue("$.verbatim_sections.personal_info", out var value) ||
                value.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            return (value.TryGetProperty("title", out var title) &&
                    title.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(title.GetString())) ||
                   (value.TryGetProperty("summary", out var summary) &&
                    summary.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(summary.GetString()));
        }

        private bool HasAnyArrayValue(string path) => _arrayValues.TryGetValue(path, out var values) && values.Count > 0;

        private List<JsonElement> Values(string path) =>
            _arrayValues.TryGetValue(path, out var values) ? values : new List<JsonElement>();

        private JsonElement? ScalarOrNull(string path) =>
            _scalars.TryGetValue(path, out var value) ? value : null;

        private void AddArrayValue(string path, JsonElement value)
        {
            if (!_arrayValues.TryGetValue(path, out var values))
            {
                values = new List<JsonElement>();
                _arrayValues[path] = values;
            }

            if (values.Count < ArrayCap(path))
            {
                values.Add(value);
            }
        }

        private static int ArrayCap(string path) => path switch
        {
            "$.verbatim_sections.education[]" => 20,
            "$.verbatim_sections.languages[]" => 20,
            "$.verbatim_sections.skills_section[]" => 80,
            "$.verbatim_sections.professional_experience_and_projects[]" => 30,
            "$.verbatim_sections.certifications_and_awards[]" => 40,
            "$.matching_metrics.job_titles_normalized[]" => 40,
            "$.matching_metrics.skills_normalized[]" => 100,
            "$.matching_metrics.domains[]" => 40,
            "$.matching_evidence.requirement_signals[]" => 100,
            "$.matching_evidence.experience_summary.periods[]" => 30,
            "$.matching_evidence.seniority_signals[]" => 40,
            _ => 100
        };
    }

    private sealed class ContainerFrame(string path, bool isObject, long start)
    {
        public string Path { get; } = path;
        public bool IsObject { get; } = isObject;
        public long Start { get; } = start;
        public string? PendingProperty { get; set; }
    }

    private static TokenCollection CollectCompletedTokens(string candidate)
    {
        var collection = new TokenCollection();
        var bytes = Encoding.UTF8.GetBytes(candidate);
        var stack = new Stack<ContainerFrame>();
        var reader = new Utf8JsonReader(
            bytes,
            isFinalBlock: true,
            state: new JsonReaderState(RecoveryReaderOptions));

        try
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (stack.TryPeek(out var owner) && owner.IsObject)
                        {
                            owner.PendingProperty = reader.GetString();
                        }
                        break;

                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                    {
                        var path = ValuePath(stack);
                        var frame = new ContainerFrame(
                            path,
                            reader.TokenType == JsonTokenType.StartObject,
                            reader.TokenStartIndex);
                        if (reader.TokenType == JsonTokenType.StartObject && ObjectArrayPaths.Contains(path))
                        {
                            collection.ObserveObjectStart(path);
                        }
                        stack.Push(frame);
                        break;
                    }

                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                    {
                        if (!stack.TryPop(out var frame))
                        {
                            break;
                        }

                        if (reader.TokenType == JsonTokenType.EndArray && TrackableArrayPaths.Contains(frame.Path))
                        {
                            collection.MarkArrayComplete(frame.Path);
                        }

                        var length = reader.BytesConsumed - frame.Start;
                        if (length <= 0 || frame.Start < 0 || frame.Start + length > bytes.Length)
                        {
                            break;
                        }

                        var element = ParseElement(bytes, frame.Start, length);
                        if (element is null)
                        {
                            break;
                        }

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            collection.AddObject(frame.Path, element.Value);
                        }
                        break;
                    }

                    case JsonTokenType.String:
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                    case JsonTokenType.Null:
                        collection.AddScalar(ValuePath(stack),
                            JsonSerializer.SerializeToElement(ReadScalar(reader)));
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // Tokens that were closed before the truncation remain in collection.
        }

        return collection;
    }

    private static object? ReadScalar(Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.String => reader.GetString(),
        JsonTokenType.Number when reader.TryGetInt32(out var integer) => integer,
        JsonTokenType.Number => reader.GetDouble(),
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        _ => null
    };

    private static JsonElement? ParseElement(byte[] bytes, long start, long length)
    {
        try
        {
            if (start > int.MaxValue || length > int.MaxValue)
            {
                return null;
            }

            using var document = JsonDocument.Parse(bytes.AsMemory((int)start, (int)length), StrictDocumentOptions);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ValuePath(Stack<ContainerFrame> stack)
    {
        if (!stack.TryPeek(out var owner))
        {
            return "$";
        }

        return owner.IsObject
            ? $"{owner.Path}.{owner.PendingProperty ?? string.Empty}"
            : $"{owner.Path}[]";
    }
}
