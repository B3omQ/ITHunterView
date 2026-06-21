import AppShell from '@/components/layout/AppShell';

// (admin) layout — auth guard + role: admin
export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
