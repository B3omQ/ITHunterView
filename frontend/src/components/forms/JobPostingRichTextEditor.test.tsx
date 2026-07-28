import { useState } from "react"
import { fireEvent, render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { JobPostingRichTextEditor } from "@/components/forms/JobPostingRichTextEditor"

function EditorHarness({ initialValue = "React" }: { initialValue?: string }) {
  const [value, setValue] = useState(initialValue)
  return <JobPostingRichTextEditor id="description" value={value} onChange={setValue} maxLength={10_000} />
}

describe("JobPostingRichTextEditor", () => {
  it("exposes accessible toolbar controls and wraps a selected value", () => {
    render(<EditorHarness />)
    const textarea = screen.getByRole("textbox") as HTMLTextAreaElement
    textarea.focus()
    textarea.setSelectionRange(0, 5)
    fireEvent.select(textarea)

    expect(screen.getByRole("button", { name: "Bold" })).toHaveAttribute("type", "button")
    fireEvent.click(screen.getByRole("button", { name: "Bold" }))

    expect(textarea.value).toBe("**React**")
  })

  it("does not enable inline formatting without a selection", () => {
    render(<EditorHarness />)
    expect(screen.getByRole("button", { name: "Italic" })).toBeDisabled()
  })

  it("continues an explicit bullet list on Enter", () => {
    render(<EditorHarness initialValue="- React" />)
    const textarea = screen.getByRole("textbox") as HTMLTextAreaElement
    textarea.focus()
    textarea.setSelectionRange(7, 7)
    fireEvent.keyDown(textarea, { key: "Enter" })

    expect(textarea.value).toBe("- React\n- ")
  })

  it("shows a local error instead of silently accepting HTML", () => {
    render(<EditorHarness initialValue="<script>alert(1)</script>" />)
    expect(screen.getByRole("alert")).toHaveTextContent("HTML tags are not supported")
  })
})
