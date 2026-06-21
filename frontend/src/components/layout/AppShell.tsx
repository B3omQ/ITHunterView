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
    if (!accessToken || !user) {
      router.push('/login');
    }
  }, [accessToken, user, router]);

  if (!mounted || !accessToken || !user) {
    return <PageLoader message="Đang tải..." />;
  }

  return (
    <div className="flex h-screen bg-[#F0F2F5] overflow-hidden">
      <Sidebar />
      <main className="flex-1 overflow-y-auto p-4 md:p-8">{children}</main>
    </div>
  );
}
