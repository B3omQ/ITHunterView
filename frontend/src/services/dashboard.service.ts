import api from './api-client';
import { ApiResponse } from '@/types/api.types';
import { 
  AdminDashboardResponse, 
  StaffDashboardResponse, 
  RecruiterDashboardResponse, 
  DashboardFilter 
} from '@/types/dashboard.types';

export const dashboardService = {
  getAdminDashboard: (params?: DashboardFilter) => 
    api.get<ApiResponse<AdminDashboardResponse>>('/api/admin/dashboard', { params }).then(r => r.data.data),
    
  getStaffDashboard: (params?: DashboardFilter) => 
    api.get<ApiResponse<StaffDashboardResponse>>('/api/staff/dashboard', { params }).then(r => r.data.data),
    
  getRecruiterDashboard: (params?: DashboardFilter) => 
    api.get<ApiResponse<RecruiterDashboardResponse>>('/api/recruiter/dashboard', { params }).then(r => r.data.data),
};
