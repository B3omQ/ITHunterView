"use client"

import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  type ReactNode,
} from "react"
import { useRouter } from "next/navigation"
import type { LoginResponse } from "@/api/auth"

interface AuthUser {
  userId: string
  email: string
  fullName: string
  role: string
  avatarUrl?: string
}

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  login: (data: LoginResponse) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const router = useRouter()

  useEffect(() => {
    const raw = localStorage.getItem("auth_user")
    if (raw) {
      try {
        setUser(JSON.parse(raw))
      } catch {
        localStorage.removeItem("auth_user")
      }
    }
    setIsLoading(false)
  }, [])

  const login = useCallback((data: LoginResponse) => {
    localStorage.setItem("accessToken", data.accessToken)
    localStorage.setItem("refreshToken", data.refreshToken)
    const u: AuthUser = {
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      role: data.role,
      avatarUrl: data.avatarUrl,
    }
    localStorage.setItem("auth_user", JSON.stringify(u))
    setUser(u)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem("accessToken")
    localStorage.removeItem("refreshToken")
    localStorage.removeItem("auth_user")
    setUser(null)
    router.push("/login")
  }, [router])

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider")
  return ctx
}

/** Redirect helper based on role */
export function getDashboardPath(role: string): string {
  switch (role.toLowerCase()) {
    case "admin":   return "/dashboard/admin"
    case "staff":   return "/dashboard/staff"
    case "recruiter": return "/dashboard/recruiter"
    case "candidate": return "/dashboard/candidate"
    default:        return "/dashboard/candidate"
  }
}
