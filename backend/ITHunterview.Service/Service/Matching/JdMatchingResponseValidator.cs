using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Applies only mechanical Stage 2 contract checks. It maps approved handler
/// codes through the backend score policy and never judges evidence meaning.
/// </summary>
public static class JdMatchingResponseValidator
{
    public const string InvalidStageTwoResponse = "INVALID_STAGE_TWO_RESPONSE";
    public const string SchemaVersion = "jd-stage2/v2";

    private const int MaxReasoningLength = 2_000;
    private const int MaxEvidenceItems = 5;
    private const int MaxEvidenceFieldLength = 500;
    private const int MaxNarrativeLength = 4_000;
    private const int MaxTopLevelItems = 100;
    private const int MaxHandlerDiagnostics = 20;

    public static JdStageTwoValidatedResponse Validate(
        JsonDocument response,
        JdRequirementProjection projection,
        bool isCompleteJson = true,
        bool wasTruncated = false,
        IReadOnlyList<string>? recoveryWarnings = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(projection);

        var expectedItems = projection.Groups.SelectMany(group => group.Items).ToArray();
        if (expectedItems.Length is 0 or > MaxTopLevelItems)
        {
            throw new InvalidOperationException(InvalidStageTwoResponse);
        }

        var expectedById = expectedItems
            .Where(item => !string.IsNullOrWhiteSpace(item.ItemId))
            .GroupBy(item => item.ItemId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        if (expectedById.Count != expectedItems.Length)
        {
            throw new InvalidOperationException(InvalidStageTwoResponse);
        }

        var warnings = new HashSet<string>(recoveryWarnings ?? Array.Empty<string>(), StringComparer.Ordinal);
        var root = response.RootElement;
        if (!HasSupportedRoot(root, warnings, out var scoresElement))
        {
            return Invalid(expectedById.Keys, wasTruncated, warnings);
        }

        var inputCount = scoresElement.GetArrayLength();
        var discardedCount = 0;
        if (inputCount > MaxTopLevelItems)
        {
            discardedCount += inputCount - MaxTopLevelItems;
            warnings.Add("SCORE_ITEM_LIMIT_EXCEEDED");
        }

        var accepted = new Dictionary<string, JdStageTwoItemAssessment>(StringComparer.Ordinal);
        var handlerDiagnostics = new List<JdStageTwoHandlerDiagnostic>();
        foreach (var element in scoresElement.EnumerateArray().Take(MaxTopLevelItems))
        {
            if (!TryReadAssessment(
                    element,
                    expectedById,
                    accepted,
                    out var assessment,
                    out var warning,
                    out var itemHandlerDiagnostics))
            {
                discardedCount++;
                warnings.Add(warning);
                AddHandlerDiagnostics(handlerDiagnostics, itemHandlerDiagnostics);
                continue;
            }

            AddHandlerDiagnostics(handlerDiagnostics, itemHandlerDiagnostics);
            accepted.Add(assessment!.ItemId, assessment);
            foreach (var diagnostic in assessment.DiagnosticCodes)
            {
                warnings.Add(diagnostic);
            }
        }

        var missingIds = expectedById.Keys
            .Where(id => !accepted.ContainsKey(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        if (missingIds.Length > 0)
        {
            warnings.Add("MISSING_REQUIREMENT_SCORES");
        }

        var quality = accepted.Count == 0
            ? JdStageTwoOutputQuality.INVALID
            : missingIds.Length == 0
                ? JdStageTwoOutputQuality.COMPLETE
                : JdStageTwoOutputQuality.PARTIAL;

        return new JdStageTwoValidatedResponse(
            accepted,
            ReadOptionalBoundedString(root, "narrative", null, MaxNarrativeLength, null),
            quality,
            new JdStageTwoOutputCoverage(
                expectedById.Count,
                inputCount,
                accepted.Count,
                discardedCount,
                missingIds,
                wasTruncated || !isCompleteJson),
            warnings.OrderBy(code => code, StringComparer.Ordinal).ToArray())
        {
            HandlerDiagnostics = handlerDiagnostics
        };
    }

    private static bool HasSupportedRoot(
        JsonElement root,
        ISet<string> warnings,
        out JsonElement scores)
    {
        scores = default;
        if (root.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("ROOT_NOT_OBJECT");
            return false;
        }

        var schemaStatus = JdStageTwoJsonPropertyReader.Read(
            root,
            "schemaVersion",
            "schema_version",
            out var schema);
        if (schemaStatus == JdStageTwoPropertyReadStatus.Ambiguous)
        {
            warnings.Add("AMBIGUOUS_SCHEMA_VERSION_PROPERTY");
            return false;
        }

        if (schemaStatus != JdStageTwoPropertyReadStatus.Found ||
            schema.ValueKind != JsonValueKind.String ||
            !string.Equals(schema.GetString(), SchemaVersion, StringComparison.Ordinal))
        {
            warnings.Add("UNSUPPORTED_SCHEMA_VERSION");
            return false;
        }

        var scoresStatus = JdStageTwoJsonPropertyReader.Read(root, "scores", null, out scores);
        if (scoresStatus == JdStageTwoPropertyReadStatus.Ambiguous)
        {
            warnings.Add("AMBIGUOUS_SCORES_PROPERTY");
            return false;
        }

        if (scoresStatus != JdStageTwoPropertyReadStatus.Found || scores.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("SCORES_ARRAY_MISSING_OR_INVALID");
            return false;
        }

        return true;
    }

    private static bool TryReadAssessment(
        JsonElement element,
        IReadOnlyDictionary<string, ProjectedJdRequirementItem> expectedById,
        IReadOnlyDictionary<string, JdStageTwoItemAssessment> accepted,
        out JdStageTwoItemAssessment? assessment,
        out string warning,
        out IReadOnlyList<JdStageTwoHandlerDiagnostic> handlerDiagnostics)
    {
        assessment = null;
        warning = "INVALID_SCORE_ITEM";
        handlerDiagnostics = Array.Empty<JdStageTwoHandlerDiagnostic>();
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryReadRequiredString(
                element,
                "reqId",
                "req_id",
                out var itemId,
                out var itemIdStatus))
        {
            warning = itemIdStatus == JdStageTwoPropertyReadStatus.Ambiguous
                ? "AMBIGUOUS_REQUIREMENT_ID_PROPERTY"
                : "INVALID_SCORE_ITEM";
            return false;
        }

        if (!TryReadHandlerCode(element, out var returnedHandlerCode, out var handlerStatus))
        {
            warning = handlerStatus == JdStageTwoPropertyReadStatus.Ambiguous
                ? "AMBIGUOUS_HANDLER_CODE_PROPERTY"
                : "INVALID_SCORE_ITEM";
            return false;
        }

        if (!expectedById.TryGetValue(itemId, out var expected))
        {
            warning = "UNKNOWN_REQUIREMENT_ID";
            return false;
        }

        if (accepted.ContainsKey(itemId))
        {
            warning = "DUPLICATE_REQUIREMENT_ID";
            return false;
        }

        if (!MatchingHandlerCodeNormalizer.TryNormalize(
                returnedHandlerCode,
                out var resolution,
                out var normalizationDiagnostic))
        {
            warning = MatchingHandlerCodePolicy.IsNonScoringCode(returnedHandlerCode.Trim())
                ? "NON_SCORING_HANDLER_CODE"
                : normalizationDiagnostic;
            handlerDiagnostics =
            [
                new JdStageTwoHandlerDiagnostic(
                    warning,
                    expected.Category,
                    returnedHandlerCode,
                    null)
            ];
            return false;
        }

        var diagnostics = new HashSet<string>(StringComparer.Ordinal);
        var resolvedHandlerDiagnostics = new List<JdStageTwoHandlerDiagnostic>(2);
        if (!string.Equals(resolution.Category, expected.Category, StringComparison.Ordinal))
        {
            resolvedHandlerDiagnostics.Add(new JdStageTwoHandlerDiagnostic(
                "HANDLER_CATEGORY_DIFFERENCE_ACCEPTED",
                expected.Category,
                returnedHandlerCode,
                resolution.HandlerCode));
        }

        if (!string.IsNullOrEmpty(normalizationDiagnostic))
        {
            resolvedHandlerDiagnostics.Add(new JdStageTwoHandlerDiagnostic(
                normalizationDiagnostic,
                expected.Category,
                returnedHandlerCode,
                resolution.HandlerCode));
        }

        var reasoning = ReadOptionalBoundedString(
            element,
            "reasoning",
            null,
            MaxReasoningLength,
            diagnostics,
            "REASONING_MISSING_OR_INVALID");
        var evidence = ReadEvidence(element, diagnostics);
        assessment = new JdStageTwoItemAssessment(
            itemId,
            expected.Category,
            resolution.HandlerCode,
            resolution.Score,
            reasoning,
            evidence,
            diagnostics.OrderBy(code => code, StringComparer.Ordinal).ToArray());
        handlerDiagnostics = resolvedHandlerDiagnostics;
        warning = string.Empty;
        return true;
    }

    private static bool TryReadHandlerCode(
        JsonElement element,
        out string returnedValue,
        out JdStageTwoPropertyReadStatus status)
    {
        returnedValue = string.Empty;
        status = JdStageTwoJsonPropertyReader.Read(
            element,
            "handlerCode",
            "handler_code",
            out var propertyValue);
        if (status != JdStageTwoPropertyReadStatus.Found ||
            propertyValue.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        returnedValue = propertyValue.GetString() ?? string.Empty;
        return returnedValue.Length <= MatchingHandlerCodeNormalizer.MaximumHandlerCodeLength &&
               returnedValue.Trim().Length is > 0 and <= MatchingHandlerCodeNormalizer.MaximumHandlerCodeLength;
    }

    private static void AddHandlerDiagnostics(
        ICollection<JdStageTwoHandlerDiagnostic> target,
        IEnumerable<JdStageTwoHandlerDiagnostic> source)
    {
        foreach (var diagnostic in source)
        {
            if (target.Count >= MaxHandlerDiagnostics)
            {
                return;
            }

            target.Add(diagnostic);
        }
    }

    private static IReadOnlyList<JdMatchingEvidence> ReadEvidence(
        JsonElement element,
        ISet<string> diagnostics)
    {
        var evidenceStatus = JdStageTwoJsonPropertyReader.Read(element, "evidence", null, out var evidence);
        if (evidenceStatus != JdStageTwoPropertyReadStatus.Found ||
            evidence.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add("EVIDENCE_MISSING_OR_INVALID");
            return Array.Empty<JdMatchingEvidence>();
        }

        var accepted = new List<JdMatchingEvidence>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in evidence.EnumerateArray().Take(MaxEvidenceItems))
        {
            if (entry.ValueKind != JsonValueKind.Object ||
                !TryReadRequiredString(
                    entry,
                    "quotation",
                    null,
                    out var quotation,
                    out _,
                    MaxEvidenceFieldLength) ||
                !TryReadRequiredString(
                    entry,
                    "section",
                    null,
                    out var section,
                    out _,
                    MaxEvidenceFieldLength))
            {
                diagnostics.Add("EVIDENCE_MISSING_OR_INVALID");
                continue;
            }

            var key = $"{quotation}\u001f{section}";
            if (seen.Add(key))
            {
                accepted.Add(new JdMatchingEvidence(quotation, section));
            }
        }

        if (accepted.Count == 0)
        {
            diagnostics.Add("EVIDENCE_MISSING_OR_INVALID");
        }

        return accepted;
    }

    private static bool TryReadRequiredString(
        JsonElement element,
        string canonicalProperty,
        string? alternateProperty,
        out string value,
        out JdStageTwoPropertyReadStatus status,
        int maximumLength = MaxNarrativeLength)
    {
        value = string.Empty;
        status = JdStageTwoJsonPropertyReader.Read(
            element,
            canonicalProperty,
            alternateProperty,
            out var propertyValue);
        if (status != JdStageTwoPropertyReadStatus.Found ||
            propertyValue.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = propertyValue.GetString()?.Trim() ?? string.Empty;
        return value.Length is > 0 && value.Length <= maximumLength;
    }

    private static string ReadOptionalBoundedString(
        JsonElement element,
        string canonicalProperty,
        string? alternateProperty,
        int maximumLength,
        ISet<string>? diagnostics,
        string diagnostic = "")
    {
        var status = JdStageTwoJsonPropertyReader.Read(
            element,
            canonicalProperty,
            alternateProperty,
            out var value);
        if (status != JdStageTwoPropertyReadStatus.Found ||
            value.ValueKind != JsonValueKind.String)
        {
            if (diagnostic.Length > 0) diagnostics?.Add(diagnostic);
            return string.Empty;
        }

        var text = value.GetString()?.Trim() ?? string.Empty;
        if (text.Length is > 0 && text.Length <= maximumLength)
        {
            return text;
        }

        if (diagnostic.Length > 0) diagnostics?.Add(diagnostic);
        return string.Empty;
    }

    private static JdStageTwoValidatedResponse Invalid(
        IEnumerable<string> expectedIds,
        bool wasTruncated,
        ISet<string> warnings)
    {
        var missing = expectedIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        return new JdStageTwoValidatedResponse(
            new Dictionary<string, JdStageTwoItemAssessment>(StringComparer.Ordinal),
            string.Empty,
            JdStageTwoOutputQuality.INVALID,
            new JdStageTwoOutputCoverage(missing.Length, 0, 0, 0, missing, wasTruncated),
            warnings.OrderBy(code => code, StringComparer.Ordinal).ToArray());
    }
}
