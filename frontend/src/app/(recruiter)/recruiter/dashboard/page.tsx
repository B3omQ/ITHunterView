"use client"

import { useAuthStore } from "@/store/auth.store"
import { Briefcase, Users, CheckCircle, Clock } from "lucide-react"

export default function RecruiterDashboard() {
  const { user } = useAuthStore()

  const stats = [
    { label: "Active Jobs", value: "8", icon: <Briefcase size={20} />, color: "from-indigo-500 to-violet-600", change: "2 pending review" },
    { label: "Total Applicants", value: "143", icon: <Users size={20} />, color: "from-blue-500 to-indigo-600", change: "+18 this week" },
    { label: "Shortlisted", value: "23", icon: <CheckCircle size={20} />, color: "from-green-500 to-emerald-600", change: "5 for interview" },
    { label: "Time to Hire", value: "12d", icon: <Clock size={20} />, color: "from-amber-500 to-orange-600", change: "Avg. 12 days" },
  ]

  return (
    <div className="space-y-7">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Recruiter Dashboard</h1>
        <p className="text-muted-foreground mt-1 text-sm">
          Manage your job postings and candidate pipeline.
        </p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map((s) => (
          <div key={s.label} className="bg-card border border-border rounded-2xl p-5 hover:border-indigo-500/30 transition-all">
            <div className={`w-10 h-10 rounded-xl mb-4`}>
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
          <h2 className="font-semibold text-foreground mb-4">Active Job Postings</h2>
          <div className="space-y-3">
            {[
              { title: "Senior Frontend Dev", applicants: 34, status: "Published" },
              { title: "Backend Engineer", applicants: 21, status: "Published" },
              { title: "DevOps Engineer", applicants: 12, status: "Pending" },
            ].map((job) => (
              <div key={job.title} className="flex items-center gap-3 p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors cursor-pointer">
                <div className="flex-1">
                  <p className="text-sm font-medium text-foreground">{job.title}</p>
                  <p className="text-xs text-muted-foreground">{job.applicants} applicants</p>
                </div>
                <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                  job.status === "Published" ? "text-green-400 bg-green-400/10" : "text-amber-400 bg-amber-400/10"
                }`}>{job.status}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl p-5">
          <h2 className="font-semibold text-foreground mb-4">Recent Applicants</h2>
          <div className="space-y-3">
            {[
              { name: "Nguyen Van A", role: "Senior Frontend", score: "92%" },
              { name: "Tran Thi B", role: "Backend Eng.", score: "85%" },
              { name: "Le Van C", role: "Backend Eng.", score: "78%" },
            ].map((c) => (
              <div key={c.name} className="flex items-center gap-3 p-3 rounded-xl bg-muted/50 hover:bg-muted transition-colors cursor-pointer">
                <div className="w-9 h-9 rounded-full flex items-center justify-center bg-muted text-xs font-bold flex-shrink-0">
                  {c.name.charAt(0)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-foreground">{c.name}</p>
                  <p className="text-xs text-muted-foreground">{c.role}</p>
                </div>
                <span className="text-xs font-semibold text-primary bg-indigo-400/10 px-2 py-0.5 rounded-full">{c.score}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
