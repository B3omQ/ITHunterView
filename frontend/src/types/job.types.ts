export interface JobSearchQuery {
  keyword?: string;
  location?: string;

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
  jobExpertises?: string[];
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
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
  publishedAt?: string;
  isSaved?: boolean;
  isApplied?: boolean;
  skills?: string[];
}

export interface SavedJobDto {
  jobId: string;
  title: string;
  companyName: string;
  logoUrl?: string;
  location: string;
  salaryText: string;
  parseStatus?: string;
  savedAt: string;
}

export interface JobDetailViewDto {
  id: string;
  title: string;
  companyName: string;
  companyId: string;
  logoUrl: string;
  description: string;
  requirements: string;
  benefits: string;
  incomeText: string;
  workLocationText: string;
  minSalary?: number;
  maxSalary?: number;
  currency: string;
  location: string;
  level?: string;
  workingModel?: string;
  jobExpertise?: string;
  jobDomain?: string[];
  status: string;
  publishedAt?: string;
  isSaved?: boolean;
  isApplied?: boolean;
  skills: string[];
}
