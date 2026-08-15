import api from '@/services/api-client';

export interface JobSkillRequirement {
  skillId: number;
  skillName: string;
  isMandatory: boolean;
}

export interface JobPostingSummary {
  id: string;
  jobCode: string;
  title: string;
  location: string;
  status: 'DRAFT' | 'PUBLISHED' | 'CLOSED' | 'PENDING_REVIEW';
  applicationCount: number;
  viewCount: number;
  publishedAt: string | null;
  expiresAt: string | null;
  applicationDeadline: string | null;
  createdAt: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
  skills: string[];
  parseStatus?: 'PENDING' | 'PROCESSING' | 'READY' | 'SUCCESS' | 'FAILED' | 'NOT_REQUESTED' | 'STALE';
  parseError?: string | null;
  isBanned?: boolean;
  banReason?: string;
  pushedTopUntil?: string;
  analysisRevision: number;
}

export interface JobPosting {
  id: string;
  jobCode: string;
  recruiterId: string;
  companyId: string;
  title: string;
  description: string;
  requirements: string;
  benefits: string;
  incomeText: string;
  workLocationText: string;
  minSalary: number | null;
  maxSalary: number | null;
  currency: string;
  location: string;
  status: 'DRAFT' | 'PUBLISHED' | 'CLOSED' | 'PENDING_REVIEW';
  applicationCount: number;
  viewCount: number;
  publishedAt: string | null;
  expiresAt: string | null;
  applicationDeadline: string | null;
  createdAt: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
  skills: JobSkillRequirement[];
  parseStatus?: 'PENDING' | 'PROCESSING' | 'READY' | 'SUCCESS' | 'FAILED' | 'NOT_REQUESTED' | 'STALE';
  parseError?: string | null;
  analysisRevision: number;
  requiresAnalysis: boolean;
  isBanned?: boolean;
  banReason?: string;
  pushedTopUntil?: string;
}

export interface JobCategory {
  id: number;
  name: string;
  description?: string;
  parentId?: number | null;
}

export interface Skill {
  id: number;
  name: string;
  categoryName?: string;
}

export interface CreateJobPostingDto {
  jobCode: string;
  title: string;
  location: string;
  workLocationText: string;
  minSalary: number | null;
  maxSalary: number | null;
  currency: string;
  applicationDeadline?: string | null;

  description: string;
  requirements: string;
  benefits: string;
  incomeText: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
}

export interface UpdateJobPostingDto {
  jobCode: string;
  title: string;
  location: string;
  workLocationText: string;
  minSalary: number | null;
  maxSalary: number | null;
  currency: string;
  applicationDeadline?: string | null;

  description: string;
  requirements: string;
  benefits: string;
  incomeText: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
}

export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data: T;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const recruiterService = {
  getJobs: async (page: number, pageSize: number, status?: string, search?: string) => {
    try {
      const statusParam = status && status !== 'ALL' ? `&status=${status}` : '';
      const searchParam = search ? `&search=${encodeURIComponent(search)}` : '';
      const response = await api.get<ApiResponse<PaginatedResult<JobPostingSummary>>>(
        `/api/jobpostings?page=${page}&pageSize=${pageSize}${statusParam}${searchParam}`
      );
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch jobs',
      };
    }
  },

  getJobById: async (id: string) => {
    try {
      const response = await api.get<ApiResponse<JobPosting>>(`/api/jobpostings/${id}`);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch job details',
      };
    }
  },

  createJob: async (payload: CreateJobPostingDto) => {
    try {
      const response = await api.post<ApiResponse<JobPosting>>('/api/jobpostings', payload);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to create job posting',
      };
    }
  },

  updateJob: async (id: string, payload: UpdateJobPostingDto) => {
    try {
      const response = await api.put<ApiResponse<JobPosting>>(`/api/jobpostings/${id}`, payload);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to update job posting',
      };
    }
  },

  closeJob: async (id: string) => {
    try {
      const response = await api.patch<ApiResponse<boolean>>(`/api/jobpostings/${id}/close`);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to close job posting',
      };
    }
  },

  extendJob: async (id: string) => {
    try {
      const response = await api.post<ApiResponse<any>>(`/api/jobpostings/${id}/extend`);
      return { success: true, data: response.data, message: response.data?.message || 'Gia hạn thành công' };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Gia hạn thất bại',
      };
    }
  },

  pushTopJob: async (id: string) => {
    try {
      const response = await api.post<ApiResponse<any>>(`/api/jobpostings/${id}/push-top`);
      return { success: true, data: response.data, message: response.data?.message || 'Đẩy Top thành công!' };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Đẩy Top thất bại',
      };
    }
  },

  getCategories: async () => {
    try {
      const response = await api.get<ApiResponse<JobCategory[]>>('/api/jobcategories');
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch categories',
      };
    }
  },

  getSkills: async () => {
    try {
      const response = await api.get<ApiResponse<Skill[]>>('/api/skills');
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch skills',
      };
    }
  },

  createSkill: async (name: string, categoryId = 1) => {
    try {
      const response = await api.post<ApiResponse<Skill>>('/api/skills', { name, categoryId });
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to create skill',
      };
    }
  },

  getCandidateProfile: async (id: string) => {
    try {
      const response = await api.get<ApiResponse<any>>(`/api/v1/recruiter/candidates/${id}/profile`);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch candidate profile',
      };
    }
  },

  getMajors: async () => {
    try {
      const response = await api.get<ApiResponse<PaginatedResult<any>>>('/api/majors');
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch majors',
      };
    }
  },


  getJobMatches: async (jobId: string, page = 1, pageSize = 10) => {
    try {
      const response = await api.get<ApiResponse<PaginatedResult<import('@/types/cv.types').MatchHistoryDto>>>(`/api/jobpostings/${jobId}/matches?page=${page}&pageSize=${pageSize}`);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch job matches',
      };
    }
  },

  matchJobWithCvs: async (jobId: string) => {
    try {
      const response = await api.post<ApiResponse<any>>(`/api/jobpostings/${jobId}/match-cvs`);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to match job with CVs',
      };
    }
  },

  matchJobWithCvsHardcode: async (jobId: string) => {
    try {
      const response = await api.post<ApiResponse<any>>(`/api/jobpostings/${jobId}/match-cvs-hardcode`);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to match job with CVs using hardcode',
      };
    }
  },

  unlockCandidateCv: async (cvId: string, jobId?: string) => {
    try {
      const response = await api.post<ApiResponse<import('@/types/cv.types').UnlockCandidateResponse>>('/api/jobpostings/unlock-candidate', { cvId, jobId });
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Lỗi mở khóa hồ sơ ứng viên',
      };
    }
  }
};

