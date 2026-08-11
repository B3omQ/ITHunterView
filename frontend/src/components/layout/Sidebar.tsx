"use client"

import React from "react"
import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import {
  LayoutDashboard, User, Briefcase, Bookmark, Bell, Settings, HelpCircle, LogOut,
  ChevronRight, Users, FileText, Building2, Shield, BarChart3, BrainCircuit,
  ClipboardList, Database, CreditCard, MessageSquare, KeyRound, AlertCircle, Sparkles, History, Map, Coins, FileSearch, PlusCircle, Menu
} from "lucide-react"
import { useAuthStore } from "@/store/auth.store"
import { APP_ROUTES } from "@/lib/constants"
import { useGetMyCompany } from "@/hooks/useCompany"
import { useTranslations } from "next-intl"
import { Logo } from "@/components/layout/Logo"

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
export interface NavItem { labelKey: string; href: string; icon: string; badge?: number; children?: { labelKey: string; href: string }[] }

const CANDIDATE_NAV: NavItem[] = [
  { labelKey: "dashboard", href: APP_ROUTES.CANDIDATE.DASHBOARD, icon: "LayoutDashboard" },
  { labelKey: "myProfile", href: APP_ROUTES.CANDIDATE.PROFILE, icon: "User" },
  { labelKey: "jobListings", href: APP_ROUTES.CANDIDATE.JOBS, icon: "Briefcase" },
  { labelKey: "savedJobs", href: APP_ROUTES.CANDIDATE.SAVED_JOBS, icon: "Bookmark" },
  { labelKey: "applications", href: APP_ROUTES.CANDIDATE.APPLICATIONS, icon: "ClipboardList" },
  { labelKey: "myResume", href: APP_ROUTES.CANDIDATE.RESUME, icon: "FileText" },
  { labelKey: "mockInterview", href: APP_ROUTES.CANDIDATE.INTERVIEW, icon: "MessageSquare" },
  { labelKey: "cvJdMatching", href: APP_ROUTES.CANDIDATE.CV_MATCHING, icon: "FileSearch" },
  { labelKey: "optimizeCv", href: APP_ROUTES.CANDIDATE.OPTIMIZE_CV, icon: "Sparkles" },
  { labelKey: "learningPath", href: APP_ROUTES.CANDIDATE.LEARNING_PATH, icon: "Map" }
]

const RECRUITER_NAV: NavItem[] = [
  { labelKey: "dashboard", href: APP_ROUTES.RECRUITER.DASHBOARD, icon: "LayoutDashboard" },
  { labelKey: "company", href: APP_ROUTES.RECRUITER.COMPANY, icon: "Building2" },
  { labelKey: "jobPostings", href: APP_ROUTES.RECRUITER.JOBS, icon: "Briefcase" }
]

const STAFF_NAV: NavItem[] = [
  { labelKey: "dashboard", href: APP_ROUTES.STAFF.DASHBOARD, icon: "LayoutDashboard" },
  { labelKey: "companies", href: APP_ROUTES.STAFF.COMPANIES, icon: "Building2" },
  { labelKey: "jobPostings", href: APP_ROUTES.STAFF.JOB_POSTINGS, icon: "Briefcase" },
  { labelKey: "systemNotifications", href: APP_ROUTES.STAFF.NOTIFICATIONS, icon: "Bell" },
  { labelKey: "aiConfig", href: APP_ROUTES.STAFF.AI_CONFIG, icon: "BrainCircuit" },
  { labelKey: "prompts", href: APP_ROUTES.STAFF.PROMPTS, icon: "MessageSquare" },
  { labelKey: "questionBank", href: APP_ROUTES.STAFF.QUESTION_BANK, icon: "FileText" },
  { labelKey: "auditLogs", href: APP_ROUTES.STAFF.AUDIT_LOGS, icon: "ClipboardList" }
]

const ADMIN_NAV: NavItem[] = [
  { labelKey: "dashboard", href: APP_ROUTES.ADMIN.DASHBOARD, icon: "LayoutDashboard" },
  { labelKey: "accounts", href: APP_ROUTES.ADMIN.ACCOUNTS, icon: "Users" },
  { labelKey: "companies", href: APP_ROUTES.ADMIN.COMPANIES, icon: "Building2" },
  { labelKey: "jobPostings", href: APP_ROUTES.ADMIN.JOB_POSTINGS, icon: "Briefcase" },
  {
    labelKey: "masterData",
    href: APP_ROUTES.ADMIN.MASTER_DATA,
    icon: "Database",
    children: [
      { labelKey: "skills", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/skills` },
      { labelKey: "sfiaSkills", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/sfia-skills` },
      { labelKey: "majors", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/majors` },
      { labelKey: "targetRoles", href: `${APP_ROUTES.ADMIN.MASTER_DATA}/target-roles` }
    ]
  },
  { labelKey: "systemNotifications", href: APP_ROUTES.ADMIN.NOTIFICATIONS, icon: "Bell" },
  { labelKey: "aiConfig", href: APP_ROUTES.ADMIN.AI_CONFIG, icon: "BrainCircuit" },
  { labelKey: "prompts", href: APP_ROUTES.ADMIN.PROMPTS, icon: "MessageSquare" },
  { labelKey: "questionBank", href: APP_ROUTES.ADMIN.QUESTION_BANK, icon: "FileText" },
  { labelKey: "subscriptions", href: APP_ROUTES.ADMIN.SUBSCRIPTIONS, icon: "CreditCard" },
  { labelKey: "finance", href: APP_ROUTES.ADMIN.FINANCE, icon: "BarChart3" },
  { labelKey: "notifications", href: APP_ROUTES.ADMIN.NOTIFICATIONS, icon: "Bell" },
  { labelKey: "platformSafety", href: APP_ROUTES.ADMIN.AUDIT_LOGS, icon: "Shield" }
]

export function getNavItems(role: string): NavItem[] {
  switch (role.toLowerCase()) {
    case "admin": return ADMIN_NAV
    case "staff": return STAFF_NAV
    case "recruiter": return RECRUITER_NAV
    default: return CANDIDATE_NAV
  }
}

interface SidebarProps {
  isOpen: boolean;
  onToggle: () => void;
}

export function Sidebar({ isOpen, onToggle }: SidebarProps) {
  const { user } = useAuthStore()
  const router = useRouter()
  const pathname = usePathname()
  const t = useTranslations("Layout.Sidebar")
  const [expandedGroups, setExpandedGroups] = React.useState<string[]>([])

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

      if (item.children && (isParentActive || isChildActive) && !expandedGroups.includes(item.labelKey)) {
        setExpandedGroups(prev => [...prev, item.labelKey])
      }
    })
  }, [pathname, navItems])

  return (
    <aside className={`flex flex-col min-h-screen bg-sidebar border-r border-transparent hover:border-sidebar-border transition-all duration-300 flex-shrink-0 overflow-hidden ${isOpen ? 'w-[240px]' : 'w-[68px]'}`}>
      <div className={`h-16 flex items-center flex-shrink-0 ${isOpen ? 'px-4 justify-between' : 'justify-center'}`}>
        {isOpen && <Logo size="sm" href="/" />}
        <button
          onClick={onToggle}
          className="p-2 text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground rounded-lg transition-colors"
          title="Toggle Sidebar"
        >
          <Menu size={20} />
        </button>
      </div>
      <nav className={`flex-1 py-4 space-y-0.5 overflow-y-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none] ${isOpen ? 'px-3' : 'px-2'}`}>
        {navItems.map((item) => {
          const active = item.href ? isActive(item.href) : false
          const isExpanded = expandedGroups.includes(item.labelKey)
          const translatedLabel = t(item.labelKey as any)

          return (
            <div key={item.labelKey} className="space-y-0.5">
              <div
                onClick={() => {
                  if (item.children) {
                    toggleExpand(item.labelKey)
                  } else {
                    router.push(item.href)
                  }
                }}
                title={!isOpen ? translatedLabel : undefined}
                className={`sidebar-item cursor-pointer flex items-center h-10 rounded-xl text-sm font-medium transition-all group ${
                  isOpen ? 'gap-3 px-3' : 'justify-center px-0'
                } ${(active || (item.children && item.children.some(c => isActive(c.href)))) && !item.children
                    ? "bg-sidebar-accent text-sidebar-accent-foreground"
                    : "text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/50"
                  }`}
              >
                <span className={(active || (item.children && item.children.some(c => isActive(c.href)))) ? "text-primary" : "text-muted-foreground group-hover:text-sidebar-foreground transition-colors"}>
                  {ICONS[item.icon]}
                </span>
                {isOpen && (
                  <>
                    <span className="flex-1 truncate">{translatedLabel}</span>
                    {item.badge !== undefined && (
                      <span className="ml-auto flex h-5 w-5 items-center justify-center rounded-full bg-indigo-600 text-[11px] font-semibold text-foreground">
                        {item.badge}
                      </span>
                    )}
                    {item.labelKey === "company" && isRecruiter && !companyLoading && !company && (
                      <span className="ml-auto text-amber-500 animate-pulse" title="Company registration required">
                        <AlertCircle size={16} />
                      </span>
                    )}
                    {item.children && (
                      <ChevronRight size={14} className={`ml-auto transition-transform ${isExpanded ? 'rotate-90 text-primary' : 'opacity-70'}`} />
                    )}
                    {!item.children && active && !item.badge && !(item.labelKey === "company" && isRecruiter && !company) && (
                      <ChevronRight size={14} className="ml-auto text-primary opacity-70" />
                    )}
                  </>
                )}
              </div>

              {/* Children Submenu */}
              {isOpen && item.children && isExpanded && (
                <div className="pl-9 pr-2 py-1 space-y-1">
                  {item.children.map(child => {
                    const childActive = isActive(child.href)
                    return (
                      <Link
                        key={child.labelKey}
                        href={child.href}
                        className={`flex items-center h-8 px-3 rounded-lg text-[13px] font-medium transition-all ${childActive
                            ? "bg-primary/10 text-primary"
                            : "text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/30"
                          }`}
                      >
                        {t(child.labelKey as any)}
                      </Link>
                    )
                  })}
                </div>
              )}
            </div>
          )
        })}
      </nav>
    </aside>
  )
}
