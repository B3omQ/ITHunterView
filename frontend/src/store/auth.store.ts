import { create } from 'zustand';
import type { User } from '@/types/auth.types';
import { authService } from '@/services/auth.service';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: User | null;
  setAuth: (token: string, refreshToken: string, user: User) => void;
  logout: () => Promise<void>;
}

const getInitialUser = (): User | null => {
  if (typeof window === 'undefined') return null;
  try {
    const userStr = localStorage.getItem('user');
    return userStr && userStr !== 'undefined' ? JSON.parse(userStr) : null;
  } catch {
    return null;
  }
};

// Hook-based selector (dùng trong React components)
export const useAuthStore = create<AuthState>((set, get) => ({
  accessToken: typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null,
  refreshToken: typeof window !== 'undefined' ? localStorage.getItem('refreshToken') : null,
  user: getInitialUser(),

  setAuth: (token, refreshToken, user) => {
    if (typeof window !== 'undefined') {
      localStorage.setItem('accessToken', token);
      localStorage.setItem('refreshToken', refreshToken);
      localStorage.setItem('user', JSON.stringify(user));
    }
    set({ accessToken: token, refreshToken, user });
  },

  logout: async () => {
    const { refreshToken } = get();

    // Gọi API backend để thu hồi refresh token (fire-and-forget)
    if (refreshToken) {
      try {
        await authService.logout(refreshToken);
      } catch {
        // Không block logout dù API lỗi
      }
    }

    // Xóa toàn bộ dữ liệu auth khỏi localStorage
    if (typeof window !== 'undefined') {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      localStorage.removeItem('google_auth_role');
      // Giữ X-Device-Fingerprint để tracking thiết bị liên tục
    }

    set({ accessToken: null, refreshToken: null, user: null });
  },
}));

// Non-hook export — dùng trong api-client.ts interceptor (không phải React context)
export const authStore = useAuthStore;
