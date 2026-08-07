"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Logo } from "@/components/layout/Logo"
import { useAuthStore } from "@/store/auth.store"
import { getDashboardPath } from "@/lib/constants"
import { LayoutDashboard, LogOut } from "lucide-react"
import { LanguageSwitcher } from "@/components/shared/LanguageSwitcher"
import { useTranslations } from "next-intl"

export function PublicHeader() {
  const { user, logout } = useAuthStore()
  const [mounted, setMounted] = useState(false)
  const t = useTranslations("Layout.Header")

  useEffect(() => {
    setMounted(true)
  }, [])

  return (
    <header className="sticky top-0 z-50 bg-white/85 backdrop-blur-md border-b border-border transition-all">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between relative">
        <Logo size="md" href="/" />

        <nav className="hidden md:flex items-center gap-8 text-sm font-medium text-muted-foreground absolute left-1/2 -translate-x-1/2">
          <Link href="/jobs" className="hover:text-foreground transition-colors">{t('jobs')}</Link>
          <Link href="/#mock-interview" className="hover:text-foreground transition-colors">{t('mockInterview')}</Link>
          <Link href="/pricing" className="hover:text-foreground transition-colors">{t('pricing')}</Link>
        </nav>

        <div className="flex items-center gap-3">
          <LanguageSwitcher />
          
          {/* Only render auth buttons after mount to prevent hydration mismatch */}
          {!mounted ? (
            // Skeleton placeholder — same size as actual buttons, invisible
            <div className="flex items-center gap-3">
              <div className="h-10 w-24 rounded-xl bg-transparent" />
              <div className="h-10 w-28 rounded-xl bg-transparent" />
            </div>
          ) : user ? (
            <>
              <Link
                href={getDashboardPath(user.role?.name)}
                className="flex items-center whitespace-nowrap gap-1.5 h-10 px-4 rounded-xl border border-border text-sm font-medium hover:bg-muted transition-all"
              >
                <LayoutDashboard size={16} className="shrink-0 text-muted-foreground" />
                <span>{t('dashboard')}</span>
              </Link>
              <button
                onClick={async () => {
                  await logout()
                }}
                className="h-10 w-10 shrink-0 flex items-center justify-center rounded-xl border border-border hover:bg-red-500/10 hover:text-red-500 transition-all cursor-pointer"
                title={t('logout')}
              >
                <LogOut size={16} />
              </button>
            </>
          ) : (
            <>
              <Link
                href="/login"
                className="whitespace-nowrap h-10 px-4 rounded-xl text-sm font-medium text-foreground hover:bg-muted flex items-center justify-center transition-all"
              >
                {t('signIn')}
              </Link>
              <Link
                href="/register"
                className="whitespace-nowrap h-10 px-4 rounded-xl bg-primary hover:bg-primary/95 text-primary-foreground text-sm font-medium flex items-center justify-center shadow-sm transition-all"
              >
                {t('getStarted')}
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
