import { describe, expect, it } from 'vitest'
import {
  isJdMatchingPromptKey,
  sanitizeJdMatchingContentForEditing,
} from './jd-matching-prompt-policy'

const lockedBlock = `--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---
{ "scores": [{ "reqId": "x", "handlerScore": 1 }], "criticalGaps": [], "penalties": [], "narrative": "", "improvements": [] }
--- END LOCKED JD MATCHING OUTPUT SCHEMA ---`

const legacyBlock = `SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):
{ "scores": [{ "reqId": "x", "handlerScore": 1 }], "criticalGaps": [], "penalties": [], "narrative": "", "improvements": [] }
Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.
HANDLER SCORING RULES (MANDATORY — follow exactly):`

describe('JD matching prompt editing policy', () => {
  it('recognizes only the matching prompt key', () => {
    expect(isJdMatchingPromptKey('JD_MATCHING_PROMPT')).toBe(true)
    expect(isJdMatchingPromptKey('JD_ANALYSIS_V2_SYSTEM')).toBe(false)
    expect(isJdMatchingPromptKey('CV_ANALYSIS_SYSTEM')).toBe(false)
  })

  it('removes a recognizable locked block and preserves semantic rules', () => {
    const content = `Semantic matching rules.\n\n${lockedBlock}\n\nKeep handler caps.`

    expect(sanitizeJdMatchingContentForEditing(content)).toBe(
      'Semantic matching rules.\n\nKeep handler caps.',
    )
  })

  it('removes the reviewed historical block while retaining the handler heading', () => {
    const content = `Semantic matching rules.\n\n${legacyBlock}\n\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]`

    expect(sanitizeJdMatchingContentForEditing(content)).toBe(
      'Semantic matching rules.\n\nHANDLER SCORING RULES (MANDATORY — follow exactly):\n\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]',
    )
  })

  it('leaves an unknown or mutated schema block untouched for backend rejection', () => {
    const mutated = lockedBlock.replace('"criticalGaps"', '"criticalGapsMutated"')

    expect(sanitizeJdMatchingContentForEditing(`Rules.\n${mutated}`)).toBe(`Rules.\n${mutated}`)
  })

  it('does not remove semantic prose that mentions scores', () => {
    const content = 'Explain how scores are grounded in evidence.\n[CV_TEXT]\n[PARSED_JD_REQUIREMENTS]'

    expect(sanitizeJdMatchingContentForEditing(content)).toBe(content)
  })
})
