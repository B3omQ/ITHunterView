const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:51148"

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  userId: string
  email: string
  fullName: string
  role: string
  avatarUrl?: string
}

export interface ApiResponse<T> {
  success: boolean
  message?: string
  data?: T
}

async function request<T>(
  path: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("accessToken") : null

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...((options.headers as Record<string, string>) ?? {}),
  }

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })
  return res.json()
}

export const authApi = {
  login: (email: string, password: string) =>
    request<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }),

  register: (email: string, password: string, confirmPassword: string, roleType: string) =>
    request<LoginResponse>("/api/auth/register", {
      method: "POST",
      body: JSON.stringify({ email, password, confirmPassword, roleType }),
    }),

  googleAuth: (idToken: string, roleType = "candidate") =>
    request<LoginResponse>("/api/auth/google", {
      method: "POST",
      body: JSON.stringify({ idToken, roleType }),
    }),

  refreshToken: (refreshToken: string) =>
    request<LoginResponse>("/api/auth/refresh-token", {
      method: "POST",
      body: JSON.stringify({ refreshToken }),
    }),

  logout: (refreshToken: string) =>
    request<null>("/api/auth/logout", {
      method: "POST",
      body: JSON.stringify({ refreshToken }),
    }),

  forgotPassword: (email: string) =>
    request<null>("/api/auth/forgot-password", {
      method: "POST",
      body: JSON.stringify({ email }),
    }),

  resetPassword: (token: string, email: string, newPassword: string, confirmNewPassword: string) =>
    request<null>("/api/auth/reset-password", {
      method: "POST",
      body: JSON.stringify({ token, email, newPassword, confirmNewPassword }),
    }),

  changePassword: (currentPassword: string, newPassword: string, confirmNewPassword: string) =>
    request<null>("/api/auth/change-password", {
      method: "POST",
      body: JSON.stringify({ currentPassword, newPassword, confirmNewPassword }),
    }),

  verifyEmail: (token: string) =>
    request<null>(`/api/auth/verify-email?token=${encodeURIComponent(token)}`),

  getMe: () => request<LoginResponse>("/api/me"),
}
