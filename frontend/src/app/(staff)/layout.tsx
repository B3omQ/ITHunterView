import AppShell from '@/components/layout/AppShell';

// (staff) layout — auth guard + role: staff
export default function StaffLayout({ children }: { children: React.ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
