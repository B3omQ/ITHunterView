"use client"

import { useLocale } from "next-intl"
import { useRouter } from "next/navigation"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Globe } from "lucide-react"

export function LanguageSwitcher() {
  const locale = useLocale()
  const router = useRouter()

  const handleLanguageChange = (value: string | null) => {
    if (!value) return;
    document.cookie = `locale=${value}; path=/; max-age=31536000`
    router.refresh()
  }

  const LOCALE_LABELS: Record<string, string> = {
    en: "English",
    vi: "Tiếng Việt",
  }

  return (
    <Select value={locale} onValueChange={handleLanguageChange}>
      <SelectTrigger className="w-full h-10 px-3 gap-3 bg-transparent border-transparent rounded-xl text-sm font-medium text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/50 transition-all [&>svg:last-child]:ml-auto">
        <Globe className="w-4 h-4 text-muted-foreground" />
        <span>{LOCALE_LABELS[locale] ?? locale}</span>
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="en">English</SelectItem>
        <SelectItem value="vi">Tiếng Việt</SelectItem>
      </SelectContent>
    </Select>
  )
}
