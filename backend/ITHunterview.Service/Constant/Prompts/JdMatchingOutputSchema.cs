using System;

namespace ITHunterview.Service.Constant.Prompts;

public sealed record JdMatchingPromptNormalization(
    string SemanticContent,
    bool RemovedKnownSchema,
    bool RemovedKnownFormatFooter);

/// <summary>
/// Owns the provider output contract for the one-CV/one-JD matching prompt.
/// The database stores semantic instructions only; this block is appended at
/// runtime and must not be edited through Prompt Management.
/// </summary>
public static class JdMatchingOutputSchema
{
    public const string BeginMarker = "--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---";
    public const string EndMarker = "--- END LOCKED JD MATCHING OUTPUT SCHEMA ---";

    private const string LegacySchemaStartMarker =
        "SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):";
    private const string LegacySchemaEndMarker =
        "HANDLER SCORING RULES (MANDATORY — follow exactly):";
    private const string LegacyFormatFooter =
        "Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.";

    /// <summary>
    /// This JSON example is copied from the reviewed active JD_MATCHING_PROMPT
    /// v2.0. Field names, nesting, enum text, and score representation are
    /// intentionally unchanged.
    /// </summary>
    public const string LockedBlock = """
        --- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---
        This output format is managed by the application. Return exactly one JSON object without Markdown, comments, headings, or surrounding text.

        SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):
        {
          "scores": [
            {
              "reqId": "string (giữ nguyên reqId từ input)",
              "handlerCode": "string (Mã code, vd: H_TECH_03...)",
              "handlerScore": 0.0 | 0.3 | 0.5 | 0.7 | 1.0,
              "reasoning": "string (Ngắn gọn tối đa 15 từ)",
              "confidence": "high" | "medium" | "low",
              "flag": "CRITICAL_GAP" | null
            }
          ],
          "criticalGaps": [
            {
              "requirement": "string",
              "gapDescription": "string",
              "severity": "high" | "medium",
              "suggestion": "string"
            }
          ],
          "penalties": [
            {
              "code": "PNL_TC1_01",
              "triggered": true/false,
              "evidence": "string"
            }
          ],
          "narrative": "string (Tóm tắt tổng quan mức độ phù hợp CV-JD, khoảng 3-4 câu)",
          "improvements": [
            {
              "priority": "high" | "medium" | "low",
              "category": "tech_skill" | "experience" | "education" | "soft_skill",
              "issue": "string",
              "action": "string",
              "example": { "before": "string", "after": "string" }
            }
          ]
        }

        Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.
        --- END LOCKED JD MATCHING OUTPUT SCHEMA ---
        """;

    public static string Compose(string managedContent)
    {
        var normalized = NormalizeManagedContent(managedContent);
        return $"{normalized.SemanticContent.Trim()}\n\n{LockedBlock.Trim()}";
    }

    public static JdMatchingPromptNormalization NormalizeManagedContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("JD matching prompt content is required.", nameof(content));
        }

        var normalized = content.Trim();
        var removedKnownSchema = false;
        var removedKnownFooter = false;

        var lockedStart = normalized.IndexOf(BeginMarker, StringComparison.Ordinal);
        var lockedEnd = normalized.IndexOf(EndMarker, StringComparison.Ordinal);
        if (lockedStart >= 0 || lockedEnd >= 0)
        {
            if (!HasSingleOrderedPair(normalized, BeginMarker, EndMarker, lockedStart, lockedEnd))
            {
                throw new ArgumentException("MATCHING_PROMPT_SCHEMA_MUTATION");
            }

            var lockedBlock = normalized[lockedStart..(lockedEnd + EndMarker.Length)].Trim();
            if (!SameBlock(lockedBlock, LockedBlock))
            {
                throw new ArgumentException("MATCHING_PROMPT_SCHEMA_MUTATION");
            }

            normalized = JoinSections(
                normalized[..lockedStart],
                normalized[(lockedEnd + EndMarker.Length)..]);
            removedKnownSchema = true;
        }

        var legacyStart = normalized.IndexOf(LegacySchemaStartMarker, StringComparison.Ordinal);
        var legacyEnd = normalized.IndexOf(LegacySchemaEndMarker, StringComparison.Ordinal);
        if (legacyStart >= 0)
        {
            if (!HasSingleOrderedPair(normalized, LegacySchemaStartMarker, LegacySchemaEndMarker, legacyStart, legacyEnd))
            {
                throw new ArgumentException("MATCHING_PROMPT_SCHEMA_MUTATION");
            }

            var legacySchemaBlock = normalized[legacyStart..legacyEnd].Trim();
            if (!SameBlock(legacySchemaBlock, KnownLegacySchemaBlock))
            {
                throw new ArgumentException("MATCHING_PROMPT_SCHEMA_MUTATION");
            }

            normalized = JoinSections(
                normalized[..legacyStart],
                normalized[legacyEnd..]);
            removedKnownSchema = true;
        }

        var footerIndex = normalized.IndexOf(LegacyFormatFooter, StringComparison.Ordinal);
        if (footerIndex >= 0)
        {
            if (normalized.IndexOf(LegacyFormatFooter, footerIndex + LegacyFormatFooter.Length, StringComparison.Ordinal) >= 0)
            {
                throw new ArgumentException("MATCHING_PROMPT_SCHEMA_MUTATION");
            }

            normalized = JoinSections(
                normalized[..footerIndex],
                normalized[(footerIndex + LegacyFormatFooter.Length)..]);
            removedKnownFooter = true;
        }

        if (ContainsUnmarkedSchemaSignature(normalized))
        {
            throw new ArgumentException("MATCHING_PROMPT_SCHEMA_MUTATION");
        }

        return new JdMatchingPromptNormalization(normalized, removedKnownSchema, removedKnownFooter);
    }

    private static string KnownLegacySchemaBlock => ExtractKnownLegacySchemaBlock();

    private static string ExtractKnownLegacySchemaBlock()
    {
        var start = LockedBlock.IndexOf(LegacySchemaStartMarker, StringComparison.Ordinal);
        var end = LockedBlock.IndexOf(LegacyFormatFooter, start, StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("The locked matching schema is incomplete.");
        }

        return LockedBlock[start..end].Trim();
    }

    private static bool SameBlock(string left, string right) =>
        string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), StringComparison.Ordinal);

    private static string NormalizeForComparison(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private static bool ContainsUnmarkedSchemaSignature(string value) =>
        value.Contains("\"scores\"", StringComparison.Ordinal) &&
        value.Contains("\"reqId\"", StringComparison.Ordinal) &&
        value.Contains("\"handlerScore\"", StringComparison.Ordinal);

    private static bool HasSingleOrderedPair(
        string content,
        string startMarker,
        string endMarker,
        int startIndex,
        int endIndex) =>
        startIndex >= 0 &&
        endIndex > startIndex &&
        content.IndexOf(startMarker, startIndex + startMarker.Length, StringComparison.Ordinal) < 0 &&
        content.IndexOf(endMarker, endIndex + endMarker.Length, StringComparison.Ordinal) < 0;

    private static string JoinSections(string before, string after)
    {
        var normalizedBefore = before.TrimEnd();
        var normalizedAfter = after.TrimStart();

        if (normalizedBefore.Length == 0)
        {
            return normalizedAfter;
        }

        if (normalizedAfter.Length == 0)
        {
            return normalizedBefore;
        }

        return $"{normalizedBefore}\n\n{normalizedAfter}";
    }
}
