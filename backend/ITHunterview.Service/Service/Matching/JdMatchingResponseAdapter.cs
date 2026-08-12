using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed class JdMatchingResponseAdapter
{
    public JdStageTwoValidatedResponse Adapt(
        JsonDocument response,
        JdRequirementProjection projection,
        bool isCompleteJson = true,
        bool wasTruncated = false,
        IReadOnlyList<string>? recoveryWarnings = null) =>
        JdMatchingResponseValidator.Validate(
            response,
            projection,
            isCompleteJson,
            wasTruncated,
            recoveryWarnings);

    public JdStageTwoValidatedResponse MergeMissingOnly(
        JdStageTwoValidatedResponse first,
        JdStageTwoValidatedResponse second,
        IReadOnlySet<string> allExpectedIds,
        IReadOnlySet<string> requestedOnSecondAttempt)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(allExpectedIds);
        ArgumentNullException.ThrowIfNull(requestedOnSecondAttempt);

        var merged = new Dictionary<string, JdStageTwoItemAssessment>(
            first.ItemAssessments,
            StringComparer.Ordinal);
        foreach (var assessment in second.ItemAssessments)
        {
            if (allExpectedIds.Contains(assessment.Key) &&
                requestedOnSecondAttempt.Contains(assessment.Key))
            {
                merged.TryAdd(assessment.Key, assessment.Value);
            }
        }

        var missingIds = allExpectedIds
            .Where(id => !merged.ContainsKey(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var quality = merged.Count == 0
            ? JdStageTwoOutputQuality.INVALID
            : missingIds.Length == 0 && merged.Count == allExpectedIds.Count
                ? JdStageTwoOutputQuality.COMPLETE
                : JdStageTwoOutputQuality.PARTIAL;

        var warnings = first.WarningCodes
            .Concat(second.WarningCodes)
            .Where(code => missingIds.Length > 0 ||
                           !string.Equals(code, "MISSING_REQUIREMENT_SCORES", StringComparison.Ordinal))
            .Append("MERGED_PARTIAL_ATTEMPTS")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        var handlerDiagnostics = first.HandlerDiagnostics
            .Concat(second.HandlerDiagnostics)
            .Distinct()
            .Take(20)
            .ToArray();

        return new JdStageTwoValidatedResponse(
            merged,
            string.IsNullOrWhiteSpace(second.Narrative) ? first.Narrative : second.Narrative,
            quality,
            new JdStageTwoOutputCoverage(
                allExpectedIds.Count,
                first.Coverage.InputCount + second.Coverage.InputCount,
                merged.Count,
                first.Coverage.DiscardedCount + second.Coverage.DiscardedCount,
                missingIds,
                first.Coverage.WasTruncated || second.Coverage.WasTruncated),
            warnings)
        {
            HandlerDiagnostics = handlerDiagnostics
        };
    }
}
