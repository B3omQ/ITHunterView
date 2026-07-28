"use client"

import { useRef, useState } from "react"
import { Bold, Italic, List, ListOrdered, Underline } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"
import {
  containsRawHtmlTag,
  continueMarkdownListOnEnter,
  hasJobPostingMarkdownVisibleText,
  normalizeJobPostingMarkdown,
  toggleInlineMarkdown,
  toggleListMarkdown,
  type MarkdownSelectionResult,
} from "@/lib/job-posting-markdown"

interface JobPostingRichTextEditorProps {
  id: string
  value: string
  onChange: (nextValue: string) => void
  maxLength: number
  placeholder?: string
  disabled?: boolean
  required?: boolean
  error?: string
  describedBy?: string
}

interface ToolbarButtonProps {
  label: string
  disabled?: boolean
  onClick: () => void
  children: React.ReactNode
}

function ToolbarButton({ label, disabled = false, onClick, children }: ToolbarButtonProps) {
  const button = (
    <Button
      type="button"
      variant="ghost"
      size="icon"
      className="size-8"
      aria-label={label}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </Button>
  )

  if (disabled) {
    return (
      <Tooltip>
        <TooltipTrigger render={<span className="inline-flex cursor-not-allowed" />}>
          <span className="pointer-events-none">{button}</span>
        </TooltipTrigger>
        <TooltipContent>{label}</TooltipContent>
      </Tooltip>
    )
  }

  return (
    <Tooltip>
      <TooltipTrigger render={button} />
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}

export function JobPostingRichTextEditor({
  id,
  value,
  onChange,
  maxLength,
  placeholder,
  disabled = false,
  required = false,
  error,
  describedBy,
}: JobPostingRichTextEditorProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const [selection, setSelection] = useState({ start: 0, end: 0 })
  const normalizedValue = normalizeJobPostingMarkdown(value)
  const localError =
    value.trim() && !hasJobPostingMarkdownVisibleText(value)
      ? "Add visible text, not formatting characters only."
      : containsRawHtmlTag(value)
        ? "HTML tags are not supported."
        : normalizedValue.length > maxLength
          ? `Content must not exceed ${maxLength.toLocaleString()} characters after formatting.`
          : ""
  const resolvedError = error || localError
  const canApplyInline = !disabled && selection.start !== selection.end
  const canApplyList = !disabled && value.length > 0

  const restoreSelection = (result: MarkdownSelectionResult) => {
    onChange(result.value)
    window.requestAnimationFrame(() => {
      const textarea = textareaRef.current
      if (!textarea) return
      textarea.focus()
      textarea.setSelectionRange(result.selectionStart, result.selectionEnd)
      setSelection({ start: result.selectionStart, end: result.selectionEnd })
    })
  }

  const getSelection = () => {
    const textarea = textareaRef.current
    return {
      start: textarea?.selectionStart ?? selection.start,
      end: textarea?.selectionEnd ?? selection.end,
    }
  }

  const applyInline = (delimiter: "**" | "_" | "++") => {
    const currentSelection = getSelection()
    const result = toggleInlineMarkdown(value, currentSelection.start, currentSelection.end, delimiter)
    if (result) restoreSelection(result)
  }

  const applyList = (kind: "unordered" | "ordered") => {
    const currentSelection = getSelection()
    restoreSelection(toggleListMarkdown(value, currentSelection.start, currentSelection.end, kind))
  }

  return (
    <TooltipProvider>
      <div className="overflow-hidden rounded-lg border border-input bg-background">
        <div className="flex flex-wrap items-center gap-1 border-b bg-muted/30 p-1.5">
          <ToolbarButton label="Bold" disabled={!canApplyInline} onClick={() => applyInline("**")}>
            <Bold className="size-4" aria-hidden="true" />
          </ToolbarButton>
          <ToolbarButton label="Italic" disabled={!canApplyInline} onClick={() => applyInline("_")}>
            <Italic className="size-4" aria-hidden="true" />
          </ToolbarButton>
          <ToolbarButton label="Underline" disabled={!canApplyInline} onClick={() => applyInline("++")}>
            <Underline className="size-4" aria-hidden="true" />
          </ToolbarButton>
          <span className="mx-1 h-5 w-px bg-border" aria-hidden="true" />
          <ToolbarButton label="Bulleted list" disabled={!canApplyList} onClick={() => applyList("unordered")}>
            <List className="size-4" aria-hidden="true" />
          </ToolbarButton>
          <ToolbarButton label="Numbered list" disabled={!canApplyList} onClick={() => applyList("ordered")}>
            <ListOrdered className="size-4" aria-hidden="true" />
          </ToolbarButton>
        </div>

        <Textarea
          ref={textareaRef}
          id={id}
          value={value}
          placeholder={placeholder}
          disabled={disabled}
          required={required}
          aria-invalid={Boolean(resolvedError)}
          aria-describedby={[describedBy, resolvedError ? `${id}-error` : undefined].filter(Boolean).join(" ") || undefined}
          className="min-h-32 resize-y rounded-none border-0 bg-background shadow-none focus-visible:border-0 focus-visible:ring-0"
          onChange={(event) => onChange(event.target.value)}
          onSelect={(event) => setSelection({ start: event.currentTarget.selectionStart, end: event.currentTarget.selectionEnd })}
          onKeyDown={(event) => {
            const isModifierShortcut = (event.ctrlKey || event.metaKey) && !event.altKey
            const shortcutDelimiter =
              isModifierShortcut && event.key.toLowerCase() === "b"
                ? "**"
                : isModifierShortcut && event.key.toLowerCase() === "i"
                  ? "_"
                  : isModifierShortcut && event.key.toLowerCase() === "u"
                    ? "++"
                    : null

            if (shortcutDelimiter) {
              const currentSelection = getSelection()
              const result = toggleInlineMarkdown(value, currentSelection.start, currentSelection.end, shortcutDelimiter)
              if (result) {
                event.preventDefault()
                restoreSelection(result)
              }
              return
            }

            if (event.key !== "Enter" || event.shiftKey || event.currentTarget.selectionStart !== event.currentTarget.selectionEnd) {
              return
            }

            const result = continueMarkdownListOnEnter(value, event.currentTarget.selectionStart)
            if (result) {
              event.preventDefault()
              restoreSelection(result)
            }
          }}
        />
      </div>

      <div className="mt-1.5 flex flex-col gap-1 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
        <span id={describedBy}>Use the toolbar for lists and formatting. Press Enter twice to start a new paragraph.</span>
        <span aria-live="polite">{normalizedValue.length.toLocaleString()} / {maxLength.toLocaleString()}</span>
      </div>
      {resolvedError ? (
        <p id={`${id}-error`} className="mt-1 text-xs text-destructive" role="alert">
          {resolvedError}
        </p>
      ) : null}
    </TooltipProvider>
  )
}
