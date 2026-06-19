"use client"

import { useState, useEffect } from "react"
import { useSearchParams, useRouter } from "next/navigation"
import Link from "next/link"
import { ArrowLeft, Eye, EyeOff, Loader2, AlertCircle, KeyRound, CheckCircle2 } from "lucide-react"
import { authApi } from "@/api/auth"
import { Logo } from "@/components/Logo"

export default function ResetPasswordPage() {
  const searchParams = useSearchParams()
  const router = useRouter()

  const token = searchParams.get("token") ?? ""
  const email = searchParams.get("email") ?? ""

  const [newPassword, setNewPassword] = useState("")
  const [confirmNewPassword, setConfirmNewPassword] = useState("")
  const [showNew, setShowNew] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState("")
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")

    if (newPassword !== confirmNewPassword) {
      setError("Mật khẩu xác nhận không khớp.")
      return
    }
    if (newPassword.length < 6) {
      setError("Mật khẩu phải có ít nhất 6 ký tự.")
      return
    }

    setLoading(true)
    try {
      const res = await authApi.resetPassword(token, email, newPassword, confirmNewPassword)
      if (!res.success) {
        setError(res.message ?? "Đặt lại mật khẩu thất bại.")
        return
      }
      setSuccess(true)
    } catch (err: any) {
      console.error("Reset password error:", err)
      setError(`Lỗi kết nối: ${err.message || "Không thể kết nối đến máy chủ."}`)
    } finally {
      setLoading(false)
    }
  }

  if (!token || !email) {
    return (
      <div className="auth-bg min-h-screen flex items-center justify-center px-4">
        <div className="bg-card border border-border rounded-2xl p-8 text-center max-w-sm w-full">
          <AlertCircle size={40} className="text-red-400 mx-auto mb-4" />
          <h2 className="text-lg font-bold text-foreground mb-2">Invalid link</h2>
          <p className="text-sm text-muted-foreground mb-5">
            This password reset link is invalid or has expired.
          </p>
          <Link href="/auth/forgot-password" className="text-primary hover:text-primary/80 text-sm">
            Request a new link →
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="auth-bg min-h-screen flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-[400px] animate-in fade-in slide-in-from-bottom-4 duration-500">
        <div className="flex justify-center mb-8">
          <Logo size="lg" href="/" />
        </div>

        <div className="bg-card border border-border rounded-2xl p-8 shadow-sm">
          {!success ? (
            <>
              <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center mb-5">
                <KeyRound size={22} className="text-primary" />
              </div>

              <h1 className="text-2xl font-bold text-foreground mb-1">Change Your Password</h1>
              <p className="text-sm text-muted-foreground mb-7">
                Enter a new password below to change your password.
              </p>

              {error && (
                <div className="mb-5 flex items-start gap-2.5 rounded-lg bg-red-500/10 border border-red-500/20 px-3.5 py-3 text-sm text-red-400">
                  <AlertCircle size={16} className="mt-0.5 flex-shrink-0" />
                  {error}
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="space-y-1.5">
                  <label htmlFor="reset-new" className="text-sm font-medium text-foreground">
                    New Password
                  </label>
                  <div className="relative">
                    <input
                      id="reset-new"
                      type={showNew ? "text" : "password"}
                      placeholder="••••••••••••"
                      required
                      value={newPassword}
                      onChange={(e) => setNewPassword(e.target.value)}
                      className="auth-input pr-10"
                    />
                    <button type="button" onClick={() => setShowNew(!showNew)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors" tabIndex={-1}>
                      {showNew ? <EyeOff size={16} /> : <Eye size={16} />}
                    </button>
                  </div>
                </div>

                <div className="space-y-1.5">
                  <label htmlFor="reset-confirm" className="text-sm font-medium text-foreground">
                    Re-enter password
                  </label>
                  <div className="relative">
                    <input
                      id="reset-confirm"
                      type={showConfirm ? "text" : "password"}
                      placeholder="••••••••••••"
                      required
                      value={confirmNewPassword}
                      onChange={(e) => setConfirmNewPassword(e.target.value)}
                      className="auth-input pr-10"
                    />
                    <button type="button" onClick={() => setShowConfirm(!showConfirm)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors" tabIndex={-1}>
                      {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
                    </button>
                  </div>
                </div>

                <button
                  id="reset-submit"
                  type="submit"
                  disabled={loading}
                  className="btn-primary-white font-semibold text-sm flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed disabled:transform-none mt-1"
                >
                  {loading ? (
                    <><Loader2 size={16} className="animate-spin" /> Resetting…</>
                  ) : (
                    "Reset Password"
                  )}
                </button>
              </form>

              <div className="mt-6 flex justify-center">
                <Link href="/login" className="flex items-center gap-1.5 text-sm text-primary hover:text-primary/80 transition-colors">
                  <ArrowLeft size={15} />
                  Back to Login
                </Link>
              </div>
            </>
          ) : (
            <>
              <div className="w-12 h-12 rounded-xl bg-green-500/15 flex items-center justify-center mb-5">
                <CheckCircle2 size={22} className="text-green-400" />
              </div>
              <h2 className="text-xl font-bold text-foreground mb-2">Password reset!</h2>
              <p className="text-sm text-muted-foreground mb-6">
                Your password has been successfully changed. You can now sign in with your new password.
              </p>
              <Link
                href="/login"
                className="btn-primary-white font-semibold text-sm flex items-center justify-center"
              >
                Sign In
              </Link>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
