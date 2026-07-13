import AppShell from '@/components/layout/AppShell';
import { CompanyReminderModal } from '@/components/shared/CompanyReminderModal';

// (recruiter) layout — auth guard + role: recruiter
export default function RecruiterLayout({ children }: { children: React.ReactNode }) {
  return (
    <AppShell>
      {children}
      <CompanyReminderModal />
    </AppShell>
  );
}

