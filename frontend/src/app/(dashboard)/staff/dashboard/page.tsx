"use client"

import { BrainCircuit, FileText, CheckCircle, BarChart3, Clock } from "lucide-react"

export default function StaffDashboard() {
  const stats = [
    { label: "Interviews Today", value: "24", icon: <BrainCircuit size={20} />, color: "from-indigo-500 to-violet-600", change: "+6 from yesterday" },
    { label: "Questions Bank", value: "1,248", icon: <FileText size={20} />, color: "from-blue-500 to-indigo-600", change: "32 added this week" },
    { label: "Sessions Completed", value: "96", icon: <CheckCircle size={20} />, color: "from-green-500 to-emerald-600", change: "This month" },
    { label: "Avg. Score", value: "74%", icon: <BarChart3 size={20} />, color: "from-amber-500 to-orange-600", change: "+2% vs last month" },
  ]

  return (
    <div className="space-y-7">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Staff / Interview Manager</h1>
        <p className="text-muted-foreground mt-1 text-sm">Monitor AI interview sessions and manage the question bank.</p>
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
          <h2 className="font-semibold text-foreground mb-4">Recent Interview Sessions</h2>
          <div className="space-y-3">
            {[
              { candidate: "Nguyen Van A", job: "Frontend Dev", score: "88%", status: "Completed" },
              { candidate: "Tran Thi B", job: "Backend Eng.", score: "72%", status: "Completed" },
              { candidate: "Le Van C", job: "DevOps", score: "—", status: "In Progress" },
            ].map((s, i) => (
              <div key={i} className="flex items-center gap-3 p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors">
                <div className="w-8 h-8 rounded-full  text-xs font-bold flex-shrink-0">
                  {s.candidate.charAt(0)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-foreground truncate">{s.candidate}</p>
                  <p className="text-xs text-muted-foreground">{s.job}</p>
                </div>
                <div className="text-right">
                  <p className="text-sm font-semibold text-primary">{s.score}</p>
                  <p className={`text-xs ${s.status === "In Progress" ? "text-amber-400" : "text-green-400"}`}>{s.status}</p>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl p-5">
          <h2 className="font-semibold text-foreground mb-4">Question Categories</h2>
          <div className="space-y-3">
            {[
              { category: "Frontend", count: 342, pct: 27 },
              { category: "Backend", count: 415, pct: 33 },
              { category: "DevOps", count: 198, pct: 16 },
              { category: "System Design", count: 293, pct: 24 },
            ].map((c) => (
              <div key={c.category}>
                <div className="flex justify-between text-sm mb-1.5">
                  <span className="text-foreground font-medium">{c.category}</span>
                  <span className="text-muted-foreground">{c.count} questions</span>
                </div>
                <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                  <div className="h-full rounded-full bg-gradient-to-r from-indigo-500 to-violet-500" style={{ width: `${c.pct}%` }} />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
