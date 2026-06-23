import api from './api-client';
import { ApiResponse, PaginatedResponse } from '@/types/api.types';
import { ApplicantDto, ApplicationStatus, JobApplicationDetailDto } from '@/types/job-application.types';

export const JobApplicationService = {
  getApplicantsByJobId: async (
    jobId: string,
    page: number = 1,
    pageSize: number = 10
  ): Promise<PaginatedResponse<ApplicantDto>> => {
    const response = await api.get<ApiResponse<PaginatedResponse<ApplicantDto>>>(
      `/api/JobApplications/job-posting/${jobId}/applicants`,
      { params: { page, pageSize } }
    );
    return response.data.data as PaginatedResponse<ApplicantDto>;
  },

  applyForJob: async (data: { jobId: string; cvId?: string; coverLetter?: string }): Promise<boolean> => {
    const response = await api.post<ApiResponse<boolean>>('/api/JobApplications/apply', data);
    return response.data.data || false;
  },

  updateApplicationStatus: async (applicationId: string, status: ApplicationStatus): Promise<boolean> => {
    const response = await api.put<ApiResponse<boolean>>(`/api/JobApplications/${applicationId}/status`, { status });
    return response.data.data || false;
  },

  getApplicationDetail: async (applicationId: string): Promise<JobApplicationDetailDto | null> => {
    const response = await api.get<ApiResponse<JobApplicationDetailDto>>(`/api/JobApplications/${applicationId}`);
    return response.data.data || null;
  },
};
