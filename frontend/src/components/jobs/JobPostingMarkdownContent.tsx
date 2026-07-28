import React from "react"
import { cn } from "@/lib/utils"
import {
  parseJobPostingInlineMarkdown,
  parseJobPostingMarkdown,
  type RichTextLegacyMode,
} from "@/lib/job-posting-markdown"

interface JobPostingMarkdownContentProps {
  value: string | null | undefined
  legacyMode: RichTextLegacyMode
  emptyFallback?: React.ReactNode
  className?: string
  itemClassName?: string
}

function InlineMarkdown({ text }: { text: string }) {
  return parseJobPostingInlineMarkdown(text).map((token, index) => {
    if (token.type === "strong") return <strong key={index} className="font-semibold text-foreground">{token.value}</strong>
    if (token.type === "emphasis") return <em key={index}>{token.value}</em>
    if (token.type === "underline") return <span key={index} className="underline underline-offset-2">{token.value}</span>
    return <React.Fragment key={index}>{token.value}</React.Fragment>
  })
}

export function JobPostingMarkdownContent({
  value,
  legacyMode,
  emptyFallback = null,
  className,
  itemClassName,
}: JobPostingMarkdownContentProps) {
  const blocks = parseJobPostingMarkdown(value, legacyMode)
  if (!blocks.length) return emptyFallback ? <>{emptyFallback}</> : null

  return (
    <div className={cn("space-y-3 text-zinc-600 dark:text-zinc-400", className)}>
      {blocks.map((block, blockIndex) => {
        if (block.type === "unordered-list") {
          return (
            <ul key={blockIndex} className="list-inside list-disc space-y-1.5">
              {block.items.map((item, itemIndex) => (
                <li key={itemIndex} className={cn("leading-relaxed", itemClassName)}><InlineMarkdown text={item} /></li>
              ))}
            </ul>
          )
        }

        if (block.type === "ordered-list") {
          return (
            <ol key={blockIndex} start={block.items[0]?.ordinal} className="list-inside list-decimal space-y-1.5">
              {block.items.map((item, itemIndex) => (
                <li key={`${item.ordinal}-${itemIndex}`} value={item.ordinal} className={cn("leading-relaxed", itemClassName)}><InlineMarkdown text={item.text} /></li>
              ))}
            </ol>
          )
        }

        return (
          <p key={blockIndex} className={cn("whitespace-pre-line leading-relaxed", itemClassName)}>
            <InlineMarkdown text={block.lines.join("\n")} />
          </p>
        )
      })}
    </div>
  )
}
