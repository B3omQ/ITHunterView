'use client';

import { Sidebar } from './Sidebar';
import { useAuthStore } from '@/store/auth.store';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { PageLoader } from '@/components/shared/PageLoader';

/**
 * AppShell — layout shell cho mọi authenticated route group.
 * Làm auth guard: redirect về /login nếu không có token.
 * Mỗi route group layout.tsx gọi <AppShell> thay vì tự guard riêng.
 */
export default function AppShell({ children }: { children: React.ReactNode }) {
  const { user, accessToken } = useAuthStore();
  const router = useRouter();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    
    // 1. Nếu chưa có token hoặc chưa load được user -> login
    if (!accessToken || !user) {
      router.push('/login');
      return;
    }

    // 2. Authorization check dựa vào đường dẫn và role
    const pathname = window.location.pathname;
    const currentRole = user.role?.name?.toLowerCase() || '';

    // Route-Role mappings
    const isRestrictedRoute = (routePrefix: string) => pathname.startsWith(routePrefix);

    if (isRestrictedRoute('/admin') && currentRole !== 'admin') {
      router.push('/unauthorized');
    } else if (isRestrictedRoute('/staff') && currentRole !== 'staff' && currentRole !== 'admin') {
      router.push('/unauthorized');
    } else if (isRestrictedRoute('/recruiter') && currentRole !== 'recruiter') {
      router.push('/unauthorized');
    } else if (isRestrictedRoute('/candidate') && currentRole !== 'candidate') {
      router.push('/unauthorized');
    }
  }, [accessToken, user, router]);

  if (!mounted || !accessToken || !user) {
    return <PageLoader message="Loading..." />;
  }

  return (
    <div className="flex h-screen bg-background overflow-hidden">
      <Sidebar />
      <main className="flex-1 overflow-y-auto p-4 md:p-8">{children}</main>
    </div>
  );
}
