"use client"

import React from "react"
import Link from "next/link"
import Image from "next/image"
import { usePathname, useRouter } from "next/navigation"
import {
  LayoutDashboard, User, Briefcase, Bookmark, Bell, Settings, HelpCircle, LogOut,
  ChevronRight, Users, FileText, Building2, Shield, BarChart3, BrainCircuit,
  ClipboardList, Database, CreditCard, MessageSquare, KeyRound, AlertCircle, Sparkles, History, Map, Coins, FileSearch, PlusCircle
} from "lucide-react"
import { useAuthStore } from "@/store/auth.store"
import { Logo } from "@/components/layout/Logo"
import { APP_ROUTES } from "@/lib/constants"
import { useGetMyCompany } from "@/hooks/useCompany"
import { useWalletBalance } from "@/hooks/useWallet"
import { NotificationDialog } from "@/components/shared/NotificationDialog"
import { useQuery } from "@tanstack/react-query"
import { notificationService } from "@/services/notification.service"

// ---- Lucide icon map ----
const iconProps = { size: 18, strokeWidth: 2.5, className: "drop-shadow-sm" };
const ICONS: Record<string, React.ReactNode> = {
  LayoutDashboard: <LayoutDashboard {...iconProps} />,
  User: <User {...iconProps} />,
  Briefcase: <Briefcase {...iconProps} />,
  Bookmark: <Bookmark {...iconProps} />,
  Bell: <Bell {...iconProps} />,
  Settings: <Settings {...iconProps} />,
  Users: <Users {...iconProps} />,
  FileText: <FileText {...iconProps} />,
  Building2: <Building2 {...iconProps} />,
  Shield: <Shield {...iconProps} />,
  BarChart3: <BarChart3 {...iconProps} />,
  BrainCircuit: <BrainCircuit {...iconProps} />,
  ClipboardList: <ClipboardList {...iconProps} />,
  Database: <Database {...iconProps} />,
  CreditCard: <CreditCard {...iconProps} />,
  MessageSquare: <MessageSquare {...iconProps} />,
  KeyRound: <KeyRound {...iconProps} />,
  Sparkles: <Sparkles {...iconProps} />,
  History: <History {...iconProps} />,
  Map: <Map {...iconProps} />,
  FileSearch: <FileSearch {...iconProps} />,
  Coins: <Coins {...iconProps} />,
}

// ---- Nav definitions per role ----
type NavItem = { label: string; href: string; icon: string; badge?: number; children?: { label: string; href: string }[] }

const CANDIDATE_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.CANDIDATE.DASHBOARD, icon: "LayoutDashboard" },
  { label: "My Profile", href: APP_ROUTES.CANDIDATE.PROFILE, icon: "User" },
  { label: "Job Listings", href: APP_ROUTES.CANDIDATE.JOBS, icon: "Briefcase" },
  { label: "Saved Jobs", href: APP_ROUTES.CANDIDATE.SAVED_JOBS, icon: "Bookmark" },
  { label: "Applications", href: APP_ROUTES.CANDIDATE.APPLICATIONS, icon: "ClipboardList" },
  { label: "My Resume", href: APP_ROUTES.CANDIDATE.RESUME, icon: "FileText" },
  { label: "Mock Interview", href: APP_ROUTES.CANDIDATE.INTERVIEW, icon: "MessageSquare" },
  { label: "CV-JD Matching", href: APP_ROUTES.CANDIDATE.CV_MATCHING, icon: "FileSearch" },
  { label: "Learning Path", href: APP_ROUTES.CANDIDATE.LEARNING_PATH, icon: "Map" },
  { 
    label: "Billing & Plans", 
    href: "", 
    icon: "CreditCard",
    children: [
      { label: "Subscriptions", href: APP_ROUTES.CANDIDATE.PRICING },
      { label: "Top Up Coins", href: APP_ROUTES.CANDIDATE.TOP_UP },
      { label: "Transaction History", href: APP_ROUTES.CANDIDATE.BILLING_HISTORY }
    ]
  },
  { label: "Change Password", href: APP_ROUTES.CANDIDATE.CHANGE_PASSWORD, icon: "KeyRound" },
]

const RECRUITER_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.RECRUITER.DASHBOARD, icon: "LayoutDashboard" },
  { label: "Company", href: APP_ROUTES.RECRUITER.COMPANY, icon: "Building2" },
  { label: "Job Postings", href: APP_ROUTES.RECRUITER.JOBS, icon: "Briefcase" },
  { 
    label: "Billing & Plans", 
    href: "", 
    icon: "CreditCard",
    children: [
      { label: "Subscriptions", href: "/recruiter/billing" },
      { label: "Top Up Coins", href: APP_ROUTES.RECRUITER.TOP_UP },
      { label: "Transaction History", href: APP_ROUTES.RECRUITER.BILLING_HISTORY }
    ]
  },
  { label: "Change Password", href: APP_ROUTES.RECRUITER.CHANGE_PASSWORD, icon: "KeyRound" },
]

const STAFF_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.STAFF.DASHBOARD, icon: "LayoutDashboard" },
  { label: "Companies", href: APP_ROUTES.STAFF.COMPANIES, icon: "Building2" },
  { label: "Job Postings", href: APP_ROUTES.STAFF.JOB_POSTINGS, icon: "Briefcase" },
  { label: "System Notifications", href: APP_ROUTES.STAFF.NOTIFICATIONS, icon: "Bell" },
  { label: "AI Config", href: APP_ROUTES.STAFF.AI_CONFIG, icon: "BrainCircuit" },
  { label: "Prompts", href: APP_ROUTES.STAFF.PROMPTS, icon: "MessageSquare" },
  { label: "Question Bank", href: APP_ROUTES.STAFF.QUESTION_BANK, icon: "FileText" },
  { label: "Audit Logs", href: APP_ROUTES.STAFF.AUDIT_LOGS, icon: "ClipboardList" },
  { label: "Change Password", href: APP_ROUTES.STAFF.CHANGE_PASSWORD, icon: "KeyRound" },
]

const ADMIN_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.ADMIN.DASHBOARD, icon: "LayoutDashboard" },
  { label: "Accounts", href: APP_ROUTES.ADMIN.ACCOUNTS, icon: "Users" },
  { label: "Companies", href: APP_ROUTES.ADMIN.COMPANIES, icon: "Building2" },
  { label: "Job Postings", href: APP_ROUTES.ADMIN.JOB_POSTINGS, icon: "Briefcase" },
  { 
    label: "Master Data", 
    href: APP_ROUTES.ADMIN.MASTER_DATA, 
    icon: "Database",
    children: [
      { label: "Skills", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/skills` },
      { label: "SFIA Skills", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/sfia-skills` },
      { label: "Majors", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/majors` },
      { label: "Target Roles", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/target-roles` }
    ]
  },
  { label: "System Notifications", href: APP_ROUTES.ADMIN.NOTIFICATIONS, icon: "Bell" },
  { label: "AI Config", href: APP_ROUTES.ADMIN.AI_CONFIG, icon: "BrainCircuit" },
  { label: "Prompts", href: APP_ROUTES.ADMIN.PROMPTS, icon: "MessageSquare" },
  { label: "Question Bank", href: APP_ROUTES.ADMIN.QUESTION_BANK, icon: "FileText" },
  { label: "Subscriptions", href: APP_ROUTES.ADMIN.SUBSCRIPTIONS, icon: "CreditCard" },
  { label: "Finance", href: APP_ROUTES.ADMIN.FINANCE, icon: "BarChart3" },
  { label: "Notifications", href: APP_ROUTES.ADMIN.NOTIFICATIONS, icon: "Bell" },
  { label: "Platform Safety", href: APP_ROUTES.ADMIN.AUDIT_LOGS, icon: "Shield" },
  { label: "Change Password", href: APP_ROUTES.ADMIN.CHANGE_PASSWORD, icon: "KeyRound" },
]

function getNavItems(role: string): NavItem[] {
  switch (role.toLowerCase()) {
    case "admin": return ADMIN_NAV
    case "staff": return STAFF_NAV
    case "recruiter": return RECRUITER_NAV
    default: return CANDIDATE_NAV
  }
}

export function Sidebar() {
  const { user, logout } = useAuthStore()
  const router = useRouter()
  const pathname = usePathname()
  const [expandedGroups, setExpandedGroups] = React.useState<string[]>([])
  const [isNotificationOpen, setIsNotificationOpen] = React.useState(false)
  const [avatarError, setAvatarError] = React.useState(false)
  const [isAvatarLoaded, setIsAvatarLoaded] = React.useState(false)

  const isRecruiter = user?.role?.name?.toLowerCase() === "recruiter"
  const { data: company, isLoading: companyLoading } = useGetMyCompany({
    enabled: isRecruiter
  })

  // Get Wallet Balance & Subscription
  const isCandidateOrRecruiter = user?.role?.name?.toLowerCase() === "candidate" || isRecruiter
  const { data: walletData, isLoading: walletLoading } = useWalletBalance({
    enabled: isCandidateOrRecruiter
  })
  const balance = walletData?.data?.balance ?? 0
  const activeSubName = walletData?.data?.activeSubscriptionName
  const subEndDate = walletData?.data?.subscriptionEndDate ? new Date(walletData.data.subscriptionEndDate).toLocaleDateString('vi-VN') : null

  // Poll for notifications to update badge
  const { data: notificationsData } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => notificationService.getUserNotifications(1, 50),
    enabled: !!user,
    refetchInterval: 30000 // Poll every 30 seconds
  })
  const unreadCount = notificationsData?.data?.filter(n => !n.isRead)?.length || 0;

  const navItems = getNavItems(user?.role?.name ?? "candidate")

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

  const isActive = (href: string) => {
    if (href === pathname) return true
    if (href.endsWith("/dashboard")) return pathname === href
    const baseHref = href.split('?')[0]
    return pathname === baseHref || pathname.startsWith(`${baseHref}/`)
  }

  const toggleExpand = (label: string) => {
    setExpandedGroups(prev => 
      prev.includes(label) ? prev.filter(l => l !== label) : [...prev, label]
    )
  }

  // Auto-expand active group
  React.useEffect(() => {
    navItems.forEach(item => {
      const isParentActive = item.href ? isActive(item.href) : false;
      const isChildActive = item.children ? item.children.some(c => isActive(c.href)) : false;
      
      if (item.children && (isParentActive || isChildActive) && !expandedGroups.includes(item.label)) {
        setExpandedGroups(prev => [...prev, item.label])
      }
    })
  }, [pathname, navItems])

  return (
    <aside className="flex flex-col w-[240px] min-h-screen bg-sidebar border-r border-transparent hover:border-sidebar-border transition-colors duration-300 flex-shrink-0">
      {/* 1. Logo (Kept clean at the top) */}
      <div className="px-5 h-[68px] flex items-center">
        <Logo size="sm" href="/" />
      </div>

      {/* 2. Navigation (Moved up, immediately visible) */}
      <nav className="flex-1 px-3 py-4 space-y-0.5 overflow-y-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
        {navItems.map((item) => {
          const active = item.href ? isActive(item.href) : false
          const isExpanded = expandedGroups.includes(item.label)

          return (
            <div key={item.label} className="space-y-0.5">
              <div
                onClick={() => {
                  if (item.children) {
                    toggleExpand(item.label)
                  } else {
                    router.push(item.href)
                  }
                }}
                className={`sidebar-item cursor-pointer flex items-center gap-3 h-10 px-3 rounded-xl text-sm font-medium transition-all group ${
                  (active || (item.children && item.children.some(c => isActive(c.href)))) && !item.children
                    ? "bg-sidebar-accent text-sidebar-accent-foreground"
                    : "text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/50"
                }`}
              >
                <span className={(active || (item.children && item.children.some(c => isActive(c.href)))) ? "text-primary" : "text-muted-foreground group-hover:text-sidebar-foreground transition-colors"}>
                  {ICONS[item.icon]}
                </span>
                <span className="flex-1 truncate">{item.label}</span>
                {item.badge !== undefined && (
                  <span className="ml-auto flex h-5 w-5 items-center justify-center rounded-full bg-indigo-600 text-[11px] font-semibold text-foreground">
                    {item.badge}
                  </span>
                )}
                {item.label === "Company" && isRecruiter && !companyLoading && !company && (
                  <span className="ml-auto text-amber-500 animate-pulse" title="Company registration required">
                    <AlertCircle size={16} />
                  </span>
                )}
                {item.children && (
                  <ChevronRight size={14} className={`ml-auto transition-transform ${isExpanded ? 'rotate-90 text-primary' : 'opacity-70'}`} />
                )}
                {!item.children && active && !item.badge && !(item.label === "Company" && isRecruiter && !company) && (
                  <ChevronRight size={14} className="ml-auto text-primary opacity-70" />
                )}
              </div>
              
              {/* Children Submenu */}
              {item.children && isExpanded && (
                <div className="pl-9 pr-2 py-1 space-y-1">
                  {item.children.map(child => {
                    // Check strict match for children, support searchParams
                    const childActive = isActive(child.href)
                    return (
                      <Link
                        key={child.label}
                        href={child.href}
                        className={`flex items-center h-8 px-3 rounded-lg text-[13px] font-medium transition-all ${
                          childActive
                            ? "bg-primary/10 text-primary"
                            : "text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/30"
                        }`}
                      >
                        {child.label}
                      </Link>
                    )
                  })}
                </div>
              )}
            </div>
          )
        })}
      </nav>

      {/* 3. Bottom Actions & User Profile Footer */}
      <div className="p-3 flex flex-col gap-0.5 border-t border-border/40">
        {/* Global Actions (e.g., Notifications) */}
        <div
          onClick={() => setIsNotificationOpen(true)}
          className="sidebar-item cursor-pointer flex items-center gap-3 h-10 px-3 rounded-xl text-sm font-medium transition-all group text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/50"
        >
          <span className="text-muted-foreground group-hover:text-sidebar-foreground transition-colors relative">
            <Bell size={18} strokeWidth={2.5} className="drop-shadow-sm" />
            {unreadCount > 0 && (
              <span className="absolute -top-1.5 -right-1.5 bg-red-500 text-white text-[9px] font-bold w-4 h-4 rounded-full flex items-center justify-center">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </span>
          <span className="flex-1 truncate">Notifications</span>
        </div>

        {/* Wallet Info */}
        {(user?.role?.name?.toLowerCase() === "candidate" || isRecruiter) && (
          <div className="flex flex-col px-3">
            <div className="flex items-center justify-between h-9">
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                  <Coins size={18} strokeWidth={2.5} className="text-muted-foreground drop-shadow-sm"/> 
                  Coins
                </span>
                <span className="text-sm font-bold text-foreground">{walletLoading ? "..." : balance.toLocaleString()}</span>
              </div>
              
              <Link 
                href={`/${user?.role?.name?.toLowerCase() === 'candidate' ? 'candidate' : 'recruiter'}/top-up`} 
                className="text-[#1877F2] hover:bg-[#1877F2]/10 p-1 rounded-md transition-colors flex items-center justify-center -mr-1.5"
                title="Top Up"
              >
                <PlusCircle size={18} strokeWidth={2.5} />
              </Link>
            </div>
          </div>
        )}

        {/* User Card (Replaces top block & standalone logout) */}
        {user && (
          <div>
            <div className="flex items-center gap-3 p-2 rounded-xl hover:bg-sidebar-accent/50 transition-all group cursor-pointer">
              {/* Avatar */}
              <div className="relative flex-shrink-0">
                <div className={`w-9 h-9 rounded-full flex items-center justify-center ${
                  // Candidate Plans
                  activeSubName === 'Mastery' ? 'bg-gradient-to-tr from-[#1877F2] to-emerald-400 p-[2px] shadow-[0_0_10px_rgba(24,119,242,0.3)]' 
                  : activeSubName === 'Pro Career' ? 'bg-[#1877F2] p-[1.5px]'
                  // Recruiter Plans
                  : (activeSubName === 'Hiring Pro' || activeSubName === 'Pro') ? 'bg-gradient-to-tr from-[#0c4a9e] via-[#1877F2] to-[#609df5] p-[2px] shadow-[0_0_10px_rgba(24,119,242,0.4)]'
                  : activeSubName === 'Growth' ? 'bg-[#1877F2] p-[1.5px]'
                  : activeSubName === 'Starter' ? 'bg-[#1877F2]/30 p-[1.5px]'
                  : 'border border-border/50 bg-white'
                }`}>
                  <div className="w-full h-full rounded-full overflow-hidden relative bg-white dark:bg-slate-900">
                    {/* Fallback Image - Always rendered beneath */}
                    <Image 
                      src={`/images/avatar_${user.role?.name?.toLowerCase() || 'candidate'}.png`}
                      alt={user.fullName || user.email}
                      fill
                      sizes="36px"
                      className="object-cover" 
                    />
                    
                    {/* Real Avatar - Fades in on load */}
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

                {/* Candidate Badges */}
                {activeSubName === 'Mastery' && (
                  <div className="absolute -bottom-1.5 left-1/2 -translate-x-1/2 flex items-center justify-center leading-none bg-gradient-to-r from-[#1877F2] to-emerald-400 text-white text-[7px] font-bold px-1.5 py-[2px] rounded-[3px] border-[1.5px] border-white dark:border-slate-900 shadow-sm z-10 whitespace-nowrap">
                    MASTERY
                  </div>
                )}
                {activeSubName === 'Pro Career' && (
                  <div className="absolute -bottom-1.5 left-1/2 -translate-x-1/2 flex items-center justify-center leading-none bg-[#1877F2] text-white text-[7px] font-bold px-1.5 py-[2px] rounded-[3px] border-[1.5px] border-white dark:border-slate-900 shadow-sm z-10 whitespace-nowrap">
                    PRO
                  </div>
                )}
                
                {/* Recruiter Badges */}
                {(activeSubName === 'Hiring Pro' || activeSubName === 'Pro') && (
                  <div className="absolute -bottom-1.5 left-1/2 -translate-x-1/2 flex items-center justify-center leading-none bg-gradient-to-r from-[#0c4a9e] via-[#1877F2] to-[#609df5] text-white text-[7px] font-bold px-1.5 py-[2px] rounded-[3px] border-[1.5px] border-white dark:border-slate-900 shadow-sm z-10 whitespace-nowrap">
                    PRO
                  </div>
                )}
                {activeSubName === 'Growth' && (
                  <div className="absolute -bottom-1.5 left-1/2 -translate-x-1/2 flex items-center justify-center leading-none bg-[#1877F2] text-white text-[7px] font-bold px-1.5 py-[2px] rounded-[3px] border-[1.5px] border-white dark:border-slate-900 shadow-sm z-10 whitespace-nowrap">
                    GROWTH
                  </div>
                )}
                {activeSubName === 'Starter' && (
                  <div className="absolute -bottom-1.5 left-1/2 -translate-x-1/2 flex items-center justify-center leading-none bg-white text-[#1877F2] text-[7px] font-bold px-1.5 py-[2px] rounded-[3px] border-[1.5px] border-[#1877F2]/30 shadow-sm z-10 whitespace-nowrap">
                    STARTER
                  </div>
                )}
              </div>

              {/* Name & Role */}
              <div className="min-w-0 flex-1 py-0.5">
                <p 
                  className="text-sm font-semibold text-sidebar-foreground line-clamp-2 leading-tight break-words"
                  title={user.fullName || user.email}
                >
                  {user.fullName || user.email}
                </p>
                <p className="text-xs text-muted-foreground capitalize mt-0.5 truncate">
                  {user.role?.name || "Candidate"}
                </p>
              </div>

              {/* Contextual Logout Action */}
              <button
                type="button"
                onClick={(e) => handleLogout(e)}
                className="p-2 text-muted-foreground hover:text-red-500 hover:bg-red-500/10 rounded-lg transition-all flex-shrink-0"
                title="Log Out"
                aria-label="Log Out"
              >
                <LogOut size={16} />
              </button>
            </div>
          </div>
        )}
      </div>

      <NotificationDialog open={isNotificationOpen} onOpenChange={setIsNotificationOpen} />
    </aside>
  )
}
