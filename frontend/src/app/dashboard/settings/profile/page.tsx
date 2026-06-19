"use client"

import { useAuth } from "@/providers/AuthProvider"

export default function ProfileSettingsPage() {
  const { user } = useAuth()

  return (
    <div className="bg-card border border-border rounded-xl p-6 shadow-sm">
      <div className="mb-6">
        <h2 className="text-xl font-bold text-foreground">Profile Settings</h2>
        <p className="text-sm text-muted-foreground mt-1">Manage your public profile information.</p>
      </div>

      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <div className="w-20 h-20 rounded-full bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center text-white text-3xl font-semibold">
            {user?.fullName?.charAt(0)?.toUpperCase() || user?.email?.charAt(0).toUpperCase()}
          </div>
          <button className="btn-primary px-4 py-2 rounded-lg text-sm font-medium">
            Upload Avatar
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">Full Name</label>
            <input
              type="text"
              defaultValue={user?.fullName || ""}
              className="w-full h-10 px-3 rounded-lg border border-border bg-background text-sm text-foreground focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            />
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">Email Address</label>
            <input
              type="email"
              defaultValue={user?.email || ""}
              disabled
              className="w-full h-10 px-3 rounded-lg border border-border bg-muted text-sm text-muted-foreground cursor-not-allowed"
            />
          </div>
        </div>
      </div>
    </div>
  )
}
