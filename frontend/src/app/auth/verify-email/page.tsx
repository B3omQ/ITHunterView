"use client"

import { useEffect, useState } from "react"
import { useSearchParams } from "next/navigation"
import Link from "next/link"
import { Loader2, CheckCircle2, XCircle } from "lucide-react"
import { authApi } from "@/api/auth"
import { Logo } from "@/components/Logo"

export default function VerifyEmailPage() {
  const searchParams = useSearchParams()
  const token = searchParams.get("token") ?? ""

  const [status, setStatus] = useState<"loading" | "success" | "error">("loading")
  const [message, setMessage] = useState("")

  useEffect(() => {
    if (!token) {
      setStatus("error")
      setMessage("Token không hợp lệ.")
      return
    }
    authApi.verifyEmail(token).then((res) => {
      if (res.success) {
        setStatus("success")
        setMessage(res.message ?? "Xác thực thành công!")
      } else {
        setStatus("error")
        setMessage(res.message ?? "Xác thực thất bại.")
      }
    }).catch((err: any) => {
      console.error("Verify email error:", err)
      setStatus("error")
      setMessage(`Lỗi kết nối: ${err.message || "Không thể kết nối đến máy chủ."}`)
    })
  }, [token])

  return (
    <div className="auth-bg min-h-screen flex items-center justify-center px-4">
      <div className="w-full max-w-sm animate-in fade-in zoom-in-95 duration-500">
        <div className="flex justify-center mb-8">
          <Logo size="lg" href="/" />
        </div>
        <div className="bg-card border border-border rounded-2xl p-8 shadow-sm text-center">
          {status === "loading" && (
            <>
              <Loader2 size={40} className="text-primary mx-auto mb-4 animate-spin" />
              <h2 className="text-lg font-bold text-foreground mb-2">Verifying your email…</h2>
              <p className="text-sm text-muted-foreground">Please wait a moment.</p>
            </>
          )}
          {status === "success" && (
            <>
              <CheckCircle2 size={40} className="text-green-400 mx-auto mb-4" />
              <h2 className="text-xl font-bold text-foreground mb-2">Email Verified!</h2>
              <p className="text-sm text-muted-foreground mb-6">{message}</p>
              <Link href="/login" className="btn-primary-white font-semibold text-sm items-center justify-center">
                Sign In Now
              </Link>
            </>
          )}
          {status === "error" && (
            <>
              <XCircle size={40} className="text-red-400 mx-auto mb-4" />
              <h2 className="text-xl font-bold text-foreground mb-2">Verification Failed</h2>
              <p className="text-sm text-muted-foreground mb-6">{message}</p>
              <Link href="/auth/register" className="text-primary hover:text-primary/80 text-sm">
                Register again →
              </Link>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
