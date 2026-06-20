import axios from 'axios';
import { authStore } from '@/store/auth.store';

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:51148',
  timeout: 15000,
});

api.interceptors.request.use((config) => {
  const token = authStore.getState().accessToken; // rỗng với Guest — interceptor tự bỏ qua
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
      authStore.getState().logout();
      if (typeof window !== 'undefined') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
