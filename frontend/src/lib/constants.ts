// APP_ROUTES — dùng với Link href và router.push
export const APP_ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  REGISTER: '/register',
  FORGOT_PASSWORD: '/forgot-password',
  RESET_PASSWORD: '/reset-password',
  VERIFY_EMAIL: '/verify-email',

  // Role dashboards — URL không đổi dù dùng route group
  CANDIDATE: {
    DASHBOARD: '/candidate/dashboard',
    PROFILE: '/candidate/profile',
    JOBS: '/candidate/jobs',
    RESUME: '/candidate/resume',
    CV_OPTIMIZER: '/candidate/cv-optimizer',
    INTERVIEW: '/candidate/interview',
    APPLICATIONS: '/candidate/applications',
    PRICING: '/candidate/pricing',
    SETTINGS: '/candidate/settings',
    NOTIFICATIONS: '/candidate/notifications',
  },
  RECRUITER: {
    DASHBOARD: '/recruiter/dashboard',
    COMPANY: '/recruiter/company',
    JOBS: '/recruiter/jobs',
    ANALYTICS: '/recruiter/analytics',
    SETTINGS: '/recruiter/settings',
    NOTIFICATIONS: '/recruiter/notifications',
  },
  STAFF: {
    DASHBOARD: '/staff/dashboard',
    AI_CONFIG: '/staff/ai-config',
    PROMPTS: '/staff/prompts',
    QUESTION_BANK: '/staff/question-bank',
    AUDIT_LOGS: '/staff/audit-logs',
    SETTINGS: '/staff/settings',
    NOTIFICATIONS: '/staff/notifications',
  },
  ADMIN: {
    DASHBOARD: '/admin/dashboard',
    ACCOUNTS: '/admin/accounts',
    COMPANIES: '/admin/companies',
    MASTER_DATA: '/admin/master-data',
    SUBSCRIPTIONS: '/admin/subscriptions',
    FINANCE: '/admin/finance',
    AUDIT_LOGS: '/admin/audit-logs',
    SETTINGS: '/admin/settings',
    NOTIFICATIONS: '/admin/notifications',
  },
} as const;

// getDashboardPath — helper cho sau khi login
export function getDashboardPath(role?: string): string {
  switch (role?.toLowerCase()) {
    case 'admin':
      return APP_ROUTES.ADMIN.DASHBOARD;
    case 'recruiter':
      return APP_ROUTES.RECRUITER.DASHBOARD;
    case 'staff':
      return APP_ROUTES.STAFF.DASHBOARD;
    case 'candidate':
    default:
      return APP_ROUTES.CANDIDATE.DASHBOARD;
  }
}

// ROLE_MENUS — mapping role → nav items (dùng trong Sidebar)
export const ROLE_MENUS: Record<string, Array<{ label: string; href: string; icon: string }>> = {
  candidate: [
    { label: 'Dashboard', href: APP_ROUTES.CANDIDATE.DASHBOARD, icon: 'LayoutDashboard' },
    { label: 'My Profile', href: APP_ROUTES.CANDIDATE.PROFILE, icon: 'User' },
    { label: 'Job Listings', href: APP_ROUTES.CANDIDATE.JOBS, icon: 'Briefcase' },
    { label: 'My Resume', href: APP_ROUTES.CANDIDATE.RESUME, icon: 'FileText' },
    { label: 'CV Optimizer', href: APP_ROUTES.CANDIDATE.CV_OPTIMIZER, icon: 'BrainCircuit' },
    { label: 'Applications', href: APP_ROUTES.CANDIDATE.APPLICATIONS, icon: 'ClipboardList' },
    { label: 'Notifications', href: APP_ROUTES.CANDIDATE.NOTIFICATIONS, icon: 'Bell' },
    { label: 'Settings', href: APP_ROUTES.CANDIDATE.SETTINGS, icon: 'Settings' },
  ],
  recruiter: [
    { label: 'Dashboard', href: APP_ROUTES.RECRUITER.DASHBOARD, icon: 'LayoutDashboard' },
    { label: 'Company', href: APP_ROUTES.RECRUITER.COMPANY, icon: 'Building2' },
    { label: 'Job Postings', href: APP_ROUTES.RECRUITER.JOBS, icon: 'Briefcase' },
    { label: 'Analytics', href: APP_ROUTES.RECRUITER.ANALYTICS, icon: 'BarChart3' },
    { label: 'Notifications', href: APP_ROUTES.RECRUITER.NOTIFICATIONS, icon: 'Bell' },
    { label: 'Settings', href: APP_ROUTES.RECRUITER.SETTINGS, icon: 'Settings' },
  ],
  staff: [
    { label: 'Dashboard', href: APP_ROUTES.STAFF.DASHBOARD, icon: 'LayoutDashboard' },
    { label: 'AI Config', href: APP_ROUTES.STAFF.AI_CONFIG, icon: 'BrainCircuit' },
    { label: 'Prompts', href: APP_ROUTES.STAFF.PROMPTS, icon: 'MessageSquare' },
    { label: 'Question Bank', href: APP_ROUTES.STAFF.QUESTION_BANK, icon: 'FileText' },
    { label: 'Audit Logs', href: APP_ROUTES.STAFF.AUDIT_LOGS, icon: 'ClipboardList' },
    { label: 'Notifications', href: APP_ROUTES.STAFF.NOTIFICATIONS, icon: 'Bell' },
    { label: 'Settings', href: APP_ROUTES.STAFF.SETTINGS, icon: 'Settings' },
  ],
  admin: [
    { label: 'Dashboard', href: APP_ROUTES.ADMIN.DASHBOARD, icon: 'LayoutDashboard' },
    { label: 'Accounts', href: APP_ROUTES.ADMIN.ACCOUNTS, icon: 'Users' },
    { label: 'Companies', href: APP_ROUTES.ADMIN.COMPANIES, icon: 'Building2' },
    { label: 'Master Data', href: APP_ROUTES.ADMIN.MASTER_DATA, icon: 'Database' },
    { label: 'Subscriptions', href: APP_ROUTES.ADMIN.SUBSCRIPTIONS, icon: 'CreditCard' },
    { label: 'Finance', href: APP_ROUTES.ADMIN.FINANCE, icon: 'BarChart3' },
    { label: 'Platform Safety', href: APP_ROUTES.ADMIN.AUDIT_LOGS, icon: 'Shield' },
    { label: 'Notifications', href: APP_ROUTES.ADMIN.NOTIFICATIONS, icon: 'Bell' },
    { label: 'Settings', href: APP_ROUTES.ADMIN.SETTINGS, icon: 'Settings' },
  ],
};
