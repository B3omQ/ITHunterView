import AppShell from '@/components/layout/AppShell';

// (candidate) layout — auth guard + role: candidate
export default function CandidateLayout({ children }: { children: React.ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
