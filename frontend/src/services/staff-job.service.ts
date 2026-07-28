import api from './api-client';
import { JobPostingSummary } from './recruiter.service';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface BaseResponse<T> {
  success: boolean;
  message?: string;
  data: T;
  errors?: string[];
}

export const staffJobService = {
  getJobs: async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    status?: string
  ): Promise<BaseResponse<PagedResult<JobPostingSummary>>> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    
    if (search) params.append('search', search);
    if (status && status !== 'ALL') params.append('status', status);

    const res = await api.get<BaseResponse<PagedResult<JobPostingSummary>>>(`/api/staff/job-postings?${params.toString()}`);
    return res.data;
  },

  banJob: async (id: string, reason: string): Promise<BaseResponse<boolean>> => {
    const res = await api.post<BaseResponse<boolean>>(`/api/staff/job-postings/${id}/ban`, { reason });
    return res.data;
  },

  unbanJob: async (id: string): Promise<BaseResponse<boolean>> => {
    const res = await api.post<BaseResponse<boolean>>(`/api/staff/job-postings/${id}/unban`);
    return res.data;
  }
};
