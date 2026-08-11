export type SubscriptionStatus = 'ACTIVE' | 'INACTIVE';

export interface FeaturesConfigDto {
  role: 'CANDIDATE' | 'RECRUITER';
  // Quota cho Candidate
  cvMatchLimit?: number | null;
  cvOptimizeLimit?: number | null;
  mockInterviewLimit?: number | null;
  learningPathLimit?: number | null;
  learningPathSlotLimit?: number | null;
  // Quota cho Recruiter
  jobSlots?: number | null;
  jobExtendLimit?: number | null;
  unlockCvLimit?: number | null;
  pushTopLimit?: number | null;
  // Dùng chung
  coinCredit?: number | null;
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
  cvOptimize: number;
  mockInterview: number;
  learningPath: number;
  unlockCv: number;
  postJob: number;
  extendJob: number;
  pushTop: number;
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
