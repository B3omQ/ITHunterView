export type SubscriptionStatus = 'ACTIVE' | 'INACTIVE';

export interface FeaturesConfigDto {
  role: 'CANDIDATE' | 'RECRUITER';
  // Quota cho Candidate
  cvMatchLimit?: number | null;
  mockInterviewLimit?: number | null;
  cvOptimizeLimit?: number | null;
  // Quota cho Recruiter
  activeJobPostings?: number | null;
  activeSourcingLimit?: number | null;
  highlightedJobs?: number | null;
  analytics?: boolean | null;
}

export interface SubscriptionDto {
  id: number;
  name: string;
  price: number;
  durationDays: number;
  featuresConfig: FeaturesConfigDto;
  status: SubscriptionStatus;
  createdBy: string;
  updatedBy: string | null;
  createdAt: string;
  updatedAt: string | null;
  isUsed: boolean;
}

export interface CreateSubscriptionDto {
  name: string;
  price: number;
  durationDays: number;
  featuresConfig: FeaturesConfigDto;
}

export interface UpdateSubscriptionDto {
  name: string;
  price: number;
  durationDays: number;
  featuresConfig: FeaturesConfigDto;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  totalItems: number;
  page: number;
  pageSize: number;
}

export interface CoinFeatureCostsDto {
  cvJdMatching: number;
  mockInterview: number;
  cvOptimize: number;
}

export interface CoinPackageDto {
  id: string;
  name: string;
  coins: number;
  price: number;
  isActive: boolean;
}

export interface UpdateCoinConfigDto {
  featureCosts: CoinFeatureCostsDto;
  packages: CoinPackageDto[];
}
