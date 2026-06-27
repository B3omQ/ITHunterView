"use client"

import { useState, useEffect, Suspense } from "react"
import { useSearchParams } from "next/navigation"
import Link from "next/link"
import { Loader2, CheckCircle2, XCircle, Mail, Send } from "lucide-react"
import { authService } from "@/services/auth.service"
import { Logo } from "@/components/layout/Logo"

type Status = "loading" | "pending" | "success" | "error" | "missing"

function VerifyEmailContent() {
  const searchParams = useSearchParams()
  const token = searchParams.get("token")
  const emailParam = searchParams.get("email") ?? ""
  const isRegistered = searchParams.get("registered") === "1"
  const isResent = searchParams.get("resent") === "1"

  const [status, setStatus] = useState<Status>(() => {
    if (token) return "loading"
    if (emailParam) return "pending"
    return "missing"
  })
  const [message, setMessage] = useState("")

  const [resendEmail, setResendEmail] = useState(emailParam)
  const [resendLoading, setResendLoading] = useState(false)
  const [resendSent, setResendSent] = useState(false)
  const [resendError, setResendError] = useState("")

  useEffect(() => {
    if (!token) return

    authService
      .verifyEmail(token)
      .then((res) => {
        if (res.success) {
          setStatus("success")
          setMessage(res.message ?? "Your email has been verified successfully.")
        } else {
          setStatus("error")
          setMessage(res.message ?? "Email verification failed.")
        }
      })
      .catch((err: any) => {
        const msg =
          err.response?.data?.message ||
          err.message ||
          "Cannot connect to the server."
        setStatus("error")
        setMessage(msg)
      })
  }, [token])

  const handleResend = async (e: React.FormEvent) => {
    e.preventDefault()
    setResendError("")
    setResendLoading(true)
    try {
      await authService.resendVerification(resendEmail)
      setResendSent(true)
    } catch (err: any) {
      setResendError(err.response?.data?.message || err.message || "Resend failed.")
    } finally {
      setResendLoading(false)
    }
  }

  const ResendForm = () => (
    <div className="mt-6 pt-6 border-t border-border">
      {!resendSent ? (
        <>
          <p className="text-sm text-muted-foreground mb-3 text-center">
            Didn't receive the email? Resend verification link:
          </p>
          {resendError && (
            <p className="text-xs text-red-400 text-center mb-3">{resendError}</p>
          )}
          <form onSubmit={handleResend} className="flex gap-2">
            <input
              type="email"
              required
              placeholder="you@example.com"
              value={resendEmail}
              onChange={(e) => setResendEmail(e.target.value)}
              className="auth-input flex-1 text-sm"
            />
            <button
              type="submit"
              disabled={resendLoading}
              className="h-11 px-4 rounded-xl bg-primary hover:bg-primary/90 text-primary-foreground text-sm font-semibold flex items-center gap-1.5 transition-all disabled:opacity-60 flex-shrink-0"
            >
              {resendLoading
                ? <Loader2 size={14} className="animate-spin" />
                : <Send size={14} />}
              Resend
            </button>
          </form>
        </>
      ) : (
        <div className="flex items-start gap-2.5 rounded-lg bg-green-500/10 border border-green-500/20 px-3.5 py-3 text-sm text-green-400">
          <CheckCircle2 size={16} className="mt-0.5 flex-shrink-0" />
          Sent! Check your inbox (including spam) and click the new link.
        </div>
      )}
    </div>
  )

  return (
    <div className="auth-bg min-h-screen flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-[440px] animate-in fade-in slide-in-from-bottom-4 duration-500">
        <div className="flex justify-center mb-8">
          <Logo size="lg" href="/" />
        </div>

        <div className="bg-card border border-border rounded-2xl p-8 shadow-sm">

          {/* ── LOADING ─────────────────────────────────────── */}
          {status === "loading" && (
            <div className="text-center">
              <div className="w-14 h-14 rounded-2xl bg-primary/10 flex items-center justify-center mx-auto mb-5">
                <Loader2 size={28} className="text-primary animate-spin" />
              </div>
              <h1 className="text-xl font-bold text-foreground mb-2">Verifying email...</h1>
              <p className="text-sm text-muted-foreground">Please wait a moment.</p>
            </div>
          )}

          {/* ── PENDING (vừa đăng ký xong) ──────────────────── */}
          {status === "pending" && (
            <>
              <div className="text-center">
                <div className="w-14 h-14 rounded-2xl bg-primary/10 flex items-center justify-center mx-auto mb-5">
                  <Mail size={28} className="text-primary" />
                </div>
                <h1 className="text-xl font-bold text-foreground mb-2">
                  {isResent
                    ? "Verification email resent 📩"
                    : isRegistered
                    ? "Registration successful! 🎉"
                    : "Verify your email"}
                </h1>
                <p className="text-sm text-muted-foreground">
                  We have sent a verification link to
                </p>
                <p className="text-sm font-semibold text-foreground mt-1 mb-4">
                  {emailParam}
                </p>
                <div className="p-4 rounded-xl bg-primary/5 border border-primary/20 text-sm text-left space-y-1.5">
                  <p className="text-foreground font-medium">Instructions:</p>
                  <p className="text-muted-foreground">1. Open your inbox</p>
                  <p className="text-muted-foreground">2. Find the email from <span className="text-foreground">ITHunterView</span></p>
                  <p className="text-muted-foreground">3. Click <span className="text-primary font-medium">"Verify Email"</span></p>
                  <p className="text-muted-foreground text-xs mt-1">⏱ Link is valid for 24 hours</p>
                </div>
              </div>
              <ResendForm />
              <div className="mt-4 text-center">
                <Link href="/login" className="text-sm text-muted-foreground hover:text-primary transition-colors">
                  Back to login
                </Link>
              </div>
            </>
          )}

          {/* ── SUCCESS ─────────────────────────────────────── */}
          {status === "success" && (
            <div className="text-center">
              <div className="w-14 h-14 rounded-2xl bg-green-500/15 flex items-center justify-center mx-auto mb-5">
                <CheckCircle2 size={28} className="text-green-400" />
              </div>
              <h1 className="text-xl font-bold text-foreground mb-2">Verification successful!</h1>
              <p className="text-sm text-muted-foreground mb-6">{message}</p>
              <Link
                href="/login"
                className="w-full h-11 rounded-xl bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm flex items-center justify-center transition-all"
              >
                Login now
              </Link>
            </div>
          )}

          {/* ── ERROR (token hết hạn / sai) ─────────────────── */}
          {status === "error" && (
            <>
              <div className="text-center">
                <div className="w-14 h-14 rounded-2xl bg-red-500/10 flex items-center justify-center mx-auto mb-5">
                  <XCircle size={28} className="text-red-400" />
                </div>
                <h1 className="text-xl font-bold text-foreground mb-2">Link expired</h1>
                <p className="text-sm text-muted-foreground">{message}</p>
              </div>
              <ResendForm />
              <div className="mt-4 text-center">
                <Link href="/login" className="text-sm text-muted-foreground hover:text-primary transition-colors">
                  Back to login
                </Link>
              </div>
            </>
          )}

          {/* ── MISSING (vào thẳng không có params) ─────────── */}
          {status === "missing" && (
            <>
              <div className="text-center">
                <div className="w-14 h-14 rounded-2xl bg-amber-500/10 flex items-center justify-center mx-auto mb-5">
                  <Mail size={28} className="text-amber-400" />
                </div>
                <h1 className="text-xl font-bold text-foreground mb-2">Haven't verified your email?</h1>
                <p className="text-sm text-muted-foreground">
                  Enter your registered email to resend the verification link.
                </p>
              </div>
              <ResendForm />
              <div className="mt-4 text-center">
                <Link href="/login" className="text-sm text-muted-foreground hover:text-primary transition-colors">
                  Back to login
                </Link>
              </div>
            </>
          )}

        </div>
      </div>
    </div>
  )
}

export default function VerifyEmailPage() {
  return (
    <Suspense fallback={
      <div className="auth-bg min-h-screen flex items-center justify-center">
        <Loader2 size={32} className="text-primary animate-spin" />
      </div>
    }>
      <VerifyEmailContent />
    </Suspense>
  )
}
