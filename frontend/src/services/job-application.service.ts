import api from './api-client';
import { ApiResponse, PaginatedResponse } from '@/types/api.types';
import { ApplicantDto } from '@/types/job-application.types';

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
};
