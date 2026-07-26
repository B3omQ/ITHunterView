import React from "react";
import { cn } from "@/lib/utils";
import { splitTextLines, normalizeMultilineText } from "@/lib/job-posting-text";

export interface JobTextContentProps {
  value: string | null | undefined;
  variant: "bullet" | "lines";
  allowInlineStrong?: boolean;
  emptyFallback?: React.ReactNode;
  className?: string;
  itemClassName?: string;
}

function renderInlineStrong(text: string): React.ReactNode {
  const parts = text.split(/(\*\*[^*]+\*\*)/g);
  return parts.map((part, index) => {
    if (part.startsWith("**") && part.endsWith("**") && part.length > 4) {
      const innerText = part.slice(2, -2);
      return (
        <strong key={index} className="font-semibold text-zinc-900 dark:text-zinc-100">
          {innerText}
        </strong>
      );
    }
    return part;
  });
}

export const JobTextContent: React.FC<JobTextContentProps> = ({
  value,
  variant,
  allowInlineStrong = false,
  emptyFallback = null,
  className,
  itemClassName,
}) => {
  if (!value || !value.trim()) {
    return emptyFallback ? <>{emptyFallback}</> : null;
  }

  if (variant === "bullet") {
    const lines = splitTextLines(value);
    if (lines.length === 0) {
      return emptyFallback ? <>{emptyFallback}</> : null;
    }

    return (
      <ul className={cn("space-y-1.5 list-disc list-inside text-zinc-600 dark:text-zinc-400", className)}>
        {lines.map((line, idx) => (
          <li key={idx} className={cn("leading-relaxed", itemClassName)}>
            {allowInlineStrong ? renderInlineStrong(line) : line}
          </li>
        ))}
      </ul>
    );
  }

  const normalized = normalizeMultilineText(value);
  if (!normalized) {
    return emptyFallback ? <>{emptyFallback}</> : null;
  }

  const lines = normalized.split("\n").map((l) => l.trimEnd()).filter((l) => l.length > 0);
  return (
    <div className={cn("space-y-1.5 text-zinc-600 dark:text-zinc-400", className)}>
      {lines.map((line, idx) => (
        <p key={idx} className={cn("leading-relaxed", itemClassName)}>
          {allowInlineStrong ? renderInlineStrong(line) : line}
        </p>
      ))}
    </div>
  );
};
