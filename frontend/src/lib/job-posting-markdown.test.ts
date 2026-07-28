import { describe, expect, it } from "vitest"
import {
  containsRawHtmlTag,
  continueMarkdownListOnEnter,
  getJobPostingMarkdownPlainText,
  hasJobPostingMarkdownVisibleText,
  normalizeJobPostingMarkdown,
  parseJobPostingMarkdown,
  toggleInlineMarkdown,
  toggleListMarkdown,
} from "@/lib/job-posting-markdown"

describe("job-posting markdown", () => {
  it("canonicalizes supported list syntax without touching visible text", () => {
    expect(normalizeJobPostingMarkdown("\r\n* React\r\n+ Node.js\r\n\r\n\r\nC++  "))
      .toBe("- React\n- Node.js\n\nC++")
  })

  it("repairs a malformed list marker before inline Markdown without changing content", () => {
    expect(normalizeJobPostingMarkdown("-**Lead backend delivery**")).toBe("- **Lead backend delivery**")
    expect(getJobPostingMarkdownPlainText("-**Lead backend delivery**")).toBe("Lead backend delivery")
    expect(parseJobPostingMarkdown("-**Lead backend delivery**", "bullet")).toEqual([
      { type: "unordered-list", items: ["**Lead backend delivery**"] },
    ])
  })

  it("projects supported Markdown to plain text without corrupting IT names", () => {
    expect(getJobPostingMarkdownPlainText("- **C++**\n1. _Node.js_\n++CI/CD++\nsome_text_here\na < b"))
      .toBe("C++\nNode.js\nCI/CD\nsome_text_here\na < b")
  })

  it("rejects formatting-only content and detects actual HTML tags", () => {
    expect(hasJobPostingMarkdownVisibleText("****\n-\n++++")).toBe(false)
    expect(containsRawHtmlTag("a < b")).toBe(false)
    expect(containsRawHtmlTag("<script>alert(1)</script>")).toBe(true)
  })

  it("preserves legacy display behavior and parses explicit Markdown blocks", () => {
    expect(parseJobPostingMarkdown("React\nNode.js", "bullet")).toEqual([
      { type: "unordered-list", items: ["React", "Node.js"] },
    ])
    expect(parseJobPostingMarkdown("Income line\nSecond line", "lines")).toEqual([
      { type: "paragraph", lines: ["Income line", "Second line"] },
    ])
    expect(parseJobPostingMarkdown("Intro\n\n- React\n- Node.js\n\n2. Deploy", "bullet")).toEqual([
      { type: "paragraph", lines: ["Intro"] },
      { type: "unordered-list", items: ["React", "Node.js"] },
      { type: "ordered-list", items: [{ ordinal: 2, text: "Deploy" }] },
    ])
  })

  it("keeps formatting that spans a selected line break as one Markdown paragraph", () => {
    const value = "- First responsibility\n\n**Own the API\nlifecycle**"

    expect(getJobPostingMarkdownPlainText(value)).toBe("First responsibility\n\nOwn the API\nlifecycle")
    expect(parseJobPostingMarkdown(value, "bullet")).toEqual([
      { type: "unordered-list", items: ["First responsibility"] },
      { type: "paragraph", lines: ["**Own the API", "lifecycle**"] },
    ])
  })

  it("transforms selections and continues Markdown lists", () => {
    expect(toggleInlineMarkdown("React", 0, 5, "**")).toEqual({
      value: "**React**",
      selectionStart: 2,
      selectionEnd: 7,
    })
    expect(toggleListMarkdown("React\nNode.js", 0, 13, "ordered").value).toBe("1. React\n2. Node.js")
    expect(toggleListMarkdown("-**React**", 0, 10, "unordered").value).toBe("**React**")
    expect(continueMarkdownListOnEnter("- React", 7)).toEqual({
      value: "- React\n- ",
      selectionStart: 10,
      selectionEnd: 10,
    })
  })
})
