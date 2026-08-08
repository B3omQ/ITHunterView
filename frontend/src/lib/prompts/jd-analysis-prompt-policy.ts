const JD_ANALYSIS_PROMPT_KEYS = new Set([
  'JD_ANALYSIS_V2_SYSTEM',
  'JD_ANALYSIS_V2_USER',
])

const LOCKED_SCHEMA_BEGIN = '--- BEGIN LOCKED JD ANALYSIS OUTPUT SCHEMA ---'
const LOCKED_SCHEMA_END = '--- END LOCKED JD ANALYSIS OUTPUT SCHEMA ---'
const LEGACY_SCHEMA_BEGIN = 'OUTPUT CONTRACT'
const LEGACY_SCHEMA_END = 'EVIDENCE AND SOURCE RULES'

export function isJdAnalysisPromptKey(promptKey?: string): boolean {
  return Boolean(promptKey && JD_ANALYSIS_PROMPT_KEYS.has(promptKey))
}

export function sanitizeJdAnalysisContentForEditing(content: string): string {
  const withoutLockedBlock = removeSingleDelimitedBlock(content, LOCKED_SCHEMA_BEGIN, LOCKED_SCHEMA_END)
  if (withoutLockedBlock !== content) {
    return withoutLockedBlock
  }

  return removeSingleDelimitedBlock(content, LEGACY_SCHEMA_BEGIN, LEGACY_SCHEMA_END, true)
}

function removeSingleDelimitedBlock(
  content: string,
  startMarker: string,
  endMarker: string,
  keepEndMarker = false,
): string {
  const startIndex = content.indexOf(startMarker)
  if (startIndex < 0 || content.indexOf(startMarker, startIndex + startMarker.length) >= 0) {
    return content
  }

  const endIndex = content.indexOf(endMarker, startIndex + startMarker.length)
  if (endIndex < 0 || content.indexOf(endMarker, endIndex + endMarker.length) >= 0) {
    return content
  }

  const prefix = content.slice(0, startIndex).trimEnd()
  const suffix = content.slice(keepEndMarker ? endIndex : endIndex + endMarker.length).trimStart()
  return [prefix, suffix].filter(Boolean).join('\n\n')
}
