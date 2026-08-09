using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

public sealed class JdMatchReportReader : IJdMatchReportReader
{
    private const int MaximumGroups = 100;
    private const int MaximumItemsPerGroup = 200;
    private const int MaximumWarnings = 100;
    private const int MaximumTextLength = 4_000;

    public MatchReportDto Read(string? matchDetails, decimal? persistedScore, string? matchType)
    {
        if (string.IsNullOrWhiteSpace(matchDetails))
        {
            return LegacySummary(persistedScore, DetectMethod(default, matchType, hasDocument: false));
        }

        try
        {
            using var document = JsonDocument.Parse(matchDetails, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
                MaxDepth = 64
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return LegacySummary(persistedScore, DetectMethod(default, matchType, hasDocument: false));
            }

            var root = document.RootElement;
            var contract = ReadString(root, "contract");
            if (string.Equals(contract, JdFitResultContract.RawTextFallback, StringComparison.Ordinal))
            {
                return ReadRawTextFallback(root, persistedScore, contract);
            }

            if (string.Equals(contract, JdFitResultContract.Current, StringComparison.Ordinal) ||
                string.Equals(contract, JdFitResultContract.Version4, StringComparison.Ordinal) ||
                (TryGet(root, "jdFit", out var jdFitCandidate) && jdFitCandidate.ValueKind == JsonValueKind.Object))
            {
                return ReadStructured(root, persistedScore, contract);
            }

            return ReadLegacy(root, persistedScore, matchType);
        }
        catch (JsonException)
        {
            return LegacySummary(persistedScore, DetectMethod(default, matchType, hasDocument: false));
        }
    }

    private static MatchReportDto ReadStructured(JsonElement root, decimal? persistedScore, string? contract)
    {
        if (!TryGet(root, "jdFit", out var jdFit) || jdFit.ValueKind != JsonValueKind.Object)
        {
            return LegacySummary(persistedScore, MatchMethodCodes.OneToOneAi);
        }

        var isV4 = string.Equals(contract, JdFitResultContract.Version4, StringComparison.Ordinal);
        var score = ReadDecimal(jdFit, "scorePercent") ?? ReadDecimal(jdFit, "score") ?? persistedScore ?? 0m;
        var report = new MatchReportDto
        {
            ReportKind = MatchReportKinds.Structured,
            SchemaVersion = contract,
            MatchMethod = MatchMethodCodes.OneToOneAi,
            ScorePercent = ClampPercent(score),
            ResultCode = ReadString(jdFit, "resultCode"),
            ResultLabel = ReadString(jdFit, "resultLabel") ?? ReadString(jdFit, "result"),
            Narrative = ReadString(jdFit, "narrative") ?? string.Empty
        };

        if (TryGet(jdFit, "requirementGroups", out var groups) && groups.ValueKind == JsonValueKind.Array)
        {
            foreach (var group in groups.EnumerateArray().Take(MaximumGroups))
            {
                if (group.ValueKind != JsonValueKind.Object) continue;
                report.RequirementGroups.Add(ReadGroup(group, isV4));
            }
        }
        else if (TryGet(jdFit, "requirementScores", out var scores) && scores.ValueKind == JsonValueKind.Array)
        {
            foreach (var scoreItem in scores.EnumerateArray().Take(MaximumGroups))
            {
                if (scoreItem.ValueKind != JsonValueKind.Object) continue;
                var item = ReadItem(scoreItem, isV4: false);
                report.RequirementGroups.Add(new MatchRequirementGroupReportDto
                {
                    GroupId = ReadString(scoreItem, "groupId"),
                    Importance = ReadString(scoreItem, "importance"),
                    GroupScore = item.Score,
                    IsCriticalGap = IsCritical(scoreItem),
                    Items = new List<MatchRequirementItemReportDto> { item }
                });
            }
        }

        ReadCriticalGaps(jdFit, report);
        ReadWarnings(jdFit, report);
        return report;
    }

    private static MatchReportDto ReadRawTextFallback(JsonElement root, decimal? persistedScore, string contract)
    {
        var report = ReadStructured(root, persistedScore, contract);
        report.ReportKind = MatchReportKinds.RawTextFallback;
        report.MatchMethod = MatchMethodCodes.RawTextAi;
        report.RequirementGroups.Clear();
        report.CriticalGaps.Clear();
        return report;
    }

    private static MatchReportDto ReadLegacy(JsonElement root, decimal? persistedScore, string? matchType)
    {
        var method = DetectMethod(root, matchType, hasDocument: true);
        var finalScore = ReadDecimal(root, "FinalScore") ?? persistedScore ?? 0m;
        var scorePercent = method is MatchMethodCodes.Hardcode or MatchMethodCodes.Vector
            ? finalScore * 100m
            : finalScore;
        var report = LegacySummary(scorePercent, method);
        report.Narrative = method switch
        {
            MatchMethodCodes.Hardcode => "Keyword-based matching result.",
            MatchMethodCodes.Vector => "Vector similarity matching result.",
            _ => string.Empty
        };
        return report;
    }

    private static MatchReportDto LegacySummary(decimal? persistedScore, string method) => new()
    {
        ReportKind = MatchReportKinds.LegacySummary,
        MatchMethod = method,
        ScorePercent = ClampPercent(persistedScore ?? 0m)
    };

    private static MatchRequirementGroupReportDto ReadGroup(JsonElement group, bool isV4)
    {
        var result = new MatchRequirementGroupReportDto
        {
            GroupId = ReadString(group, "groupId"),
            SourceRequirementId = isV4 ? ReadString(group, "sourceRequirementId") : null,
            Intent = isV4 ? ReadString(group, "intent") : null,
            Operator = ReadString(group, "operator"),
            MinSatisfied = ReadInt(group, "minSatisfied"),
            Importance = ReadString(group, "importance"),
            SourceSection = isV4 ? ReadString(group, "sourceSection") : null,
            RequirementVerbatim = isV4 ? ReadString(group, "requirementVerbatim") : null,
            GroupScore = ClampUnit(ReadDecimal(group, "groupScore") ?? ReadDecimal(group, "handlerScore") ?? 0m),
            IsCriticalGap = ReadBool(group, "isCriticalGap") || IsCritical(group),
            SourceOrder = isV4 ? ReadInt(group, "sourceOrder") : null
        };

        if (TryGet(group, "selectedItemIds", out var selected) && selected.ValueKind == JsonValueKind.Array)
        {
            result.SelectedItemIds = selected.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => Bound(item.GetString()))
                .Where(item => item is not null)
                .Cast<string>()
                .Take(MaximumItemsPerGroup)
                .ToList();
        }

        if (TryGet(group, "items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray().Take(MaximumItemsPerGroup))
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    result.Items.Add(ReadItem(item, isV4));
                }
            }
        }
        return result;
    }

    private static MatchRequirementItemReportDto ReadItem(JsonElement item, bool isV4)
    {
        var result = new MatchRequirementItemReportDto
        {
            ItemId = ReadString(item, "itemId") ?? ReadString(item, "reqId"),
            NormalizedText = ReadString(item, "normalizedText"),
            DetailVerbatim = ReadString(item, "detailVerbatim"),
            RawMention = isV4 ? ReadString(item, "rawMention") : null,
            Category = ReadString(item, "category"),
            Score = ClampUnit(ReadDecimal(item, "score") ?? ReadDecimal(item, "handlerScore") ?? 0m),
            HandlerCode = ReadString(item, "handlerCode"),
            Reasoning = ReadString(item, "reasoning") ?? string.Empty,
            IsCriticalGap = ReadBool(item, "isCriticalGap") || IsCritical(item),
            SourceOrder = isV4 ? ReadInt(item, "sourceOrder") : null
        };

        ReadEvidence(item, result.Evidence);
        return result;
    }

    private static void ReadCriticalGaps(JsonElement jdFit, MatchReportDto report)
    {
        if (!TryGet(jdFit, "criticalGaps", out var gaps) || gaps.ValueKind != JsonValueKind.Array) return;
        foreach (var gap in gaps.EnumerateArray().Take(MaximumWarnings))
        {
            if (gap.ValueKind != JsonValueKind.Object) continue;
            var result = new MatchCriticalGapReportDto
            {
                Code = ReadString(gap, "code") ?? ReadString(gap, "flag") ?? "CRITICAL_GAP",
                Scope = ReadString(gap, "scope"),
                GroupId = ReadString(gap, "groupId"),
                ItemId = ReadString(gap, "itemId"),
                Requirement = ReadString(gap, "requirement") ?? string.Empty,
                Reasoning = ReadString(gap, "reasoning") ?? ReadString(gap, "gapDescription") ?? string.Empty
            };
            ReadEvidence(gap, result.Evidence);
            report.CriticalGaps.Add(result);
        }
    }

    private static void ReadEvidence(JsonElement owner, List<MatchEvidenceReportDto> target)
    {
        if (!TryGet(owner, "evidence", out var evidence) || evidence.ValueKind != JsonValueKind.Array) return;

        foreach (var entry in evidence.EnumerateArray().Take(50))
        {
            if (entry.ValueKind == JsonValueKind.String)
            {
                var quotation = Bound(entry.GetString());
                if (quotation is not null)
                {
                    target.Add(new MatchEvidenceReportDto { Quotation = quotation });
                }
            }
            else if (entry.ValueKind == JsonValueKind.Object)
            {
                var quotation = ReadString(entry, "quotation");
                if (!string.IsNullOrWhiteSpace(quotation))
                {
                    target.Add(new MatchEvidenceReportDto
                    {
                        Quotation = quotation,
                        Section = ReadString(entry, "section")
                    });
                }
            }
        }
    }

    private static void ReadWarnings(JsonElement jdFit, MatchReportDto report)
    {
        if (!TryGet(jdFit, "warningFlags", out var warnings) || warnings.ValueKind != JsonValueKind.Array) return;
        report.WarningFlags = warnings.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => Bound(item.GetString()))
            .Where(item => item is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Take(MaximumWarnings)
            .ToList();
    }

    private static string DetectMethod(JsonElement root, string? matchType, bool hasDocument)
    {
        if (hasDocument)
        {
            var method = ReadString(root, "Method");
            if (string.Equals(method, "Hardcode", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "HardcodeV3", StringComparison.OrdinalIgnoreCase))
            {
                return MatchMethodCodes.Hardcode;
            }
            if (TryGet(root, "FinalScore", out _) &&
                (TryGet(root, "TitleScore", out _) || TryGet(root, "SkillsScore", out _)))
            {
                return MatchMethodCodes.Vector;
            }
        }
        if (string.Equals(matchType, "Hardcode", StringComparison.OrdinalIgnoreCase))
        {
            return MatchMethodCodes.Hardcode;
        }
        return MatchMethodCodes.LegacyUnknown;
    }

    private static bool IsCritical(JsonElement element) =>
        string.Equals(ReadString(element, "flag"), "CRITICAL_GAP", StringComparison.OrdinalIgnoreCase);

    private static bool TryGet(JsonElement element, string property, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(property, out value)) return true;
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, property, StringComparison.OrdinalIgnoreCase))
                {
                    value = candidate.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    private static string? ReadString(JsonElement element, string property) =>
        TryGet(element, property, out var value) && value.ValueKind == JsonValueKind.String
            ? Bound(value.GetString())
            : null;

    private static decimal? ReadDecimal(JsonElement element, string property) =>
        TryGet(element, property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number
            : null;

    private static int? ReadInt(JsonElement element, string property) =>
        TryGet(element, property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : null;

    private static bool ReadBool(JsonElement element, string property) =>
        TryGet(element, property, out var value) && value.ValueKind == JsonValueKind.True;

    private static string? Bound(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        return normalized.Length <= MaximumTextLength ? normalized : normalized[..MaximumTextLength];
    }

    private static decimal ClampUnit(decimal value) => Math.Clamp(value, 0m, 1m);
    private static decimal ClampPercent(decimal value) => Math.Clamp(value, 0m, 100m);
}
