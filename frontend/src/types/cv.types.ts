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
  scorePercent: number;
  reportKind: MatchReportKind;
  matchMethod: MatchMethodCode;
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
  /** @deprecated Compatibility only. New UI code must use report. */
  matchDetails?: string;
  scorePercent?: number;
  reportKind?: MatchReportKind;
  matchMethod?: MatchMethodCode;
  report?: MatchReport | null;
  cvAnalysis?: CvAnalysisResult | null;
}

export type MatchReportKind = 'structured' | 'raw_text_fallback' | 'legacy_summary';
export type MatchMethodCode =
  | 'one_to_one_ai'
  | 'hardcode'
  | 'vector'
  | 'raw_text_ai'
  | 'legacy_unknown';

export interface MatchEvidenceReport {
  quotation: string;
  section?: string | null;
}

export interface MatchRequirementItemReport {
  itemId?: string | null;
  normalizedText?: string | null;
  detailVerbatim?: string | null;
  rawMention?: string | null;
  category?: string | null;
  score: number;
  handlerCode?: string | null;
  reasoning: string;
  evidence: MatchEvidenceReport[];
  isCriticalGap: boolean;
  sourceOrder?: number | null;
}

export interface MatchRequirementGroupReport {
  groupId?: string | null;
  sourceRequirementId?: string | null;
  intent?: string | null;
  operator?: string | null;
  minSatisfied?: number | null;
  importance?: string | null;
  sourceSection?: string | null;
  requirementVerbatim?: string | null;
  groupScore: number;
  selectedItemIds: string[];
  isCriticalGap: boolean;
  sourceOrder?: number | null;
  items: MatchRequirementItemReport[];
}

export interface MatchCriticalGapReport {
  code: string;
  scope?: string | null;
  groupId?: string | null;
  itemId?: string | null;
  requirement: string;
  reasoning: string;
  evidence: MatchEvidenceReport[];
}

export interface MatchReport {
  reportKind: MatchReportKind;
  schemaVersion?: string | null;
  matchMethod: MatchMethodCode;
  scorePercent: number;
  resultCode?: string | null;
  resultLabel?: string | null;
  narrative: string;
  requirementGroups: MatchRequirementGroupReport[];
  criticalGaps: MatchCriticalGapReport[];
  warningFlags: string[];
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
