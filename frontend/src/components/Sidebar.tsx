"use client"

import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import {
  LayoutDashboard,
  User,
  Briefcase,
  Bookmark,
  Bell,
  Settings,
  HelpCircle,
  LogOut,
  ChevronRight,
  Users,
  FileText,
  Building2,
  Shield,
  BarChart3,
  BrainCircuit,
} from "lucide-react"
import { useAuth } from "@/providers/AuthProvider"
import { authApi } from "@/api/auth"
import { Logo } from "@/components/Logo"

interface NavItem {
  label: string
  href: string
  icon: React.ReactNode
  badge?: number
  children?: NavItem[]
}

function getCandidateNav(base: string): NavItem[] {
  return [
    { label: "Dashboard", href: `${base}`, icon: <LayoutDashboard size={18} /> },
    { label: "My Profile", href: `${base}/profile`, icon: <User size={18} /> },
    { label: "Job Listings", href: `${base}/jobs`, icon: <Briefcase size={18} /> },
    { label: "Saved Jobs", href: `${base}/saved-jobs`, icon: <Bookmark size={18} /> },
    { label: "Notifications", href: `${base}/notifications`, icon: <Bell size={18} />, badge: 3 },
    { label: "Settings", href: `/dashboard/settings`, icon: <Settings size={18} /> },
  ]
}

function getRecruiterNav(base: string): NavItem[] {
  return [
    { label: "Dashboard", href: `${base}`, icon: <LayoutDashboard size={18} /> },
    { label: "My Profile", href: `${base}/profile`, icon: <User size={18} /> },
    { label: "Job Postings", href: `${base}/jobs`, icon: <Briefcase size={18} /> },
    { label: "Applications", href: `${base}/applications`, icon: <FileText size={18} /> },
    { label: "Candidates", href: `${base}/candidates`, icon: <Users size={18} /> },
    { label: "Notifications", href: `${base}/notifications`, icon: <Bell size={18} />, badge: 2 },
    { label: "Settings", href: `/dashboard/settings`, icon: <Settings size={18} /> },
  ]
}

function getAdminNav(base: string): NavItem[] {
  return [
    { label: "Dashboard", href: `${base}`, icon: <LayoutDashboard size={18} /> },
    { label: "Users", href: `${base}/users`, icon: <Users size={18} /> },
    { label: "Companies", href: `${base}/companies`, icon: <Building2 size={18} /> },
    { label: "Job Reviews", href: `${base}/job-reviews`, icon: <Briefcase size={18} /> },
    { label: "Analytics", href: `${base}/analytics`, icon: <BarChart3 size={18} /> },
    { label: "System Config", href: `${base}/config`, icon: <Shield size={18} /> },
    { label: "Settings", href: `/dashboard/settings`, icon: <Settings size={18} /> },
  ]
}

function getStaffNav(base: string): NavItem[] {
  return [
    { label: "Dashboard", href: `${base}`, icon: <LayoutDashboard size={18} /> },
    { label: "AI Interviews", href: `${base}/interviews`, icon: <BrainCircuit size={18} /> },
    { label: "Question Bank", href: `${base}/questions`, icon: <FileText size={18} /> },
    { label: "Reports", href: `${base}/reports`, icon: <BarChart3 size={18} /> },
    { label: "Notifications", href: `${base}/notifications`, icon: <Bell size={18} /> },
    { label: "Settings", href: `/dashboard/settings`, icon: <Settings size={18} /> },
  ]
}

function getNavItems(role: string): NavItem[] {
  switch (role.toLowerCase()) {
    case "admin":     return getAdminNav("/dashboard/admin")
    case "staff":     return getStaffNav("/dashboard/staff")
    case "recruiter": return getRecruiterNav("/dashboard/recruiter")
    default:          return getCandidateNav("/dashboard/candidate")
  }
}

export function Sidebar() {
  const pathname = usePathname()
  const router = useRouter()
  const { user, logout } = useAuth()

  const navItems = getNavItems(user?.role ?? "candidate")

  const handleLogout = async () => {
    const refreshToken = localStorage.getItem("refreshToken")
    if (refreshToken) {
      await authApi.logout(refreshToken).catch(() => {})
    }
    logout()
  }

  const isActive = (href: string) => {
    if (href === pathname) return true
    // Exact match for dashboard root, prefix for sub-pages
    if (href.endsWith("/dashboard/candidate") || href.endsWith("/dashboard/admin") ||
        href.endsWith("/dashboard/recruiter") || href.endsWith("/dashboard/staff")) {
      return pathname === href
    }
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
            <div className="w-9 h-9 rounded-full  font-semibold text-sm flex-shrink-0">
              {user.fullName?.charAt(0)?.toUpperCase() || user.email.charAt(0).toUpperCase()}
            </div>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-sidebar-foreground truncate">
                {user.fullName || user.email}
              </p>
              <p className="text-xs text-muted-foreground capitalize">{user.role}</p>
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
                {item.icon}
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
          Help & Support
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
