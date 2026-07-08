import AppShell from '@/components/layout/AppShell';
import { OnboardingGate } from '@/components/shared/OnboardingGate';

// (candidate) layout — auth guard + role: candidate
export default function CandidateLayout({ children }: { children: React.ReactNode }) {
  return (
    <AppShell>
      <OnboardingGate>
        {children}
      </OnboardingGate>
    </AppShell>
  );
}
