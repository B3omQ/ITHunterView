export interface JobSearchQuery {
  keyword?: string;
  location?: string;
  jobType?: string;
  categoryId?: number;
  minSalary?: number;
  currency?: string;
  skill?: string;
  companyName?: string;
  postedWithinDays?: number;
  status?: string;
  levels?: string[];
  workingModels?: string[];
  jobDomains?: string[];
  companyIndustries?: string[];
  companyTypes?: string[];
  maxSalary?: number;
  page?: number;
  pageSize?: number;
}

export interface JobCardDto {
  id: string;
  title: string;
  companyName: string;
  logoUrl: string;
  minSalary?: number;
  maxSalary?: number;
  currency: string;
  location: string;
  jobType: string;
  publishedAt?: string;
  isSaved?: boolean;
  skills?: string[];
}

export interface SavedJobDto {
  jobId: string;
  title: string;
  companyName: string;
  logoUrl: string;
  location: string;
  salaryText: string;
  savedAt: string;
}

export interface JobDetailViewDto {
  id: string;
  title: string;
  companyName: string;
  companyId: string;
  logoUrl: string;
  description: string;
  responsibilities: string;
  requirements: string;
  benefits: string;
  minSalary?: number;
  maxSalary?: number;
  currency: string;
  location: string;
  jobType: string;
  status: string;
  publishedAt?: string;
  isSaved?: boolean;
  skills: string[];
}
