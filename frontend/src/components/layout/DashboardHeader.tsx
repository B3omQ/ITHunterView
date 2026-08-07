"use client"

import React from "react"
import Link from "next/link"
import Image from "next/image"
import { Bell, LogOut, Coins, PlusCircle, User as UserIcon, Settings, ChevronDown, Globe, CreditCard, History, Search } from "lucide-react"
import { useAuthStore } from "@/store/auth.store"
import { Logo } from "@/components/layout/Logo"
import { Input } from "@/components/ui/input"
import { useWalletBalance } from "@/hooks/useWallet"
import { NotificationDialog } from "@/components/shared/NotificationDialog"
import { LanguageSwitcher } from "@/components/shared/LanguageSwitcher"
import { useQuery } from "@tanstack/react-query"
import { notificationService } from "@/services/notification.service"
import { useTranslations, useLocale } from "next-intl"
import { useRouter } from "next/navigation"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"

export function DashboardHeader() {
  const { user, logout } = useAuthStore()
  const router = useRouter()
  const locale = useLocale()
  const t = useTranslations("Layout.Sidebar") // Reusing same translations for now
  const [isNotificationOpen, setIsNotificationOpen] = React.useState(false)
  const [avatarError, setAvatarError] = React.useState(false)
  const [isAvatarLoaded, setIsAvatarLoaded] = React.useState(false)

  const isRecruiter = user?.role?.name?.toLowerCase() === "recruiter"

  // Get Wallet Balance & Subscription
  const { data: walletData, isLoading: walletLoading } = useWalletBalance()
  const balance = walletData?.data?.balance ?? 0
  const activeSubName = walletData?.data?.activeSubscriptionName

  // Poll for notifications
  const { data: notificationsData } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => notificationService.getUserNotifications(1, 50),
    enabled: !!user,
    refetchInterval: 30000 // Poll every 30 seconds
  })
  const unreadCount = notificationsData?.data?.filter(n => !n.isRead)?.length || 0;

  const handleLogout = async (e?: React.MouseEvent) => {
    if (e) {
      e.preventDefault()
      e.stopPropagation()
    }
    await logout()
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem('dismissedCompanyReminder')
      window.location.href = "/login"
    }
  }

  return (
    <header className="h-16 border-b border-border bg-background flex items-center justify-between px-4 shrink-0 gap-4">
      {/* Left side: Global Search */}
      <div className="flex-1 flex items-center min-w-0">
        <div className="relative w-full max-w-md hidden sm:flex items-center group">
          <Search size={16} className="absolute left-3 text-muted-foreground group-focus-within:text-primary transition-colors" />
          <Input 
            placeholder={t('searchPlaceholder')} 
            className="pl-9 pr-14 h-9 w-full bg-muted/40 border-transparent hover:bg-muted/60 focus:bg-background focus:border-primary/50 transition-all rounded-full shadow-none"
          />
          <div className="absolute right-2 flex items-center pointer-events-none">
            <kbd className="hidden lg:inline-flex items-center gap-0.5 rounded-full border border-border/50 bg-background/50 px-2 h-5 font-mono text-[10px] font-medium text-muted-foreground shadow-sm">
              <span className="text-[11px]">⌘</span>K
            </kbd>
          </div>
        </div>
      </div>

      {/* Right side: Tools & Profile */}
      <div className="flex items-center gap-1 shrink-0">
        {/* Wallet Info Dropdown */}
        {(user?.role?.name?.toLowerCase() === "candidate" || isRecruiter) && (
          <div className="hidden sm:flex items-center">
            <Popover>
              <PopoverTrigger className="flex items-center gap-1.5 h-10 px-2 rounded-full hover:bg-muted transition-colors outline-none cursor-pointer focus-visible:ring-2 focus-visible:ring-primary">
                <Coins size={18} className="text-[#FACC15]" />
                <span className="text-sm font-bold text-foreground">{walletLoading ? "..." : balance.toLocaleString()}</span>
                <ChevronDown size={14} className="text-muted-foreground" />
              </PopoverTrigger>
              <PopoverContent align="end" className="w-56 p-2" sideOffset={8}>
                <div className="flex flex-col gap-0.5">
                  <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1 whitespace-nowrap">
                    {t('billingPlans')}
                  </div>
                  <Link
                    href={`/${user?.role?.name?.toLowerCase() === 'candidate' ? 'candidate/pricing' : 'recruiter/billing'}`}
                    className="flex items-center gap-3 p-2 text-sm font-medium rounded-md hover:bg-muted transition-colors cursor-pointer whitespace-nowrap"
                  >
                    <CreditCard size={16} className="text-muted-foreground shrink-0" />
                    <span>{t('subscriptions')}</span>
                  </Link>
                  <Link
                    href={`/${user?.role?.name?.toLowerCase() === 'candidate' ? 'candidate' : 'recruiter'}/top-up`}
                    className="flex items-center gap-3 p-2 text-sm font-medium rounded-md hover:bg-muted transition-colors cursor-pointer whitespace-nowrap"
                  >
                    <PlusCircle size={16} className="text-muted-foreground shrink-0" />
                    <span>{t('topUpCoins')}</span>
                  </Link>
                  <Link
                    href={`/${user?.role?.name?.toLowerCase() === 'candidate' ? 'candidate' : 'recruiter'}/billing-history`}
                    className="flex items-center gap-3 p-2 text-sm font-medium rounded-md hover:bg-muted transition-colors cursor-pointer whitespace-nowrap"
                  >
                    <History size={16} className="text-muted-foreground shrink-0" />
                    <span>{t('transactionHistory')}</span>
                  </Link>
                </div>
              </PopoverContent>
            </Popover>
          </div>
        )}

        {/* Notification Bell */}
        <Popover open={isNotificationOpen} onOpenChange={setIsNotificationOpen}>
          <PopoverTrigger className="relative w-10 h-10 flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-muted rounded-full transition-colors outline-none cursor-pointer focus-visible:ring-2 focus-visible:ring-primary" title={t('notifications')}>
            <Bell size={18} />
            {unreadCount > 0 && (
              <span className="absolute top-2 right-2 bg-red-500 text-white text-[9px] font-bold w-4 h-4 rounded-full flex items-center justify-center">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </PopoverTrigger>
          <PopoverContent align="end" className="w-80 md:w-[450px] p-0" sideOffset={8}>
            <NotificationDialog open={isNotificationOpen} onOpenChange={setIsNotificationOpen} />
          </PopoverContent>
        </Popover>

        {/* User Profile & Dropdown */}
        {user && (
          <div className="flex items-center">
            <Popover>
              <PopoverTrigger className="flex items-center p-0.5 rounded-full hover:bg-muted active:scale-95 transition-all outline-none cursor-pointer focus-visible:ring-2 focus-visible:ring-primary">
                  {/* Avatar */}
                  <div className="relative flex-shrink-0">
                    <div className={`w-9 h-9 rounded-full flex items-center justify-center ${
                      activeSubName === 'Mastery' ? 'bg-gradient-to-tr from-[#1877F2] to-emerald-400 p-[2px] shadow-[0_0_10px_rgba(24,119,242,0.3)]'
                        : activeSubName === 'Pro Career' ? 'bg-[#1877F2] p-[1.5px]'
                          : (activeSubName === 'Hiring Pro' || activeSubName === 'Pro') ? 'bg-gradient-to-tr from-[#0c4a9e] via-[#1877F2] to-[#609df5] p-[2px] shadow-[0_0_10px_rgba(24,119,242,0.4)]'
                            : activeSubName === 'Growth' ? 'bg-[#1877F2] p-[1.5px]'
                              : activeSubName === 'Starter' ? 'bg-[#1877F2]/30 p-[1.5px]'
                                : 'border border-border/50 bg-white'
                      }`}>
                      <div className="w-full h-full rounded-full overflow-hidden relative bg-white dark:bg-slate-900">
                        <Image
                          src={`/images/avatar_${user.role?.name?.toLowerCase() || 'candidate'}.png`}
                          alt={user.fullName || user.email}
                          fill
                          sizes="36px"
                          className="object-cover"
                        />
                        {user.avatarUrl && user.avatarUrl !== 'null' && user.avatarUrl !== 'undefined' && !avatarError && (
                          <Image
                            src={user.avatarUrl}
                            alt={user.fullName || user.email}
                            fill
                            sizes="36px"
                            className={`object-cover transition-opacity duration-300 ${isAvatarLoaded ? 'opacity-100' : 'opacity-0'}`}
                            onLoad={() => setIsAvatarLoaded(true)}
                            onError={() => setAvatarError(true)}
                          />
                        )}
                      </div>
                    </div>
                  </div>
              </PopoverTrigger>
              <PopoverContent align="end" className="w-64 p-2" sideOffset={8}>
                {/* Header (Not clickable) */}
                <div className="flex flex-col items-center gap-1 p-2 pb-4 mb-2 border-b border-border">
                  <div className="relative w-16 h-16 rounded-full flex-shrink-0 mb-1 border border-border/50 bg-white">
                    <div className="w-full h-full rounded-full overflow-hidden relative bg-white dark:bg-slate-900">
                      <Image
                        src={`/images/avatar_${user.role?.name?.toLowerCase() || 'candidate'}.png`}
                        alt={user.fullName || user.email}
                        fill
                        sizes="64px"
                        className="object-cover"
                      />
                      {user.avatarUrl && user.avatarUrl !== 'null' && user.avatarUrl !== 'undefined' && !avatarError && (
                        <Image
                          src={user.avatarUrl}
                          alt={user.fullName || user.email}
                          fill
                          sizes="64px"
                          className={`object-cover transition-opacity duration-300 ${isAvatarLoaded ? 'opacity-100' : 'opacity-0'}`}
                        />
                      )}
                    </div>
                  </div>
                  <p className="text-base font-bold text-foreground text-center line-clamp-1" title={user.fullName || user.email}>
                    {user.fullName || user.email}
                  </p>
                  <p className="text-xs text-muted-foreground capitalize text-center">
                    {user.role?.name ? t(user.role.name.toLowerCase() as any) : t('candidate')}
                  </p>
                </div>

                {/* Group 1: Profile & Settings */}
                <div className="flex flex-col mb-2 border-b border-border pb-2 gap-0.5">
                  <Link
                    href={`/${user.role?.name?.toLowerCase() === 'candidate' ? 'candidate/profile' : user.role?.name?.toLowerCase() === 'recruiter' ? 'recruiter/company' : 'staff/dashboard'}`}
                    className="flex items-center gap-3 p-2 text-sm font-medium rounded-md hover:bg-muted transition-colors cursor-pointer whitespace-nowrap"
                  >
                    <UserIcon size={16} className="text-muted-foreground shrink-0" />
                    <span>{t('myProfile')}</span>
                  </Link>
                  <Link
                    href={`/${user.role?.name?.toLowerCase() || 'candidate'}/change-password`}
                    className="flex items-center gap-3 p-2 text-sm font-medium rounded-md hover:bg-muted transition-colors cursor-pointer whitespace-nowrap"
                  >
                    <Settings size={16} className="text-muted-foreground shrink-0" />
                    <span>{t('changePassword')}</span>
                  </Link>
                </div>

                {/* Group 2: Language */}
                <div className="flex flex-col mb-2 border-b border-border pb-2">
                  <div className="relative flex items-center justify-between p-2 text-sm font-medium rounded-md hover:bg-muted transition-colors w-full focus-within:bg-muted whitespace-nowrap">
                    <div className="flex items-center gap-3 pointer-events-none">
                      <Globe size={16} className="text-muted-foreground shrink-0" />
                      <span>{t('language')}</span>
                    </div>
                    <select
                      className="absolute inset-0 w-full h-full opacity-0 cursor-pointer text-sm"
                      value={locale}
                      onChange={(e) => {
                        document.cookie = `locale=${e.target.value}; path=/; max-age=31536000`
                        router.refresh()
                      }}
                      title="Language"
                    >
                      <option value="vi">Tiếng Việt</option>
                      <option value="en">English</option>
                    </select>
                    <div className="flex items-center gap-1 text-xs text-muted-foreground pointer-events-none">
                      <span>{locale === 'vi' ? 'Tiếng Việt' : 'English'}</span>
                      <ChevronDown size={12} />
                    </div>
                  </div>
                </div>

                {/* Group 3: Logout */}
                <button
                  type="button"
                  onClick={handleLogout}
                  className="flex items-center gap-3 p-2 text-sm font-medium text-red-500 rounded-md hover:bg-red-500/10 transition-colors cursor-pointer w-full"
                >
                  <LogOut size={16} />
                  <span>{t('logOut')}</span>
                </button>
              </PopoverContent>
            </Popover>
          </div>
        )}
      </div>
    </header>
  )
}
