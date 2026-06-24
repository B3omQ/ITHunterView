"use client"

import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import {
  LayoutDashboard, User, Briefcase, Bookmark, Bell, Settings, HelpCircle, LogOut,
  ChevronRight, Users, FileText, Building2, Shield, BarChart3, BrainCircuit,
  ClipboardList, Database, CreditCard, MessageSquare,
} from "lucide-react"
import { useAuthStore } from "@/store/auth.store"
import { Logo } from "@/components/layout/Logo"
import { APP_ROUTES } from "@/lib/constants"

// ---- Lucide icon map ----
const ICONS: Record<string, React.ReactNode> = {
  LayoutDashboard: <LayoutDashboard size={18} />,
  User: <User size={18} />,
  Briefcase: <Briefcase size={18} />,
  Bookmark: <Bookmark size={18} />,
  Bell: <Bell size={18} />,
  Settings: <Settings size={18} />,
  Users: <Users size={18} />,
  FileText: <FileText size={18} />,
  Building2: <Building2 size={18} />,
  Shield: <Shield size={18} />,
  BarChart3: <BarChart3 size={18} />,
  BrainCircuit: <BrainCircuit size={18} />,
  ClipboardList: <ClipboardList size={18} />,
  Database: <Database size={18} />,
  CreditCard: <CreditCard size={18} />,
  MessageSquare: <MessageSquare size={18} />,
}

// ---- Nav definitions per role ----
type NavItem = { label: string; href: string; icon: string; badge?: number }

const CANDIDATE_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.CANDIDATE.DASHBOARD, icon: "LayoutDashboard" },
  { label: "My Profile", href: APP_ROUTES.CANDIDATE.PROFILE, icon: "User" },
  { label: "Job Listings", href: APP_ROUTES.CANDIDATE.JOBS, icon: "Briefcase" },
  { label: "My Resume", href: APP_ROUTES.CANDIDATE.RESUME, icon: "FileText" },
  { label: "CV Optimizer", href: APP_ROUTES.CANDIDATE.CV_OPTIMIZER, icon: "BrainCircuit" },
  { label: "Applications", href: APP_ROUTES.CANDIDATE.APPLICATIONS, icon: "ClipboardList" },
  { label: "Notifications", href: APP_ROUTES.CANDIDATE.NOTIFICATIONS, icon: "Bell", badge: 3 },
  { label: "Settings", href: APP_ROUTES.CANDIDATE.SETTINGS, icon: "Settings" },
]

const RECRUITER_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.RECRUITER.DASHBOARD, icon: "LayoutDashboard" },
  { label: "Company", href: APP_ROUTES.RECRUITER.COMPANY, icon: "Building2" },
  { label: "Job Postings", href: APP_ROUTES.RECRUITER.JOBS, icon: "Briefcase" },
  { label: "Analytics", href: APP_ROUTES.RECRUITER.ANALYTICS, icon: "BarChart3" },
  { label: "Notifications", href: APP_ROUTES.RECRUITER.NOTIFICATIONS, icon: "Bell", badge: 2 },
  { label: "Settings", href: APP_ROUTES.RECRUITER.SETTINGS, icon: "Settings" },
]

const STAFF_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.STAFF.DASHBOARD, icon: "LayoutDashboard" },
  { label: "Companies", href: APP_ROUTES.STAFF.COMPANIES, icon: "Building2" },
  { label: "AI Config", href: APP_ROUTES.STAFF.AI_CONFIG, icon: "BrainCircuit" },
  { label: "Prompts", href: APP_ROUTES.STAFF.PROMPTS, icon: "MessageSquare" },
  { label: "Question Bank", href: APP_ROUTES.STAFF.QUESTION_BANK, icon: "FileText" },
  { label: "Audit Logs", href: APP_ROUTES.STAFF.AUDIT_LOGS, icon: "ClipboardList" },
  { label: "Notifications", href: APP_ROUTES.STAFF.NOTIFICATIONS, icon: "Bell" },
  { label: "Settings", href: APP_ROUTES.STAFF.SETTINGS, icon: "Settings" },
]

const ADMIN_NAV: NavItem[] = [
  { label: "Dashboard", href: APP_ROUTES.ADMIN.DASHBOARD, icon: "LayoutDashboard" },
  { label: "Accounts", href: APP_ROUTES.ADMIN.ACCOUNTS, icon: "Users" },
  { label: "Companies", href: APP_ROUTES.ADMIN.COMPANIES, icon: "Building2" },
  { label: "Master Data", href: APP_ROUTES.ADMIN.MASTER_DATA, icon: "Database" },
  { label: "Subscriptions", href: APP_ROUTES.ADMIN.SUBSCRIPTIONS, icon: "CreditCard" },
  { label: "Finance", href: APP_ROUTES.ADMIN.FINANCE, icon: "BarChart3" },
  { label: "Platform Safety", href: APP_ROUTES.ADMIN.AUDIT_LOGS, icon: "Shield" },
  { label: "Notifications", href: APP_ROUTES.ADMIN.NOTIFICATIONS, icon: "Bell" },
  { label: "Settings", href: APP_ROUTES.ADMIN.SETTINGS, icon: "Settings" },
]

function getNavItems(role: string): NavItem[] {
  switch (role.toLowerCase()) {
    case "admin":     return ADMIN_NAV
    case "staff":     return STAFF_NAV
    case "recruiter": return RECRUITER_NAV
    default:          return CANDIDATE_NAV
  }
}

export function Sidebar() {
  const pathname = usePathname()
  const router = useRouter()
  const { user, logout } = useAuthStore()

  const navItems = getNavItems(user?.role?.name ?? "candidate")

  const handleLogout = async () => {
    await logout()
    router.push("/login")
  }

  const isActive = (href: string) => {
    if (href === pathname) return true
    if (href.endsWith("/dashboard")) return pathname === href
    return pathname.startsWith(href)
  }

  return (
    <aside className="flex flex-col w-[240px] min-h-screen bg-sidebar border-r border-sidebar-border flex-shrink-0">
      {/* Logo */}
      <div className="px-5 py-5 border-b border-sidebar-border">
        <Logo size="sm" href="/" />
      </div>

      {/* User info */}
      {user && (
        <div className="px-4 py-4 border-b border-sidebar-border">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-full font-semibold text-sm flex-shrink-0 flex items-center justify-center bg-primary/10 text-primary">
              {user.fullName?.charAt(0)?.toUpperCase() || user.email.charAt(0).toUpperCase()}
            </div>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-sidebar-foreground truncate">
                {user.fullName || user.email}
              </p>
              <p className="text-xs text-muted-foreground capitalize">{user.role?.name || "Candidate"}</p>
            </div>
          </div>
        </div>
      )}

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 space-y-0.5 overflow-y-auto">
        {navItems.map((item) => {
          const active = isActive(item.href)
          return (
            <Link
              key={item.href}
              href={item.href}
              className={`sidebar-item flex items-center gap-3 h-10 px-3 rounded-xl text-sm font-medium transition-all group ${
                active
                  ? "bg-sidebar-accent text-sidebar-accent-foreground"
                  : "text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/50"
              }`}
            >
              <span className={active ? "text-primary" : "text-muted-foreground group-hover:text-sidebar-foreground transition-colors"}>
                {ICONS[item.icon]}
              </span>
              <span className="flex-1 truncate">{item.label}</span>
              {item.badge !== undefined && (
                <span className="ml-auto flex h-5 w-5 items-center justify-center rounded-full bg-indigo-600 text-[11px] font-semibold text-foreground">
                  {item.badge}
                </span>
              )}
              {active && !item.badge && (
                <ChevronRight size={14} className="ml-auto text-primary opacity-70" />
              )}
            </Link>
          )
        })}
      </nav>

      {/* Bottom actions */}
      <div className="px-3 py-4 border-t border-sidebar-border space-y-0.5">
        <Link
          href="/help"
          className="flex items-center gap-3 h-10 px-3 rounded-xl text-sm font-medium text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/50 transition-all"
        >
          <HelpCircle size={18} />
          Help &amp; Support
        </Link>
        <button
          id="sidebar-logout"
          onClick={handleLogout}
          className="w-full flex items-center gap-3 h-10 px-3 rounded-xl text-sm font-medium text-muted-foreground hover:text-red-400 hover:bg-red-500/10 transition-all"
        >
          <LogOut size={18} />
          Log Out
        </button>
      </div>
    </aside>
  )
}
