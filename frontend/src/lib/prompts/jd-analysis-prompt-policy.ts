const JD_ANALYSIS_PROMPT_KEYS = new Set([
  'JD_ANALYSIS_V2_SYSTEM',
  'JD_ANALYSIS_V2_USER',
])

const LOCKED_SCHEMA_BEGIN = '--- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---'
const LOCKED_SCHEMA_END = '--- END LOCKED JD ANALYSIS OUTPUT SCHEMA ---'
const LEGACY_SCHEMA_BEGIN = 'OUTPUT CONTRACT'
const LEGACY_SCHEMA_END = 'EVIDENCE AND SOURCE RULES'

const KNOWN_LOCKED_V5_BLOCK = `--- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---
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

const KNOWN_LEGACY_V4_SCHEMA = `{
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

export function isJdAnalysisPromptKey(promptKey?: string): boolean {
  return Boolean(promptKey && JD_ANALYSIS_PROMPT_KEYS.has(promptKey))
}

export function sanitizeJdAnalysisContentForEditing(content: string): string {
  const withoutLockedBlock = removeExactLockedV5Block(content)
  if (withoutLockedBlock !== content) {
    return withoutLockedBlock
  }

  return removeExactLegacyV4Contract(content)
}

function removeExactLockedV5Block(content: string): string {
  const startIndex = findSingleMarker(content, LOCKED_SCHEMA_BEGIN)
  const endIndex = findSingleMarker(content, LOCKED_SCHEMA_END)
  if (startIndex < 0 || endIndex < startIndex) {
    return content
  }

  const blockEnd = endIndex + LOCKED_SCHEMA_END.length
  const block = content.slice(startIndex, blockEnd)
  if (normalizeText(block) !== normalizeText(KNOWN_LOCKED_V5_BLOCK)) {
    return content
  }

  return joinSections(content.slice(0, startIndex), content.slice(blockEnd))
}

function removeExactLegacyV4Contract(content: string): string {
  const startIndex = findSingleMarker(content, LEGACY_SCHEMA_BEGIN)
  const endMarkerIndex = findSingleMarker(content, LEGACY_SCHEMA_END)
  if (startIndex < 0 || endMarkerIndex < startIndex) {
    return content
  }

  const json = extractBalancedJsonObject(
    content,
    startIndex + LEGACY_SCHEMA_BEGIN.length,
    endMarkerIndex,
  )
  if (!json || canonicalJson(json.text) !== canonicalJson(KNOWN_LEGACY_V4_SCHEMA)) {
    return content
  }

  return joinSections(content.slice(0, startIndex), content.slice(endMarkerIndex))
}

function findSingleMarker(content: string, marker: string): number {
  const first = content.indexOf(marker)
  if (first < 0 || content.indexOf(marker, first + marker.length) >= 0) {
    return -1
  }

  return first
}

function extractBalancedJsonObject(
  content: string,
  searchStart: number,
  searchEnd: number,
): { text: string; endIndex: number } | null {
  const objectStart = content.indexOf('{', searchStart)
  if (objectStart < 0 || objectStart >= searchEnd) {
    return null
  }

  let depth = 0
  let inString = false
  let escaped = false
  for (let index = objectStart; index < searchEnd; index += 1) {
    const character = content[index]
    if (inString) {
      if (escaped) {
        escaped = false
      } else if (character === '\\') {
        escaped = true
      } else if (character === '"') {
        inString = false
      }
      continue
    }

    if (character === '"') {
      inString = true
    } else if (character === '{') {
      depth += 1
    } else if (character === '}') {
      depth -= 1
      if (depth === 0) {
        return { text: content.slice(objectStart, index + 1), endIndex: index + 1 }
      }
    }
  }

  return null
}

function canonicalJson(value: string): string | null {
  try {
    return JSON.stringify(JSON.parse(value))
  } catch {
    return null
  }
}

function normalizeText(value: string): string {
  return value.replace(/\r\n?/g, '\n').trim()
}

function joinSections(prefix: string, suffix: string): string {
  return [prefix.trimEnd(), suffix.trimStart()].filter(Boolean).join('\n\n')
}
