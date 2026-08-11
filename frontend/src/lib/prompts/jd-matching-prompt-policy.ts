const JD_MATCHING_PROMPT_KEY = 'JD_MATCHING_PROMPT'

const LOCKED_SCHEMA_BEGIN = '--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---'
const LOCKED_SCHEMA_END = '--- END LOCKED JD MATCHING OUTPUT SCHEMA ---'
const LEGACY_SCHEMA_BEGIN = 'SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):'
const LEGACY_SCHEMA_END = 'HANDLER SCORING RULES (MANDATORY — follow exactly):'
const LEGACY_FORMAT_FOOTER = 'Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.'

const NEW_LOCKED_BLOCK = `--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---
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
--- END LOCKED JD MATCHING OUTPUT SCHEMA ---`

const LEGACY_V2_SCHEMA_BLOCK = `SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):
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
}`

const LEGACY_V2_LOCKED_BLOCK = `${LOCKED_SCHEMA_BEGIN}
This output format is managed by the application. Return exactly one JSON object without Markdown, comments, headings, or surrounding text.

${LEGACY_V2_SCHEMA_BLOCK}

${LEGACY_FORMAT_FOOTER}
${LOCKED_SCHEMA_END}`

export function isJdMatchingPromptKey(promptKey?: string): boolean {
  return promptKey === JD_MATCHING_PROMPT_KEY
}

export function normalizePromptModelConfigForSubmission(
  promptKey: string | undefined,
  modelConfig: string | undefined,
): string | undefined {
  return isJdMatchingPromptKey(promptKey) ? undefined : modelConfig
}

/**
 * Removes only exact reviewed application-managed blocks. Unknown or mutated
 * blocks stay visible so backend validation can reject them without the editor
 * silently deleting administrator content.
 */
export function sanitizeJdMatchingContentForEditing(content: string): string {
  const canonical = normalizeLineEndings(content)
  const locked = removeExactDelimitedBlock(
    canonical,
    LOCKED_SCHEMA_BEGIN,
    LOCKED_SCHEMA_END,
    [NEW_LOCKED_BLOCK, LEGACY_V2_LOCKED_BLOCK],
    false,
  )
  if (locked !== canonical) {
    return removeLegacyFooter(locked)
  }

  const legacy = removeExactDelimitedBlock(
    canonical,
    LEGACY_SCHEMA_BEGIN,
    LEGACY_SCHEMA_END,
    [LEGACY_V2_SCHEMA_BLOCK],
    true,
  )
  return legacy === canonical ? canonical : removeLegacyFooter(legacy)
}

function removeExactDelimitedBlock(
  content: string,
  startMarker: string,
  endMarker: string,
  reviewedBlocks: readonly string[],
  keepEndMarker: boolean,
): string {
  const startIndex = content.indexOf(startMarker)
  if (startIndex < 0 || content.indexOf(startMarker, startIndex + startMarker.length) >= 0) {
    return content
  }

  const endIndex = content.indexOf(endMarker, startIndex + startMarker.length)
  if (endIndex < 0 || content.indexOf(endMarker, endIndex + endMarker.length) >= 0) {
    return content
  }

  const blockEnd = keepEndMarker ? endIndex : endIndex + endMarker.length
  const block = content.slice(startIndex, blockEnd).trim()
  if (!reviewedBlocks.some(reviewed => block === reviewed.trim())) {
    return content
  }

  return joinSections(content.slice(0, startIndex), content.slice(blockEnd))
}

function removeLegacyFooter(content: string): string {
  const first = content.indexOf(LEGACY_FORMAT_FOOTER)
  if (first < 0 || content.indexOf(LEGACY_FORMAT_FOOTER, first + LEGACY_FORMAT_FOOTER.length) >= 0) {
    return content
  }

  return joinSections(
    content.slice(0, first),
    content.slice(first + LEGACY_FORMAT_FOOTER.length),
  )
}

function joinSections(before: string, after: string): string {
  return [before.trimEnd(), after.trimStart()].filter(Boolean).join('\n\n')
}

function normalizeLineEndings(value: string): string {
  return value.replace(/\r\n?/g, '\n')
}
