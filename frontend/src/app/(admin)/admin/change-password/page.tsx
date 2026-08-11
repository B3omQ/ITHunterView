"use client"

import { useState } from "react"
import Link from "next/link"
import { Eye, EyeOff, Loader2, AlertCircle, CheckCircle2 } from "lucide-react"
import { authService } from "@/services/auth.service"
import { useAuthStore } from "@/store/auth.store"
import { useTranslations } from "next-intl"

export default function SecuritySettingsPage() {
  const t = useTranslations("AdminChangePassword")
  const { logout } = useAuthStore()

  const [currentPassword, setCurrentPassword] = useState("")
  const [newPassword, setNewPassword] = useState("")
  const [confirmNewPassword, setConfirmNewPassword] = useState("")

  const [showCurrent, setShowCurrent] = useState(false)
  const [showNew, setShowNew] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState("")
  const [success, setSuccess] = useState("")

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")
    setSuccess("")

    if (newPassword !== confirmNewPassword) {
      setError(t("errMatch"))
      return
    }

    if (newPassword.length < 6) {
      setError("New password must be at least 6 characters long.")
      return
    }

    setLoading(true)
    try {
      const res = await authService.changePassword({ currentPassword, newPassword, confirmNewPassword })
      if (!res.success) {
        setError(res.message ?? t("errFailed"))
        return
      }
      setSuccess(t("success"))
      setTimeout(async () => {
        await logout()
      }, 3000)
    } catch (err: any) {
      const serverMsg = err?.response?.data?.message 
        || err?.response?.data?.errors?.NewPassword?.[0]
        || err?.response?.data?.title
        || t("errServer")
      setError(serverMsg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="bg-card border border-border rounded-xl p-6 shadow-sm">
        <div className="mb-6">
          <h2 className="text-xl font-bold text-foreground">{t("pageTitle")}</h2>
          <p className="text-sm text-muted-foreground mt-1">{t("pageDesc")}</p>
        </div>

        {error && (
          <div className="mb-5 flex items-start gap-2.5 rounded-lg bg-red-500/10 border border-red-500/20 px-3.5 py-3 text-sm text-red-600">
            <AlertCircle size={16} className="mt-0.5 flex-shrink-0" />
            {error}
          </div>
        )}

        {success && (
          <div className="mb-5 flex items-start gap-2.5 rounded-lg bg-green-500/10 border border-green-500/20 px-3.5 py-3 text-sm text-green-700">
            <CheckCircle2 size={16} className="mt-0.5 flex-shrink-0" />
            {success}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6 max-w-lg">
          {/* Current Password */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">{t("lblCurrent")}</label>
            <div className="relative">
              <input
                type={showCurrent ? "text" : "password"}
                required
                placeholder={t("phCurrent")}
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                className="w-full h-11 px-3 pr-10 rounded-lg border border-border bg-background text-sm text-foreground focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
              />
              <button
                type="button"
                onClick={() => setShowCurrent(!showCurrent)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                tabIndex={-1}
              >
                {showCurrent ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
            <Link href="/forgot-password" className="text-xs text-primary hover:text-primary/80 inline-block mt-1">
              {t("forgot")}
            </Link>
          </div>

          <hr className="border-border" />

          {/* New Password */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">{t("lblNew")}</label>
            <div className="relative">
              <input
                type={showNew ? "text" : "password"}
                required
                placeholder={t("phNew")}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="w-full h-11 px-3 pr-10 rounded-lg border border-border bg-background text-sm text-foreground focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
              />
              <button
                type="button"
                onClick={() => setShowNew(!showNew)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                tabIndex={-1}
              >
                {showNew ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {/* Confirm New Password */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">{t("lblConfirm")}</label>
            <div className="relative">
              <input
                type={showConfirm ? "text" : "password"}
                required
                placeholder={t("phConfirm")}
                value={confirmNewPassword}
                onChange={(e) => setConfirmNewPassword(e.target.value)}
                className="w-full h-11 px-3 pr-10 rounded-lg border border-border bg-background text-sm text-foreground focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
              />
              <button
                type="button"
                onClick={() => setShowConfirm(!showConfirm)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                tabIndex={-1}
              >
                {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center gap-3 pt-2">
            <button
              type="submit"
              disabled={loading}
              className="btn-primary h-10 px-5 rounded-lg text-sm font-semibold flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {loading ? <><Loader2 size={16} className="animate-spin" /> {t("btnSaving")}</> : t("btnSave")}
            </button>
            <button
              type="button"
              className="h-10 px-5 rounded-lg text-sm font-semibold text-primary border border-primary hover:bg-primary/5 transition-colors"
            >
              {t("btnCancel")}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
