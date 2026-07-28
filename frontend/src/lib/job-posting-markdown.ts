export const JOB_POSTING_RICH_TEXT_LIMITS = {
  description: 10_000,
  requirements: 10_000,
  benefits: 10_000,
  incomeText: 4_000,
} as const

export type JobPostingRichTextField = keyof typeof JOB_POSTING_RICH_TEXT_LIMITS
export type RichTextLegacyMode = "bullet" | "lines"

export type JobPostingMarkdownBlock =
  | { type: "paragraph"; lines: string[] }
  | { type: "unordered-list"; items: string[] }
  | { type: "ordered-list"; items: Array<{ ordinal: number; text: string }> }

export type JobPostingInlineToken =
  | { type: "text"; value: string }
  | { type: "strong" | "emphasis" | "underline"; value: string }

export interface MarkdownSelectionResult {
  value: string
  selectionStart: number
  selectionEnd: number
}

const unorderedListPattern = /^[-*+]\s+(.+)$/
const orderedListPattern = /^(\d+)\.\s+(.+)$/
const rawHtmlTagPattern = /<\/?[A-Za-z][A-Za-z0-9:-]*(?:\s+[^<>]*)?\s*\/?>/

function normalizeInput(value: string | null | undefined): string {
  return (value ?? "")
    .normalize("NFKC")
    .replace(/\r\n?/g, "\n")
    .replace(/\u00A0/g, " ")
    .replace(/\t/g, " ")
}

function collapseHorizontalWhitespace(value: string): string {
  return value.replace(/[ \f\v]+/g, " ")
}

function normalizeMalformedUnorderedListMarker(value: string): string {
  // A recruiter can apply inline formatting immediately after a manually typed
  // hyphen, producing "-**text**". Canonical Markdown requires "- **text**".
  return value.replace(/^-(?=(?:\*\*|_|\+\+))/, "- ")
}

function canonicalizeLine(value: string): string {
  const line = normalizeMalformedUnorderedListMarker(collapseHorizontalWhitespace(value).trim())
  if (!line) return ""

  const unordered = line.match(unorderedListPattern)
  if (unordered) return `- ${unordered[1].trim()}`

  const ordered = line.match(orderedListPattern)
  if (ordered && Number.parseInt(ordered[1], 10) > 0) {
    return `${Number.parseInt(ordered[1], 10)}. ${ordered[2].trim()}`
  }

  return line
}

export function normalizeJobPostingMarkdown(value: string | null | undefined): string {
  const lines: string[] = []
  let previousWasBlank = false

  for (const rawLine of normalizeInput(value).split("\n")) {
    const line = canonicalizeLine(rawLine)
    const isBlank = line.length === 0
    if (isBlank && (previousWasBlank || lines.length === 0)) continue

    lines.push(line)
    previousWasBlank = isBlank
  }

  while (lines.at(-1) === "") lines.pop()
  return lines.join("\n")
}

function canOpenUnderscoreDelimiter(value: string, index: number): boolean {
  const hasWordBefore = index > 0 && /[\p{L}\p{N}]/u.test(value[index - 1])
  const hasWordAfter = index + 1 < value.length && /[\p{L}\p{N}]/u.test(value[index + 1])
  return !(hasWordBefore && hasWordAfter)
}

function readDelimited(
  value: string,
  startIndex: number,
  delimiter: "**" | "_" | "++",
): { content: string; nextIndex: number } | null {
  if (!value.startsWith(delimiter, startIndex)) return null

  const contentStart = startIndex + delimiter.length
  const closingIndex = value.indexOf(delimiter, contentStart)
  if (closingIndex <= contentStart) return null

  return {
    content: value.slice(contentStart, closingIndex),
    nextIndex: closingIndex + delimiter.length,
  }
}

export function parseJobPostingInlineMarkdown(text: string): JobPostingInlineToken[] {
  const tokens: JobPostingInlineToken[] = []
  let plainText = ""

  const pushPlainText = () => {
    if (plainText) {
      tokens.push({ type: "text", value: plainText })
      plainText = ""
    }
  }

  for (let index = 0; index < text.length; ) {
    const bold = readDelimited(text, index, "**")
    if (bold) {
      pushPlainText()
      tokens.push({ type: "strong", value: bold.content })
      index = bold.nextIndex
      continue
    }

    const underline = readDelimited(text, index, "++")
    if (underline) {
      pushPlainText()
      tokens.push({ type: "underline", value: underline.content })
      index = underline.nextIndex
      continue
    }

    const italic =
      text[index] === "_" && canOpenUnderscoreDelimiter(text, index)
        ? readDelimited(text, index, "_")
        : null
    if (italic) {
      pushPlainText()
      tokens.push({ type: "emphasis", value: italic.content })
      index = italic.nextIndex
      continue
    }

    plainText += text[index]
    index += 1
  }

  pushPlainText()
  return tokens
}

function stripInlineMarkdown(value: string): string {
  return parseJobPostingInlineMarkdown(value)
    .map((token) => token.value)
    .join("")
}

function stripListMarker(value: string): string {
  const trimmed = normalizeMalformedUnorderedListMarker(value.trim())
  const unordered = trimmed.match(unorderedListPattern)
  if (unordered) return unordered[1]

  const ordered = trimmed.match(orderedListPattern)
  return ordered ? ordered[2] : trimmed
}

export function getJobPostingMarkdownPlainText(value: string | null | undefined): string {
  const withoutListMarkers = normalizeInput(value)
    .split("\n")
    .map(stripListMarker)
    .join("\n")
  const withoutInlineFormatting = stripInlineMarkdown(withoutListMarkers)
  const lines: string[] = []
  let previousWasBlank = false

  for (const rawLine of withoutInlineFormatting.split("\n")) {
    const line = collapseHorizontalWhitespace(rawLine).trim()
    const isBlank = line.length === 0
    if (isBlank && (previousWasBlank || lines.length === 0)) continue

    lines.push(line)
    previousWasBlank = isBlank
  }

  while (lines.at(-1) === "") lines.pop()
  return lines.join("\n")
}

export function hasJobPostingMarkdownVisibleText(value: string | null | undefined): boolean {
  return getJobPostingMarkdownPlainText(value)
    .split("\n")
    .some((line) => /[^*_+\-\s]/.test(line))
}

export function containsRawHtmlTag(value: string | null | undefined): boolean {
  return rawHtmlTagPattern.test(value ?? "")
}

function hasBalancedInlineToken(value: string): boolean {
  return parseJobPostingInlineMarkdown(value).some((token) => token.type !== "text")
}

function isExplicitListLine(value: string): boolean {
  return unorderedListPattern.test(value) || orderedListPattern.test(value)
}

function getLegacyBlocks(value: string, legacyMode: RichTextLegacyMode): JobPostingMarkdownBlock[] {
  const lines = value.split("\n").filter(Boolean)
  if (legacyMode === "bullet") {
    return lines.length ? [{ type: "unordered-list", items: lines }] : []
  }

  return lines.length ? [{ type: "paragraph", lines }] : []
}

export function parseJobPostingMarkdown(
  value: string | null | undefined,
  legacyMode: RichTextLegacyMode,
): JobPostingMarkdownBlock[] {
  const normalized = normalizeJobPostingMarkdown(value)
  if (!normalized) return []

  const lines = normalized.split("\n")
  const hasBlankParagraphSeparator = lines.includes("")
  const hasExplicitList = lines.some(isExplicitListLine)
  // Inline formatting may deliberately span a selected line break, e.g.
  // "**Own the API\n// lifecycle**". Detect it before splitting into blocks.
  const hasInlineFormatting = hasBalancedInlineToken(normalized)

  if (!hasBlankParagraphSeparator && !hasExplicitList && !hasInlineFormatting) {
    return getLegacyBlocks(normalized, legacyMode)
  }

  const blocks: JobPostingMarkdownBlock[] = []
  let paragraphLines: string[] = []
  let unorderedItems: string[] = []
  let orderedItems: Array<{ ordinal: number; text: string }> = []

  const flushParagraph = () => {
    if (paragraphLines.length) blocks.push({ type: "paragraph", lines: paragraphLines })
    paragraphLines = []
  }
  const flushUnordered = () => {
    if (unorderedItems.length) blocks.push({ type: "unordered-list", items: unorderedItems })
    unorderedItems = []
  }
  const flushOrdered = () => {
    if (orderedItems.length) blocks.push({ type: "ordered-list", items: orderedItems })
    orderedItems = []
  }
  const flushAll = () => {
    flushParagraph()
    flushUnordered()
    flushOrdered()
  }

  for (const line of lines) {
    if (!line) {
      flushAll()
      continue
    }

    const unordered = line.match(unorderedListPattern)
    if (unordered) {
      flushParagraph()
      flushOrdered()
      unorderedItems.push(unordered[1])
      continue
    }

    const ordered = line.match(orderedListPattern)
    if (ordered) {
      flushParagraph()
      flushUnordered()
      orderedItems.push({ ordinal: Number.parseInt(ordered[1], 10), text: ordered[2] })
      continue
    }

    flushUnordered()
    flushOrdered()
    paragraphLines.push(line)
  }

  flushAll()
  return blocks
}

export function toggleInlineMarkdown(
  value: string,
  start: number,
  end: number,
  delimiter: "**" | "_" | "++",
): MarkdownSelectionResult | null {
  if (start === end || start < 0 || end > value.length || start > end) return null

  const selected = value.slice(start, end)
  const isWrapped =
    selected.startsWith(delimiter) &&
    selected.endsWith(delimiter) &&
    selected.length > delimiter.length * 2

  if (isWrapped) {
    const nextValue = `${value.slice(0, start)}${selected.slice(delimiter.length, -delimiter.length)}${value.slice(end)}`
    return {
      value: nextValue,
      selectionStart: start,
      selectionEnd: end - delimiter.length * 2,
    }
  }

  return {
    value: `${value.slice(0, start)}${delimiter}${selected}${delimiter}${value.slice(end)}`,
    selectionStart: start + delimiter.length,
    selectionEnd: end + delimiter.length,
  }
}

function getLineRange(value: string, start: number, end: number): { start: number; end: number } {
  const rangeStart = value.lastIndexOf("\n", Math.max(0, start - 1)) + 1
  const rangeEndAt = value.indexOf("\n", end)
  return { start: rangeStart, end: rangeEndAt === -1 ? value.length : rangeEndAt }
}

function removeAnyListMarker(line: string): string {
  return normalizeMalformedUnorderedListMarker(line).replace(/^[-*+]\s+/, "").replace(/^\d+\.\s+/, "")
}

export function toggleListMarkdown(
  value: string,
  start: number,
  end: number,
  kind: "unordered" | "ordered",
): MarkdownSelectionResult {
  const range = getLineRange(value, start, end)
  const selectedLines = value.slice(range.start, range.end).split("\n").map(normalizeMalformedUnorderedListMarker)
  const pattern = kind === "unordered" ? unorderedListPattern : orderedListPattern
  const allAreTargetList = selectedLines.filter(Boolean).every((line) => pattern.test(line))

  const replacement = allAreTargetList
    ? selectedLines.map((line) => (line ? removeAnyListMarker(line) : line)).join("\n")
    : selectedLines
        .map((line, index) => {
          if (!line) return line
          const item = removeAnyListMarker(line)
          return kind === "unordered" ? `- ${item}` : `${index + 1}. ${item}`
        })
        .join("\n")

  return {
    value: `${value.slice(0, range.start)}${replacement}${value.slice(range.end)}`,
    selectionStart: range.start,
    selectionEnd: range.start + replacement.length,
  }
}

export function continueMarkdownListOnEnter(value: string, caret: number): MarkdownSelectionResult | null {
  const range = getLineRange(value, caret, caret)
  if (caret !== range.end) return null

  const currentLine = normalizeMalformedUnorderedListMarker(value.slice(range.start, range.end))
  const unordered = currentLine.match(unorderedListPattern)
  if (unordered) {
    return {
      value: `${value.slice(0, caret)}\n- ${value.slice(caret)}`,
      selectionStart: caret + 3,
      selectionEnd: caret + 3,
    }
  }

  const ordered = currentLine.match(orderedListPattern)
  if (ordered) {
    const nextOrdinal = Number.parseInt(ordered[1], 10) + 1
    const marker = `${nextOrdinal}. `
    return {
      value: `${value.slice(0, caret)}\n${marker}${value.slice(caret)}`,
      selectionStart: caret + marker.length + 1,
      selectionEnd: caret + marker.length + 1,
    }
  }

  if (/^[-*+]\s*$/.test(currentLine) || /^\d+\.\s*$/.test(currentLine)) {
    const nextValue = `${value.slice(0, range.start)}${value.slice(range.end)}`
    return { value: nextValue, selectionStart: range.start, selectionEnd: range.start }
  }

  return null
}
