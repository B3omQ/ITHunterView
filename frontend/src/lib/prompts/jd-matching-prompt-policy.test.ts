import { describe, expect, it } from 'vitest'
import {
  isJdMatchingPromptKey,
  normalizePromptModelConfigForSubmission,
  sanitizeJdMatchingContentForEditing,
} from './jd-matching-prompt-policy'

const newLockedBlock = `--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---
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

const legacySchema = `SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):
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

const legacyBlock = `${legacySchema}
HANDLER SCORING RULES (MANDATORY — follow exactly):`

describe('JD matching prompt editing policy', () => {
  it('recognizes only the matching prompt key', () => {
    expect(isJdMatchingPromptKey('JD_MATCHING_PROMPT')).toBe(true)
    expect(isJdMatchingPromptKey('JD_ANALYSIS_V2_SYSTEM')).toBe(false)
    expect(isJdMatchingPromptKey('CV_ANALYSIS_SYSTEM')).toBe(false)
  })

  it('removes ModelConfig only for the matching prompt', () => {
    expect(normalizePromptModelConfigForSubmission('JD_MATCHING_PROMPT', '{"temperature":0.2}'))
      .toBeUndefined()
    expect(normalizePromptModelConfigForSubmission('JD_ANALYSIS_V2_USER', '{"contract":"v6"}'))
      .toBe('{"contract":"v6"}')
  })

  it('removes the exact new locked block and preserves semantic rules', () => {
    const content = `Semantic matching rules.\n\n${newLockedBlock}\n\nKeep handler rules.`

    expect(sanitizeJdMatchingContentForEditing(content)).toBe(
      'Semantic matching rules.\n\nKeep handler rules.',
    )
  })

  it('removes the reviewed historical block while retaining the handler heading', () => {
    const content = `Semantic matching rules.\n\n${legacyBlock}\n\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]\nChỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.`

    expect(sanitizeJdMatchingContentForEditing(content)).toBe(
      'Semantic matching rules.\n\nHANDLER SCORING RULES (MANDATORY — follow exactly):\n\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]',
    )
  })

  it('normalizes Windows line endings for the exact reviewed block', () => {
    const content = `Rules.\n${newLockedBlock}`.replace(/\n/g, '\r\n')

    expect(sanitizeJdMatchingContentForEditing(content)).toBe('Rules.')
  })

  it('leaves a mutated new schema block untouched for backend rejection', () => {
    const mutated = newLockedBlock.replace('detailed user-safe explanation', 'short explanation')

    expect(sanitizeJdMatchingContentForEditing(`Rules.\n${mutated}`)).toBe(`Rules.\n${mutated}`)
  })

  it('leaves a mutated historical schema block untouched for backend rejection', () => {
    const mutated = legacyBlock.replace('"criticalGaps"', '"criticalGapsMutated"')

    expect(sanitizeJdMatchingContentForEditing(`Rules.\n${mutated}`)).toBe(`Rules.\n${mutated}`)
  })

  it('does not remove semantic prose that mentions scores', () => {
    const content = 'Explain how scores are grounded in evidence.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]'

    expect(sanitizeJdMatchingContentForEditing(content)).toBe(content)
  })
})
