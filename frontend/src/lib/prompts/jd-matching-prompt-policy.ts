const JD_MATCHING_PROMPT_KEY = 'JD_MATCHING_PROMPT'

const LOCKED_SCHEMA_BEGIN = '--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---'
const LOCKED_SCHEMA_END = '--- END LOCKED JD MATCHING OUTPUT SCHEMA ---'
const LEGACY_SCHEMA_BEGIN = 'SCHEMA OUTPUT BẮT BUỘC (Chỉ trả về JSON này, không có markdown block hay text thừa):'
const LEGACY_SCHEMA_END = 'HANDLER SCORING RULES (MANDATORY — follow exactly):'
const LEGACY_FORMAT_FOOTER = 'Chỉ trả về JSON hợp lệ. Bắt đầu bằng { và kết thúc bằng }.'

const KNOWN_SCHEMA_SIGNATURE = [
  '"scores"',
  '"reqId"',
  '"handlerScore"',
  '"criticalGaps"',
  '"penalties"',
  '"narrative"',
  '"improvements"',
]

export function isJdMatchingPromptKey(promptKey?: string): boolean {
  return promptKey === JD_MATCHING_PROMPT_KEY
}

/**
 * Removes only a recognizable copy of the application-managed output block.
 * The complete schema intentionally stays in backend code; an unknown or
 * malformed block is returned unchanged so the backend can reject it rather
 * than the editor silently deleting user content.
 */
export function sanitizeJdMatchingContentForEditing(content: string): string {
  const locked = removeKnownDelimitedBlock(content, LOCKED_SCHEMA_BEGIN, LOCKED_SCHEMA_END, false)
  if (locked !== content) {
    return removeLegacyFooter(locked)
  }

  const legacy = removeKnownDelimitedBlock(content, LEGACY_SCHEMA_BEGIN, LEGACY_SCHEMA_END, true)
  return legacy === content ? content : removeLegacyFooter(legacy)
}

function removeKnownDelimitedBlock(
  content: string,
  startMarker: string,
  endMarker: string,
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
  const block = content.slice(startIndex, blockEnd)
  if (!KNOWN_SCHEMA_SIGNATURE.every(signature => block.includes(signature))) {
    return content
  }

  const prefix = content.slice(0, startIndex).trimEnd()
  const suffix = content.slice(blockEnd).trimStart()
  return [prefix, suffix].filter(Boolean).join('\n\n')
}

function removeLegacyFooter(content: string): string {
  const first = content.indexOf(LEGACY_FORMAT_FOOTER)
  if (first < 0 || content.indexOf(LEGACY_FORMAT_FOOTER, first + LEGACY_FORMAT_FOOTER.length) >= 0) {
    return content
  }

  const prefix = content.slice(0, first).trimEnd()
  const suffix = content.slice(first + LEGACY_FORMAT_FOOTER.length).trimStart()
  return [prefix, suffix].filter(Boolean).join('\n\n')
}
