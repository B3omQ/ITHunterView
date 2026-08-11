export interface DashboardFilter {
  year?: number | null;
  month?: number | null;
  fromDate?: string | null;
  toDate?: string | null;
}

export interface AdminDashboardResponse {
  totalRevenue: number;
  revenueGrowthPercentage: number;
  totalUsers: number;
  userGrowthPercentage: number;
  aiTokensUsed: number;
  tokensGrowthPercentage: number;
  transactions: number;
  transactionsGrowthPercentage: number;
  userRevenueGrowth: { month: string; users: number; revenue: number }[];
  tokenUsage: { day: string; tokens: number }[];
  subscriptionBreakdown: { name: string; value: number }[];
}

export interface StaffDashboardResponse {
  totalQuestions: number;
  newQuestions: number;
  pendingCompanies: number;
  auditWarnings: number;
  questionsByCategory: { name: string; value: number }[];
  questionsByLevel: { level: string; count: number }[];
  companyVerifications: { week: string; new: number; verified: number }[];
}

export interface RecruiterDashboardResponse {
  activeJobs: number;
  totalApplications: number;
  dailyApplications: { day: string; apps: number }[];
  applicationStatus: { name: string; value: number }[];
  topJobs: { title: string; applicants: number }[];
}
