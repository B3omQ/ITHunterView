"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { User, Lock } from "lucide-react"

export default function SettingsLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()

  // Detect role từ pathname để build đúng hrefs
  const role = pathname.split("/")[1] // e.g. "candidate", "recruiter", etc.
  const base = `/${role}/settings`

  const tabs = [
    { label: "Profile", href: `${base}/profile`, icon: <User size={18} /> },
    { label: "Password & Security", href: `${base}/security`, icon: <Lock size={18} /> },
  ]

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Settings</h1>
        <p className="text-sm text-muted-foreground mt-1">Manage your account settings and preferences.</p>
      </div>

      <div className="flex flex-col md:flex-row gap-6 items-start">
        {/* Sub-navigation Sidebar */}
        <div className="w-full md:w-64 flex-shrink-0 bg-card border border-border rounded-xl p-2 shadow-sm">
          <nav className="flex flex-col gap-1">
            {tabs.map((tab) => {
              const active = pathname === tab.href || pathname.startsWith(tab.href)
              return (
                <Link
                  key={tab.href}
                  href={tab.href}
                  className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                    active
                      ? "bg-primary/10 text-primary"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground"
                  }`}
                >
                  {tab.icon}
                  {tab.label}
                </Link>
              )
            })}
          </nav>
        </div>

        {/* Content Area */}
        <div className="flex-1 min-w-0">{children}</div>
      </div>
    </div>
  )
}
