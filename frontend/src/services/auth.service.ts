import api from './api-client';
import type {
  LoginCredentials,
  RegisterCredentials,
  LoginResponse,
  ForgotPasswordPayload,
  ResetPasswordPayload,
  ChangePasswordPayload,
  User,
} from '@/types/auth.types';
import type { ApiResponse } from '@/types/api.types';

// Mỗi hàm = 1 endpoint, trả typed data. KHÔNG có useState/router/JSX/toast.
export const authService = {
  login: (credentials: LoginCredentials) =>
    api
      .post<ApiResponse<LoginResponse>>('/api/auth/login', credentials)
      .then((r) => r.data),

  register: (credentials: RegisterCredentials) =>
    api
      .post<ApiResponse<LoginResponse>>('/api/auth/register', credentials)
      .then((r) => r.data),

  googleAuth: (idToken: string, roleType = 'candidate') =>
    api
      .post<ApiResponse<LoginResponse>>('/api/auth/google', { idToken, roleType })
      .then((r) => r.data),

  refreshToken: (refreshToken: string) =>
    api
      .post<ApiResponse<LoginResponse>>('/api/auth/refresh-token', { refreshToken })
      .then((r) => r.data),

  logout: (refreshToken: string) =>
    api
      .post<ApiResponse<null>>('/api/auth/logout', { refreshToken })
      .then((r) => r.data),

  forgotPassword: (payload: ForgotPasswordPayload) =>
    api
      .post<ApiResponse<null>>('/api/auth/forgot-password', payload)
      .then((r) => r.data),

  resetPassword: (payload: ResetPasswordPayload) =>
    api
      .post<ApiResponse<null>>('/api/auth/reset-password', payload)
      .then((r) => r.data),

  changePassword: (payload: ChangePasswordPayload) =>
    api
      .post<ApiResponse<null>>('/api/auth/change-password', payload)
      .then((r) => r.data),

  verifyEmail: (token: string) =>
    api
      .get<ApiResponse<null>>(`/api/auth/verify-email?token=${encodeURIComponent(token)}`)
      .then((r) => r.data),

  resendVerification: (email: string) =>
    api
      .post<ApiResponse<null>>('/api/auth/resend-verification', { email })
      .then((r) => r.data),

  getMe: () =>
    api.get<ApiResponse<User>>('/api/me').then((r) => r.data),
};
