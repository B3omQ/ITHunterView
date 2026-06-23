import api from './api-client';
import type { ApiResponse, PaginatedDataResponse } from '@/types/api.types';
import type { JobSearchQuery, JobCardDto, SavedJobDto, JobDetailViewDto } from '@/types/job.types';

export const jobService = {
  // Public APIs
  getPublicJobs: (query?: JobSearchQuery) =>
    api
      .get<PaginatedDataResponse<JobCardDto>>('/api/public/jobs', { params: query })
      .then((r) => r.data),

  getPublicJobDetail: (id: string) =>
    api
      .get<ApiResponse<JobDetailViewDto>>(`/api/public/jobs/${id}`)
      .then((r) => r.data),

  // Candidate APIs
  getCandidateJobs: (query?: JobSearchQuery) =>
    api
      .get<PaginatedDataResponse<JobCardDto>>('/api/candidate/jobs', { params: query })
      .then((r) => r.data),

  getCandidateJobDetail: (id: string) =>
    api
      .get<ApiResponse<JobDetailViewDto>>(`/api/candidate/jobs/${id}`)
      .then((r) => r.data),

  getSavedJobs: (page = 1, pageSize = 10) =>
    api
      .get<PaginatedDataResponse<SavedJobDto>>('/api/candidate/saved-jobs', {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  saveJob: (jobId: string) =>
    api
      .post<ApiResponse<void>>('/api/candidate/saved-jobs', { jobId })
      .then((r) => r.data),

  unsaveJob: (jobId: string) =>
    api
      .delete<void>(`/api/candidate/saved-jobs/${jobId}`)
      .then((r) => r.data),
};
