import api from './api-client';

export interface JobSkill {
  skillId: number;
  skillName?: string;
  name?: string;
  isMandatory: boolean;
}

export interface JobPosting {
  id: string;
  jobCode: string;
  title: string;
  location: string;
  status: string;
  minSalary: number | null;
  maxSalary: number | null;
  currency: string;
  description: string;
  responsibilities?: string;
  requirements?: string;
  benefits?: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
  skills?: JobSkill[];
  createdAt: string;
  publishedAt?: string;
  expiresAt?: string;
  applicationCount?: number;
}

export interface JobPostingSummary {
  id: string;
  jobCode: string;
  title: string;
  location: string;

  status: string;
  applicationCount: number;
  viewCount: number;
  publishedAt?: string;
  expiresAt?: string;
  createdAt: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
  skills: string[];
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
  status: string;
  minSalary: number | null;
  maxSalary: number | null;
  currency: string;
  description: string;
  responsibilities?: string;
  requirements?: string;
  benefits?: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
  skills: { skillId: number; isMandatory: boolean }[];
}

export interface UpdateJobPostingDto extends CreateJobPostingDto {}

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
      const response = await api.patch<ApiResponse<any>>(`/api/jobpostings/${id}/close`);
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to close job posting',
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
        message: error.response?.data?.message || error.message || 'Failed to fetch job categories',
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

  createSkill: async (name: string, categoryId: number = 1) => {
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

  getMajors: async () => {
    try {
      const response = await api.get<ApiResponse<PaginatedResult<{ id: number; name: string }>>>('/api/majors?pageSize=1000');
      return { success: true, data: response.data };
    } catch (error: any) {
      return {
        success: false,
        message: error.response?.data?.message || error.message || 'Failed to fetch majors',
      };
    }
  },
};
