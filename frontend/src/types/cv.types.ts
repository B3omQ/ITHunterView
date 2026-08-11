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
  matchType?: 'AI' | 'Hardcode' | 'Vector';
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
  satisfiedItemIds?: string[];
  isCriticalGap: boolean;
  sourceOrder?: number | null;
  items: MatchRequirementItemReport[];
}

export interface MatchCriticalGapReport {
  code: string;
  scope?: string | null;
  groupId?: string | null;
  itemId?: string | null;
  operator?: string | null;
  requiredCount?: number | null;
  satisfiedCount?: number | null;
  affectedItemIds?: string[];
  requirement: string;
  reasoning: string;
  evidence: MatchEvidenceReport[];
}

export interface MatchReport {
  reportContract?: 'match-report/v2';
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
