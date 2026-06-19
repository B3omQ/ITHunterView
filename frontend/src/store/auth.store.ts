import { create } from 'zustand';

interface AuthState {
  accessToken: string | null;
  user: any | null;
  setAuth: (token: string, user: any) => void;
  logout: () => void;
}

const getInitialUser = () => {
  if (typeof window === 'undefined') return null;
  try {
    const userStr = localStorage.getItem('user');
    return userStr && userStr !== 'undefined' ? JSON.parse(userStr) : null;
  } catch (e) {
    return null;
  }
};

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null,
  user: getInitialUser(),
  setAuth: (token, user) => {
    if (typeof window !== 'undefined') {
      localStorage.setItem('accessToken', token);
      localStorage.setItem('user', JSON.stringify(user));
    }
    set({ accessToken: token, user });
  },
  logout: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
    }
    set({ accessToken: null, user: null });
  },
}));
