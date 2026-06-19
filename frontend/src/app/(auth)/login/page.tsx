"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { Eye, EyeOff, Loader2, AlertCircle } from "lucide-react"
import { authService } from "@/services/auth.service"
import { useAuthStore } from "@/store/auth.store"
import { getDashboardPath } from "@/lib/constants"
import { Logo } from "@/components/layout/Logo"

export default function LoginPage() {
  const router = useRouter()
  const { setAuth } = useAuthStore()

  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [showPwd, setShowPwd] = useState(false)
  const [error, setError] = useState("")
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")
    setLoading(true)
    try {
      const res = await authService.login(email, password)
      if (!res.success || !res.data) {
        setError(res.message ?? "Đăng nhập thất bại")
        return
      }
      
      const payload = res.data.data;
      const user = {
        id: payload.userId,
        email: payload.email,
        fullName: payload.fullName,
        role: { name: payload.role },
        avatarUrl: payload.avatarUrl
      };
      setAuth(payload.accessToken, user);
      router.push(getDashboardPath(user.role.name));
    } catch (err: any) {
      console.error("Login error details:", err)
      setError(`Lỗi kết nối: ${err.message || "Không thể kết nối đến máy chủ."}`)
    } finally {
      setLoading(false)
    }
  }

  const handleGoogle = () => {
    // In production: trigger Google Sign-In, get idToken, then call authApi.googleAuth
    alert("Google OAuth – cần cấu hình Google Client ID")
  }

  return (
    <div className="auth-bg min-h-screen flex items-center justify-center px-4 py-12">
      {/* Card */}
      <div className="w-full max-w-[400px] animate-in fade-in slide-in-from-bottom-4 duration-500">
        {/* Logo */}
        <div className="flex justify-center mb-8">
          <Logo size="lg" href="/" />
        </div>

        <div className="bg-card border border-border rounded-2xl p-8 shadow-sm">
          {/* Header */}
          <div className="mb-7">
            <h1 className="text-2xl font-bold text-foreground mb-1">Welcome back</h1>
            <p className="text-sm text-muted-foreground">
              Sign in to your ITHunterView account
            </p>
          </div>

          {/* Error */}
          {error && (
            <div className="mb-5 flex items-start gap-2.5 rounded-lg bg-red-500/10 border border-red-500/20 px-3.5 py-3 text-sm text-red-400">
              <AlertCircle size={16} className="mt-0.5 flex-shrink-0" />
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Email */}
            <div className="space-y-1.5">
              <label htmlFor="login-email" className="text-sm font-medium text-foreground">
                Email address
              </label>
              <input
                id="login-email"
                type="email"
                autoComplete="email"
                placeholder="you@example.com"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="auth-input"
              />
            </div>

            {/* Password */}
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label htmlFor="login-password" className="text-sm font-medium text-foreground">
                  Password
                </label>
                <Link
                  href="/auth/forgot-password"
                  className="text-xs text-primary hover:text-primary/80 transition-colors"
                >
                  Forgot password?
                </Link>
              </div>
              <div className="relative">
                <input
                  id="login-password"
                  type={showPwd ? "text" : "password"}
                  autoComplete="current-password"
                  placeholder="••••••••"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="auth-input pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPwd(!showPwd)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                  tabIndex={-1}
                  aria-label={showPwd ? "Hide password" : "Show password"}
                >
                  {showPwd ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>

            {/* Submit */}
            <button
              id="login-submit"
              type="submit"
              disabled={loading}
              className="btn-primary-white font-semibold text-sm flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed disabled:transform-none"
            >
              {loading ? (
                <><Loader2 size={16} className="animate-spin" /> Signing in…</>
              ) : (
                "Sign In"
              )}
            </button>
          </form>

          {/* Divider */}
          <div className="my-6 flex items-center gap-3">
            <div className="flex-1 h-px bg-border" />
            <span className="text-xs text-muted-foreground">or</span>
            <div className="flex-1 h-px bg-border" />
          </div>

          {/* Google */}
          <button
            id="login-google"
            type="button"
            onClick={handleGoogle}
            className="w-full h-11 rounded-xl border border-border bg-muted hover:bg-muted/80 text-foreground font-medium text-sm flex items-center justify-center gap-2.5 transition-all hover:border-primary"
          >
            <GoogleIcon />
            Continue with Google
          </button>

          {/* Footer */}
          <p className="mt-6 text-center text-sm text-muted-foreground">
            Don&apos;t have an account?{" "}
            <Link href="/auth/register" className="text-primary hover:text-primary/80 font-medium transition-colors">
              Sign up
            </Link>
          </p>
        </div>
      </div>

      
    </div>
  )
}

function GoogleIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 48 48">
      <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
      <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
      <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
      <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
    </svg>
  )
}
