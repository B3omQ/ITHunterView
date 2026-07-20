import Image from "next/image"
import Link from "next/link"

interface LogoProps {
  size?: "sm" | "md" | "lg"
  showText?: boolean
  href?: string
}

// Logo03.png là mascot vuông (chỉ icon, không có text)
// → luôn kết hợp icon + text thủ công

const iconSizes = {
  sm: 36,
  md: 36,
  lg: 56,
}

const textSizes = {
  sm: "text-base",
  md: "text-xl",
  lg: "text-2xl",
}

export function Logo({ size = "md", showText = true, href = "/" }: LogoProps) {
  const iconPx = iconSizes[size]

  const content = (
    <div className="flex items-center gap-2 select-none flex-shrink-0">
      {/* Mascot icon */}
      <Image
        src="/images/Logo03.png"
        alt="IT HunterView icon"
        width={iconPx}
        height={iconPx}
        className="rounded-xl flex-shrink-0"
        priority
      />

      {/* Brand text */}
      {showText && (
        <span className={`font-bold ${textSizes[size]} tracking-tight text-foreground leading-none`}>
          IT<span className="text-primary">Hunter</span>View
        </span>
      )}
    </div>
  )

  if (href) {
    return <Link href={href} className="inline-flex items-center">{content}</Link>
  }
  return content
}

