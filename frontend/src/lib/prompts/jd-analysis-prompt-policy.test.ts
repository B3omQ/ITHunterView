import { describe, expect, it } from 'vitest'
import {
  isJdAnalysisPromptKey,
  sanitizeJdAnalysisContentForEditing,
} from './jd-analysis-prompt-policy'

const lockedV5Block = `--- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---
This output format is managed by the application. It overrides any conflicting output-format instruction above.
Return exactly one JSON object without Markdown, comments, headings, or surrounding text.

{
  "schema_version": "jd-analysis/v5",
  "matching_metrics": {
    "job_titles_normalized": [],
    "total_years_exp": 0,
    "domains": [],
    "requirement_groups": [
      {
        "source_requirement_id": "req-001",
        "intent": "qualification",
        "operator": "all_of",
        "importance": "must_have",
        "source_section": "requirements",
        "requirement_verbatim": "exact complete source clause supporting this group",
        "items": [
          {
            "category": "tech_skill",
            "skill_name": "normalized requirement name",
            "raw_mention": "exact phrase from requirement_verbatim"
          }
        ]
      }
    ]
  }
}

Fixed shape rules:
- schema_version is exactly "jd-analysis/v5".
- matching_metrics contains exactly job_titles_normalized, total_years_exp, domains, and requirement_groups.
- source_requirement_id uses req-NNN in physical source-clause order; groups from the same clause reuse it.
- intent is qualification or experience_duration.
- operator is all_of, one_of, or at_least_n.
- importance is must_have or nice_to_have.
- source_section is title, description, or requirements.
- requirement_verbatim is required and non-empty for every group.
- every group contains at least one item.
- category is tech_skill, experience, domain_knowledge, language, education, or soft_skill.
- skill_name and raw_mention are non-empty strings.
- min_years and max_years are optional non-negative integers; omit unsupported values instead of returning null.
- min_satisfied appears only for at_least_n and is an integer from 1 through the item count.
- output at most 50 requirement groups and at most 100 total group items.
- do not output detail_verbatim, evidence, evidences, confidence, group_id, item_id, requirements_list, skills_normalized, or seniority_fit.
--- END LOCKED JD ANALYSIS OUTPUT SCHEMA ---`

const legacyV4Schema = `{
  "schema_version": "jd-analysis/v4",
  "matching_metrics": {
    "job_titles_normalized": [],
    "total_years_exp": 0,
    "domains": [],
    "requirement_groups": [
      {
        "operator": "all_of",
        "importance": "must_have",
        "source_section": "requirements",
        "requirement_verbatim": "exact complete source clause supporting this group",
        "items": [
          {
            "category": "tech_skill",
            "skill_name": "normalized lowercase requirement name",
            "raw_mention": "exact phrase from requirement_verbatim"
          }
        ]
      }
    ]
  }
}`

describe('JD analysis prompt editing policy', () => {
  it('recognizes only the managed JD analysis prompt keys', () => {
    expect(isJdAnalysisPromptKey('JD_ANALYSIS_V2_SYSTEM')).toBe(true)
    expect(isJdAnalysisPromptKey('JD_ANALYSIS_V2_USER')).toBe(true)
    expect(isJdAnalysisPromptKey('JD_MATCHING_PROMPT')).toBe(false)
  })

  it('removes the exact application-managed v5 block from an editable copy', () => {
    const content = `Semantic extraction instructions.\n\n${lockedV5Block}`

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe('Semantic extraction instructions.')
  })

  it('removes the known active v5.2 legacy v4 contract but preserves surrounding instructions', () => {
    const content = `Semantic extraction instructions.\n\nOUTPUT CONTRACT\n${legacyV4Schema}\n\nREQUIRED STRUCTURE\n- Return exactly one JSON object.\n- Keep every requirement group grounded.\n\nEVIDENCE AND SOURCE RULES\nKeep evidence grounded.`

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe(
      'Semantic extraction instructions.\n\nEVIDENCE AND SOURCE RULES\nKeep evidence grounded.',
    )
  })

  it('keeps a changed managed schema so the backend can reject the mutation', () => {
    const content = `Instructions.\n\n${lockedV5Block.replace('jd-analysis/v5', 'jd-analysis/v999')}`

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe(content)
  })

  it('keeps an unknown legacy schema rather than silently deleting it', () => {
    const changedSchema = legacyV4Schema.replace('"operator": "all_of"', '"operator": "one_of"')
    const content = `Instructions.\n\nOUTPUT CONTRACT\n${changedSchema}\n\nEVIDENCE AND SOURCE RULES\nKeep evidence grounded.`

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe(content)
  })

  it('keeps content when managed markers are duplicated', () => {
    const content = `${lockedV5Block}\n\n${lockedV5Block}`

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe(content)
  })

  it('does not remove semantic prose that merely mentions a requirement group', () => {
    const content = 'Explain how requirement_groups should reflect the JD.'

    expect(sanitizeJdAnalysisContentForEditing(content)).toBe(content)
  })
})
