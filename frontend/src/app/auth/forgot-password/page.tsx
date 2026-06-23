"use client"

import { useState } from "react"
import Link from "next/link"
import { ArrowLeft, Loader2, AlertCircle, Mail, CheckCircle2 } from "lucide-react"
import { authService } from "@/services/auth.service"
import { Logo } from "@/components/layout/Logo"

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("")
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState("")
  const [sent, setSent] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")
    setLoading(true)
    try {
      const res = await authService.forgotPassword({ email })
      setSent(true)
    } catch (err: any) {
      console.error("Forgot password error:", err)
      setError(`Lỗi kết nối: ${err.message || "Không thể kết nối đến máy chủ."}`)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-bg min-h-screen flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-[400px] animate-in fade-in slide-in-from-bottom-4 duration-500">
        <div className="flex justify-center mb-8">
          <Logo size="lg" href="/" />
        </div>

        <div className="bg-card border border-border rounded-2xl p-8 shadow-sm">
          {!sent ? (
            <>
              <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center mb-5">
                <Mail size={22} className="text-primary" />
              </div>

              <h1 className="text-2xl font-bold text-foreground mb-1">Forgot Password</h1>
              <p className="text-sm text-muted-foreground mb-7">
                Enter your email address and we&apos;ll send you a link to reset your password.
              </p>

              {error && (
                <div className="mb-5 flex items-start gap-2.5 rounded-lg bg-red-500/10 border border-red-500/20 px-3.5 py-3 text-sm text-red-400">
                  <AlertCircle size={16} className="mt-0.5 flex-shrink-0" />
                  {error}
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-5">
                <div className="space-y-1.5">
                  <label htmlFor="forgot-email" className="text-sm font-medium text-foreground">
                    Email Address
                  </label>
                  <input
                    id="forgot-email"
                    type="email"
                    placeholder="you@example.com"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="auth-input"
                  />
                </div>

                <button
                  id="forgot-submit"
                  type="submit"
                  disabled={loading}
                  className="w-full h-11 rounded-xl bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed disabled:transform-none transition-all"
                >
                  {loading ? (
                    <><Loader2 size={16} className="animate-spin" /> Sending…</>
                  ) : (
                    "Send Reset Link"
                  )}
                </button>
              </form>

              <div className="mt-6 flex justify-center">
                <Link
                  href="/login"
                  className="flex items-center gap-1.5 text-sm text-primary hover:text-primary/80 transition-colors"
                >
                  <ArrowLeft size={15} />
                  Back to Login
                </Link>
              </div>
            </>
          ) : (
            <>
              {/* Success state */}
              <div className="w-12 h-12 rounded-xl bg-green-500/15 flex items-center justify-center mb-5">
                <CheckCircle2 size={22} className="text-green-400" />
              </div>
              <h2 className="text-xl font-bold text-foreground mb-2">Check your email</h2>
              <p className="text-sm text-muted-foreground mb-6">
                If an account exists for <span className="text-foreground font-medium">{email}</span>,
                we sent a password reset link. Check your inbox (and spam folder).
              </p>
              <div className="p-4 rounded-xl bg-indigo-500/10 border border-indigo-500/20 text-sm text-indigo-300 mb-6">
                The link expires in <strong>15 minutes</strong>.
              </div>
              <button
                onClick={() => { setSent(false); setEmail("") }}
                className="w-full h-10 rounded-xl border border-border text-sm text-muted-foreground hover:text-foreground hover:border-primary transition-all"
              >
                Try a different email
              </button>
              <div className="mt-4 flex justify-center">
                <Link
                  href="/login"
                  className="flex items-center gap-1.5 text-sm text-primary hover:text-primary/80 transition-colors"
                >
                  <ArrowLeft size={15} />
                  Back to Login
                </Link>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
