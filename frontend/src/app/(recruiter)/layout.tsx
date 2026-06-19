import AppShell from '@/components/layout/AppShell';

// (recruiter) layout — auth guard + role: recruiter
export default function RecruiterLayout({ children }: { children: React.ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
