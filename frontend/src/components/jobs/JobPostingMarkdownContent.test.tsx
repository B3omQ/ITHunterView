import { render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { JobPostingMarkdownContent } from "@/components/jobs/JobPostingMarkdownContent"

describe("JobPostingMarkdownContent", () => {
  it("renders supported lists and inline formatting as safe React elements", () => {
    const { container } = render(
      <JobPostingMarkdownContent
        legacyMode="bullet"
        value={"- **React**\n- _Node.js_\n\n1. ++Deploy++"}
      />,
    )

    expect(container.querySelector("ul")).toBeInTheDocument()
    expect(container.querySelector("ol")).toBeInTheDocument()
    expect(screen.getByText("React").tagName).toBe("STRONG")
    expect(screen.getByText("Node.js").tagName).toBe("EM")
    expect(screen.getByText("Deploy")).toHaveClass("underline")
  })

  it("renders HTML-looking historic text literally instead of creating a script node", () => {
    const { container } = render(
      <JobPostingMarkdownContent legacyMode="lines" value="<script>alert(1)</script>" />,
    )

    expect(screen.getByText("<script>alert(1)</script>")).toBeInTheDocument()
    expect(container.querySelector("script")).not.toBeInTheDocument()
  })

  it("keeps legacy job multiline text as bullets and income as lines", () => {
    const { rerender, container } = render(
      <JobPostingMarkdownContent legacyMode="bullet" value={"React\nNode.js"} />,
    )
    expect(container.querySelectorAll("li")).toHaveLength(2)

    rerender(<JobPostingMarkdownContent legacyMode="lines" value={"10M VND\nBonus"} />)
    expect(container.querySelectorAll("li")).toHaveLength(0)
    expect(screen.getByText(/10M VND/)).toBeInTheDocument()
  })

  it("renders formatting that wraps across a line break without exposing Markdown delimiters", () => {
    const { container } = render(
      <JobPostingMarkdownContent legacyMode="bullet" value={"- First item\n\n**Own the API\nlifecycle**"} />,
    )

    const strong = container.querySelector("strong")
    expect(strong?.textContent).toBe("Own the API\nlifecycle")
    expect(container.textContent).not.toContain("**")
  })
})
