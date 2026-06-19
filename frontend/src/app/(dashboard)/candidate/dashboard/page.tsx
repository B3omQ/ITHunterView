"use client"

import { useAuthStore } from "@/store/auth.store"
import { Briefcase, FileText, Bell, TrendingUp, Star, Clock } from "lucide-react"

export default function CandidateDashboard() {
  const { user } = useAuthStore()

  const stats = [
    { label: "Applications Sent", value: "12", icon: <FileText size={20} />, color: "from-indigo-500 to-violet-600", change: "+3 this week" },
    { label: "Interview Invites", value: "4", icon: <Bell size={20} />, color: "from-blue-500 to-indigo-600", change: "+1 new" },
    { label: "Profile Views", value: "89", icon: <TrendingUp size={20} />, color: "from-violet-500 to-purple-600", change: "+22 this month" },
    { label: "Saved Jobs", value: "7", icon: <Star size={20} />, color: "from-amber-500 to-orange-600", change: "Last added 2h ago" },
  ]

  return (
    <div className="space-y-7">
      {/* Greeting */}
      <div>
        <h1 className="text-2xl font-bold text-foreground">
          Good morning, {user?.fullName?.split(" ")[0] || "there"} 👋
        </h1>
        <p className="text-muted-foreground mt-1 text-sm">
          Here&apos;s what&apos;s happening with your job search today.
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map((s) => (
          <div key={s.label} className="bg-card border border-border rounded-2xl p-5 group hover:border-indigo-500/30 transition-all">
            <div className={`w-10 h-10 rounded-xl  mb-4 shadow-lg`}>
              {s.icon}
            </div>
            <div className="text-2xl font-bold text-foreground">{s.value}</div>
            <div className="text-xs text-muted-foreground mt-0.5">{s.label}</div>
            <div className="text-xs text-primary mt-2 font-medium">{s.change}</div>
          </div>
        ))}
      </div>

      {/* Recent Activity */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Recent Applications */}
        <div className="bg-card border border-border rounded-2xl p-5">
          <h2 className="font-semibold text-foreground mb-4">Recent Applications</h2>
          <div className="space-y-3">
            {[
              { company: "Google", role: "Frontend Engineer", status: "Interviewing", color: "text-blue-400 bg-blue-400/10" },
              { company: "Meta", role: "React Developer", status: "Applied", color: "text-amber-400 bg-amber-400/10" },
              { company: "Shopee", role: "Full Stack Dev", status: "Viewed", color: "text-green-400 bg-green-400/10" },
            ].map((app) => (
              <div key={app.company} className="flex items-center gap-3 p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors">
                <div className="w-9 h-9 rounded-lg  flex-shrink-0">
                  {app.company.charAt(0)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-foreground truncate">{app.role}</p>
                  <p className="text-xs text-muted-foreground">{app.company}</p>
                </div>
                <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${app.color}`}>
                  {app.status}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Recommended Jobs */}
        <div className="bg-card border border-border rounded-2xl p-5">
          <h2 className="font-semibold text-foreground mb-4">Recommended Jobs</h2>
          <div className="space-y-3">
            {[
              { company: "Tiki", role: "Senior React Dev", salary: "$2,500 – $4,000", match: "95%" },
              { company: "VNG", role: "Backend Engineer", salary: "$2,000 – $3,500", match: "88%" },
              { company: "MoMo", role: "Full Stack Dev", salary: "$1,800 – $3,000", match: "82%" },
            ].map((job) => (
              <div key={job.company} className="flex items-center gap-3 p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors cursor-pointer">
                <div className="w-9 h-9 rounded-lg  flex-shrink-0">
                  {job.company.charAt(0)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-foreground truncate">{job.role}</p>
                  <p className="text-xs text-muted-foreground">{job.company} · {job.salary}</p>
                </div>
                <span className="text-xs font-semibold text-primary bg-indigo-400/10 px-2 py-0.5 rounded-full">
                  {job.match}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}

