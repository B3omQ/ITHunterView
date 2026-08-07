import { describe, expect, it } from 'vitest'
import {
  isJdAnalysisPromptKey,
  sanitizeJdAnalysisContentForEditing,
} from './jd-analysis-prompt-policy'

describe('JD analysis prompt editing policy', () => {
  it('recognizes only the managed JD analysis prompt keys', () => {
    expect(isJdAnalysisPromptKey('JD_ANALYSIS_V2_SYSTEM')).toBe(true)
    expect(isJdAnalysisPromptKey('JD_ANALYSIS_V2_USER')).toBe(true)
    expect(isJdAnalysisPromptKey('JD_MATCHING_PROMPT')).toBe(false)
  })

  it('removes a copied legacy output contract but preserves semantic instructions', () => {
    const content = `Semantic extraction instructions.\n\nOUTPUT CONTRACT\n{ "schema_version": "jd-analysis/v3" }\n\nEVIDENCE AND SOURCE RULES\nKeep evidence grounded.`

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe(
      'Semantic extraction instructions.\n\nEVIDENCE AND SOURCE RULES\nKeep evidence grounded.',
    )
  })

  it('does not remove semantic prose that merely mentions a requirement group', () => {
    const content = 'Explain how requirement_groups should reflect the JD.'

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe(content)
  })
})
