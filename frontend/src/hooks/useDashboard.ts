import { useQuery } from '@tanstack/react-query';
import { dashboardService } from '@/services/dashboard.service';
import { DashboardFilter } from '@/types/dashboard.types';

export function useAdminDashboard(filters?: DashboardFilter) {
  return useQuery({
    queryKey: ['admin-dashboard', filters],
    queryFn: () => dashboardService.getAdminDashboard(filters),
  });
}

export function useStaffDashboard(filters?: DashboardFilter) {
  return useQuery({
    queryKey: ['staff-dashboard', filters],
    queryFn: () => dashboardService.getStaffDashboard(filters),
  });
}

export function useRecruiterDashboard(filters?: DashboardFilter) {
  return useQuery({
    queryKey: ['recruiter-dashboard', filters],
    queryFn: () => dashboardService.getRecruiterDashboard(filters),
  });
}
