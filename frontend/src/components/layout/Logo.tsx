import Link from "next/link"

interface LogoProps {
  size?: "sm" | "md" | "lg"
  showText?: boolean
  href?: string
}

const sizes = {
  sm: { icon: 28, text: "text-base" },
  md: { icon: 36, text: "text-xl" },
  lg: { icon: 44, text: "text-2xl" },
}

export function Logo({ size = "md", showText = true, href = "/" }: LogoProps) {
  const s = sizes[size]

  const content = (
    <div className="flex items-center gap-2.5 select-none">
      {/* Icon */}
      <div
        className="rounded-xl bg-primary flex items-center justify-center shadow-lg shadow-indigo-500/25 flex-shrink-0"
        style={{ width: s.icon, height: s.icon }}
      >
        <svg
          width={s.icon * 0.58}
          height={s.icon * 0.58}
          viewBox="0 0 20 20"
          fill="none"
        >
          <rect x="2" y="3" width="16" height="11" rx="2" stroke="white" strokeWidth="1.6" />
          <path d="M6 17h8" stroke="white" strokeWidth="1.6" strokeLinecap="round" />
          <path d="M10 14v3" stroke="white" strokeWidth="1.6" strokeLinecap="round" />
          <path d="M7 8l2 2 4-4" stroke="white" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>
      {showText && (
        <span className={`font-bold ${s.text} text-foreground tracking-tight`}>
          IT<span className="text-primary">Hunter</span>View
        </span>
      )}
    </div>
  )

  if (href) {
    return <Link href={href} className="inline-flex">{content}</Link>
  }
  return content
}
