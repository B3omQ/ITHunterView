import axios from 'axios';
import { authStore } from '@/store/auth.store';

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:50504',
  timeout: 15000,
});

api.interceptors.request.use((config) => {
  // Fix SSR hydration: Zustand store may be null if initialized on server.
  // Always fall back to localStorage on client to get the real token.
  const storeToken = authStore.getState().accessToken;
  const localToken = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
  const token = storeToken || localToken;

  if (process.env.NODE_ENV === 'development') {
    console.log(`[api] ${config.method?.toUpperCase()} ${config.url} | token: ${token ? 'present' : 'MISSING'}`);
  }

  if (token) config.headers.Authorization = `Bearer ${token}`;

  if (typeof window !== 'undefined') {
    let fingerprint = localStorage.getItem('X-Device-Fingerprint');
    if (!fingerprint) {
      fingerprint = 'fp_' + Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
      localStorage.setItem('X-Device-Fingerprint', fingerprint);
    }
    config.headers['X-Device-Fingerprint'] = fingerprint;
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      const url = error.config?.url ?? '';
      // Only auto-logout + redirect for auth-related endpoints (token refresh, etc.)
      // For all other endpoints, just clear auth state and let the caller handle it.
      const isAuthEndpoint = url.includes('/api/auth/');
      if (isAuthEndpoint) {
        void authStore.getState().logout();
        if (typeof window !== 'undefined') {
          const currentPath = window.location.pathname + window.location.search;
          window.location.href = `/login?redirect=${encodeURIComponent(currentPath)}`;
        }
      } else {
        // Non-auth endpoint 401: clear stale auth state but don't redirect
        // Let the component handle the UX (show a toast, inline error, etc.)
        void authStore.getState().logout();
      }
    }
    return Promise.reject(error);
  }
);

export default api;
