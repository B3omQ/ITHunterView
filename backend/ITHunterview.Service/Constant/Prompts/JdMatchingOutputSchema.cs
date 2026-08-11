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

    private static readonly string LegacyV2LockedBlock = NormalizeLineEndings("""
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
        """);

    private const string LegacyV2SchemaBlock = """
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
        """;

    /// <summary>
    /// Application-owned Stage 2 provider contract. Prompt Management stores
    /// semantic instructions only; this LF-normalized block is appended once.
    /// </summary>
    public static readonly string LockedBlock = NormalizeLineEndings("""
        --- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---
        This output format is managed by the application. Return exactly one JSON object without Markdown, comments, headings, or surrounding text.

        {
          "schemaVersion": "jd-stage2/v2",
          "scores": [
            {
              "reqId": "exact input item ID",
              "handlerCode": "approved code for the input category",
              "reasoning": "detailed user-safe explanation",
              "evidence": [
                {
                  "quotation": "bounded CV quotation",
                  "section": "bounded CV section identifier"
                }
              ]
            }
          ],
          "narrative": "overall summary"
        }

        Only schemaVersion, scores, reqId, and handlerCode are required for scoring. Optional reasoning, evidence, and narrative must be preserved when available but must not change the selected score.
        --- END LOCKED JD MATCHING OUTPUT SCHEMA ---
        """);

    public static string Compose(string managedContent)
    {
        var normalized = NormalizeManagedContent(managedContent);
        return $"{NormalizeLineEndings(normalized.SemanticContent).Trim()}\n\n{LockedBlock.Trim()}";
    }

    public static JdMatchingPromptNormalization NormalizeManagedContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("JD matching prompt content is required.", nameof(content));
        }

        var normalized = NormalizeLineEndings(content).Trim();
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
            if (!SameBlock(lockedBlock, LockedBlock) &&
                !SameBlock(lockedBlock, LegacyV2LockedBlock))
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
            if (!SameBlock(legacySchemaBlock, LegacyV2SchemaBlock))
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

    private static bool SameBlock(string left, string right) =>
        string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), StringComparison.Ordinal);

    private static string NormalizeForComparison(string value) =>
        NormalizeLineEndings(value).Trim();

    private static bool ContainsUnmarkedSchemaSignature(string value) =>
        value.Contains("\"scores\"", StringComparison.Ordinal) &&
        value.Contains("\"reqId\"", StringComparison.Ordinal) &&
        (value.Contains("\"handlerScore\"", StringComparison.Ordinal) ||
         (value.Contains("\"schemaVersion\"", StringComparison.Ordinal) &&
          value.Contains("\"jd-stage2/v2\"", StringComparison.Ordinal) &&
          value.Contains("\"handlerCode\"", StringComparison.Ordinal)));

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

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
