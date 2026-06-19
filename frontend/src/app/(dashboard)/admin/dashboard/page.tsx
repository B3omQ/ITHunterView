"use client"

import { Users, Building2, Briefcase, ShieldCheck, TrendingUp, AlertTriangle } from "lucide-react"

export default function AdminDashboard() {
  const stats = [
    { label: "Total Users", value: "2,481", icon: <Users size={20} />, color: "from-indigo-500 to-violet-600", change: "+124 this month" },
    { label: "Companies", value: "187", icon: <Building2 size={20} />, color: "from-blue-500 to-cyan-600", change: "12 pending review" },
    { label: "Job Postings", value: "634", icon: <Briefcase size={20} />, color: "from-green-500 to-emerald-600", change: "48 published today" },
    { label: "System Health", value: "99.9%", icon: <ShieldCheck size={20} />, color: "from-amber-500 to-orange-600", change: "All systems normal" },
  ]

  return (
    <div className="space-y-7">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Admin Dashboard</h1>
        <p className="text-muted-foreground mt-1 text-sm">Platform overview and system management.</p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map((s) => (
          <div key={s.label} className="bg-card border border-border rounded-2xl p-5 hover:border-indigo-500/30 transition-all">
            <div className={`w-10 h-10 rounded-xl  mb-4`}>
              {s.icon}
            </div>
            <div className="text-2xl font-bold text-foreground">{s.value}</div>
            <div className="text-xs text-muted-foreground mt-0.5">{s.label}</div>
            <div className="text-xs text-primary mt-2">{s.change}</div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="bg-card border border-border rounded-2xl p-5">
          <h2 className="font-semibold text-foreground mb-4">Pending Reviews</h2>
          <div className="space-y-3">
            {[
              { type: "Company", name: "Axon Technology", priority: "High" },
              { type: "Job Post", name: "Senior Dev at TechCo", priority: "Medium" },
              { type: "Company", name: "Bright Solutions", priority: "Low" },
            ].map((item, i) => (
              <div key={i} className="flex items-center gap-3 p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors cursor-pointer">
                <div className="flex-1">
                  <p className="text-sm font-medium text-foreground">{item.name}</p>
                  <p className="text-xs text-muted-foreground">{item.type}</p>
                </div>
                <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                  item.priority === "High" ? "text-red-400 bg-red-400/10"
                  : item.priority === "Medium" ? "text-amber-400 bg-amber-400/10"
                  : "text-green-400 bg-green-400/10"
                }`}>{item.priority}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl p-5">
          <h2 className="font-semibold text-foreground mb-4">Recent Activity</h2>
          <div className="space-y-3">
            {[
              { action: "New company registered", time: "2 min ago" },
              { action: "Job post approved: VNG Corp", time: "15 min ago" },
              { action: "User banned: spam report", time: "1 hr ago" },
              { action: "System config updated", time: "3 hr ago" },
            ].map((log, i) => (
              <div key={i} className="flex items-start gap-3 py-2 border-b border-border/50 last:border-0">
                <div className="w-1.5 h-1.5 rounded-full bg-indigo-500 mt-1.5 flex-shrink-0" />
                <div className="flex-1">
                  <p className="text-sm text-foreground">{log.action}</p>
                  <p className="text-xs text-muted-foreground mt-0.5">{log.time}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
