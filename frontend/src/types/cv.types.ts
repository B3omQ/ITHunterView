export interface Cv {
  id: string;
  userId: string;
  fileUrl: string;
  fileName: string;
  fileSize: number | null;
  fileType: string;
  isPrimary: boolean;
  parsedData: string;
  parseStatus?: 'PENDING' | 'PROCESSING' | 'SUCCESS' | 'FAILED';
  parseError?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCvRequest {
  fileUrl: string;
  fileName: string;
  fileSize: number | null;
  fileType: string;
  isPrimary: boolean;
  parsedData: string;
}

export interface MatchJdRequest {
  cvId?: string;
  cvUrl?: string;
  cvText?: string;
  jobId?: string;
  rawJdText?: string;
  cvFileName?: string;
  jdTitle?: string;
}

export interface MatchJdResponse {
  id: string; // JobId for polling
}

export interface MatchHistoryDto {
  jobId: string;
  cvId?: string;
  candidateId?: string;
  cvFileName?: string;
  sourceJobId?: string;
  jdTitle?: string;
  matchScore?: number;
  status: string;
  errorMessage?: string;
  updatedAt: string;
  matchType?: 'AI' | 'Hardcode';
  fileUrl?: string;
}

export interface PagedResult<T> {
  totalCount: number;
  page: number;
  pageSize: number;
  items: T[];
}

export interface MatchingResultDto {
  id: string;
  cvId?: string;
  cvFileName?: string;
  jobId?: string;
  jdTitle?: string;
  status: string;
  errorMessage?: string;
  matchDetails?: string; // The raw JSON string from LLM
}

export interface MatchingOutput {
  mode: "jd_fit" | "cv_quality" | "both";
  jdFit?: {
    score: number;
    result: "Highly Suitable" | "Suitable" | "Partially Suitable" | "Not Suitable";
    killSwitchTriggered: boolean;
    poolACapped: boolean;
    poolA: { score: number; max: number };
    poolB: { score: number; max: number };
    requirementScores: RequirementScore[];
    criticalGaps: CriticalGap[];
    penalties: Penalty[];
    narrative: string;
  };
  cvQuality?: {
    score: number;
    result: "Excellent" | "Good" | "Fair" | "Poor";
    breakdown: any;
    penalties: Penalty[];
  };
  improvements: ImprovementSuggestion[];
  processingTime: number;
}

export type RequirementCategory =
  | "tech_skill"
  | "experience"
  | "seniority_fit"
  | "domain_knowledge"
  | "language"
  | "education"
  | "soft_skill";

export interface RequirementScore {
  reqId: string;
  normalizedText: string;
  importance: "must_have" | "nice_to_have";
  category: RequirementCategory;
  categoryWeight: number;
  entities: any;
  handlerUsed: string;
  handlerCode: string;
  handlerScore: number;
  reasoning: string;
  confidence: "high" | "medium" | "low";
  flag: "CRITICAL_GAP" | null;
}

export interface CriticalGap {
  requirement: string;
  gapDescription: string;
  severity: "high" | "medium";
  suggestion: string;
}

export interface Penalty {
  code: string;
  triggered: boolean;
  deduction: number;
  evidence: string;
}

export interface ImprovementSuggestion {
  priority: "high" | "medium" | "low";
  category: string;
  issue: string;
  action: string;
  example?: {
    before: string;
    after: string;
  };
}
