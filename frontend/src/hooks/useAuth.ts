import { useMutation } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { authService } from '@/services/auth.service';
import { useAuthStore } from '@/store/auth.store';
import { getDashboardPath } from '@/lib/constants';
import type { User } from '@/types/auth.types';

// Hook login — wrap authService + expose cho login page
export function useLogin() {
  const { setAuth } = useAuthStore();
  const router = useRouter();

  return useMutation({
    mutationFn: authService.login,
    onSuccess: (res) => {
      if (!res.success || !res.data) return;
      const payload = res.data;
      const user: User = {
        id: payload.userId,
        email: payload.email,
        fullName: payload.fullName,
        role: { name: payload.role },
        avatarUrl: payload.avatarUrl,
      };
      setAuth(payload.accessToken, user);
      router.push(getDashboardPath(user.role.name));
    },
  });
}

// Hook register — wrap authService + expose cho register page
export function useRegister() {
  const router = useRouter();

  return useMutation({
    mutationFn: authService.register,
    onSuccess: (res) => {
      if (res.success) {
        router.push('/login?registered=1');
      }
    },
  });
}

// Hook forgotPassword — wrap authService
export function useForgotPassword() {
  return useMutation({
    mutationFn: authService.forgotPassword,
  });
}
