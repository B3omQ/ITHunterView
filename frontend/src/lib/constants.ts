export const APP_ROUTES = {
  LOGIN: '/login',
  REGISTER: '/register',
  FORGOT_PASSWORD: '/forgot-password',
};

export function getDashboardPath(role?: string): string {
  switch (role?.toLowerCase()) {
    case "admin": return "/admin/dashboard";
    case "candidate": return "/candidate/dashboard";
    case "recruiter": return "/recruiter/dashboard";
    case "staff": return "/staff/dashboard";
    default: return "/candidate/dashboard";
  }
}
