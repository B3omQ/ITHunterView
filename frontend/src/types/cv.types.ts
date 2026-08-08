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
  analysisQuality?: CvAnalysisQuality | null;
  analysisCoverage?: CvAnalysisCoverage | null;
  analysisWarningCodes?: string[];
  warningMessage?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCvRequest {
  fileUrl: string;
  fileName: string;
  fileSize?: number;
  fileType: string;
  isPrimary: boolean;
  isTemporary?: boolean;
}

export interface MatchJdRequest {
  cvId?: string;
  cvText?: string;
  jobId?: string;
  rawJdText?: string;
  cvFileName?: string;
  jdTitle?: string;
}

export interface MatchJdResponse {
  id: string; // JobId for polling
}

export type JdAnalysisQuality = 'COMPLETE' | 'PARTIAL' | 'INVALID';
export type CvAnalysisQuality = 'COMPLETE' | 'PARTIAL' | 'INVALID';

export interface CvAnalysisCoverage {
  inputExperienceEntryCount: number;
  acceptedExperienceEntryCount: number;
  discardedExperienceEntryCount: number;
  inputRequirementSignalCount: number;
  acceptedRequirementSignalCount: number;
  discardedRequirementSignalCount: number;
  inputExperiencePeriodCount: number;
  acceptedExperiencePeriodCount: number;
  discardedExperiencePeriodCount: number;
  titleMetricsAvailable: boolean;
  skillMetricsAvailable: boolean;
  experienceMetricAvailable: boolean;
  domainMetricsAvailable: boolean;
}

export interface CvAnalysisResult {
  quality: CvAnalysisQuality;
  scoreBasis: string;
  coverage?: CvAnalysisCoverage | null;
  warningCodes: string[];
}

export interface JdAnalysisCoverage {
  inputGroupCount: number;
  acceptedGroupCount: number;
  discardedGroupCount: number;
  inputItemCount: number;
  acceptedItemCount: number;
  discardedItemCount: number;
  requirementSetComplete: boolean;
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
  jdAnalysisQuality?: JdAnalysisQuality | null;
  jdAnalysisScoreBasis?: string | null;
  jdAnalysisCoverage?: JdAnalysisCoverage | null;
  cvAnalysisQuality?: CvAnalysisQuality | null;
  cvAnalysisScoreBasis?: string | null;
  cvAnalysisCoverage?: CvAnalysisCoverage | null;
  fileUrl?: string;
  isUnlocked?: boolean;
  unlockCost?: number;
}

export interface UnlockCandidateResponse {
  success: boolean;
  message: string;
  unlockedVia: 'SUBSCRIPTION' | 'COINS';
  coinsDeducted: number;
  remainingCoins: number;
  cvId?: string;
  candidateId?: string;
  cvFileName?: string;
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
  errorCode?: string;
  errorMessage?: string;
  canRetry: boolean;
  matchDetails?: string; // The raw JSON string from LLM
  cvAnalysis?: CvAnalysisResult | null;
}

export interface MatchingOutput {
  mode: "jd_fit" | "cv_quality" | "both";
  contract?: string;
  sourceJdSchemaVersion?: string;
  jdAnalysis?: {
    quality: JdAnalysisQuality;
    scoreBasis: string;
    requirementSetComplete: boolean;
    coverage?: JdAnalysisCoverage | null;
    warningCodes: string[];
  };
  jdFit?: {
    score: number;
    result: "Highly Suitable" | "Suitable" | "Partially Suitable" | "Not Suitable";
    killSwitchTriggered: boolean;
    poolACapped: boolean;
    poolA: { score: number | null; max: number | null };
    poolB: { score: number | null; max: number | null };
    requirementScores: RequirementScore[];
    criticalGaps: CriticalGap[];
    penalties: Penalty[];
    narrative: string;
  };
  cvQuality?: {
    score: number;
    result: "Excellent" | "Good" | "Fair" | "Poor";
    breakdown: Record<string, unknown>;
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

export interface RequirementEntities {
  skill_name?: string;
  [key: string]: unknown;
}

export interface RequirementScore {
  reqId: string;
  normalizedText: string;
  importance: "must_have" | "nice_to_have";
  category: RequirementCategory;
  categoryWeight: number;
  entities: RequirementEntities;
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
