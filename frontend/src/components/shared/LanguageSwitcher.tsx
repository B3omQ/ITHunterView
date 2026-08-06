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

  return (
    <Select value={locale} onValueChange={handleLanguageChange}>
      <SelectTrigger className="w-[140px] h-9 flex gap-2 bg-transparent border-transparent hover:bg-accent hover:text-accent-foreground">
        <Globe className="w-4 h-4 text-muted-foreground" />
        <SelectValue placeholder="Language" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="en">English</SelectItem>
        <SelectItem value="vi">Tiếng Việt</SelectItem>
      </SelectContent>
    </Select>
  )
}
