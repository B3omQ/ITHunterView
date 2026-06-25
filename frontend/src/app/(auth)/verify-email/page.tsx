"use client"

import { useState, useEffect, Suspense } from "react"
import { useSearchParams } from "next/navigation"
import Link from "next/link"
import { Loader2, CheckCircle2, XCircle, Mail, Send } from "lucide-react"
import { authService } from "@/services/auth.service"
import { Logo } from "@/components/layout/Logo"

// loading  → đang gọi API xác thực token
// pending  → vừa đăng ký xong, chờ user click link trong email
// success  → xác thực thành công
// error    → token sai / hết hạn
// missing  → vào trang trực tiếp không có token lẫn email
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

  // Resend form state
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
          setMessage(res.message ?? "Email của bạn đã được xác thực thành công.")
        } else {
          setStatus("error")
          setMessage(res.message ?? "Xác thực email thất bại.")
        }
      })
      .catch((err: any) => {
        const msg =
          err.response?.data?.message ||
          err.message ||
          "Không thể kết nối đến máy chủ."
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
      setResendError(err.response?.data?.message || err.message || "Gửi lại thất bại.")
    } finally {
      setResendLoading(false)
    }
  }

  const ResendForm = () => (
    <div className="mt-6 pt-6 border-t border-border">
      {!resendSent ? (
        <>
          <p className="text-sm text-muted-foreground mb-3 text-center">
            Không nhận được email? Gửi lại link xác thực:
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
              Gửi lại
            </button>
          </form>
        </>
      ) : (
        <div className="flex items-start gap-2.5 rounded-lg bg-green-500/10 border border-green-500/20 px-3.5 py-3 text-sm text-green-400">
          <CheckCircle2 size={16} className="mt-0.5 flex-shrink-0" />
          Đã gửi! Kiểm tra hộp thư (kể cả spam) và nhấn link mới.
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
              <h1 className="text-xl font-bold text-foreground mb-2">Đang xác thực email…</h1>
              <p className="text-sm text-muted-foreground">Vui lòng chờ trong giây lát.</p>
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
                    ? "Đã gửi lại email xác thực 📩"
                    : isRegistered
                    ? "Đăng ký thành công! 🎉"
                    : "Xác thực email của bạn"}
                </h1>
                <p className="text-sm text-muted-foreground">
                  Chúng tôi đã gửi một link xác thực đến
                </p>
                <p className="text-sm font-semibold text-foreground mt-1 mb-4">
                  {emailParam}
                </p>
                <div className="p-4 rounded-xl bg-primary/5 border border-primary/20 text-sm text-left space-y-1.5">
                  <p className="text-foreground font-medium">Hướng dẫn:</p>
                  <p className="text-muted-foreground">1. Mở hộp thư của bạn</p>
                  <p className="text-muted-foreground">2. Tìm email từ <span className="text-foreground">ITHunterView</span></p>
                  <p className="text-muted-foreground">3. Nhấn <span className="text-primary font-medium">"Xác thực Email"</span></p>
                  <p className="text-muted-foreground text-xs mt-1">⏱ Link có hiệu lực trong 24 giờ</p>
                </div>
              </div>
              <ResendForm />
              <div className="mt-4 text-center">
                <Link href="/login" className="text-sm text-muted-foreground hover:text-primary transition-colors">
                  Quay lại đăng nhập
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
              <h1 className="text-xl font-bold text-foreground mb-2">Xác thực thành công!</h1>
              <p className="text-sm text-muted-foreground mb-6">{message}</p>
              <Link
                href="/login"
                className="w-full h-11 rounded-xl bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm flex items-center justify-center transition-all"
              >
                Đăng nhập ngay
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
                <h1 className="text-xl font-bold text-foreground mb-2">Link hết hạn</h1>
                <p className="text-sm text-muted-foreground">{message}</p>
              </div>
              <ResendForm />
              <div className="mt-4 text-center">
                <Link href="/login" className="text-sm text-muted-foreground hover:text-primary transition-colors">
                  Quay lại đăng nhập
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
                <h1 className="text-xl font-bold text-foreground mb-2">Chưa xác thực email?</h1>
                <p className="text-sm text-muted-foreground">
                  Nhập email đã đăng ký để nhận lại link xác thực.
                </p>
              </div>
              <ResendForm />
              <div className="mt-4 text-center">
                <Link href="/login" className="text-sm text-muted-foreground hover:text-primary transition-colors">
                  Quay lại đăng nhập
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
