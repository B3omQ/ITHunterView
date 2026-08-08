export type JobAnalysisStatus = 'PENDING' | 'PROCESSING' | 'READY' | 'FAILED' | 'SUPERSEDED'
export type JobAnalysisLifecycleState =
  | 'NOT_REQUESTED'
  | 'PENDING'
  | 'PROCESSING'
  | 'READY'
  | 'FAILED'
  | 'STALE'
export type SkillResolutionStatus = 'EXACT_CANONICAL' | 'EXACT_ALIAS' | 'AMBIGUOUS' | 'UNMATCHED' | 'MANUAL'
export type SkillDecisionStatus = 'PENDING' | 'ACCEPTED' | 'REJECTED'
export type JdAnalysisQuality = 'COMPLETE' | 'PARTIAL' | 'INVALID'

export interface JdAnalysisCoverage {
  inputGroupCount: number
  acceptedGroupCount: number
  discardedGroupCount: number
  inputItemCount: number
  acceptedItemCount: number
  discardedItemCount: number
  requirementSetComplete: boolean
}

export interface JdAnalysisDiagnostic {
  code: string
  jsonPath: string
}

export interface AnalyzeJobRequest {
  expectedRevision: number
  idempotencyKey?: string
}

export interface JobSkillDecisionInput {
  decisionId: string
  decision: SkillDecisionStatus
  resolvedSkillId?: number | null
  importance: string
}

export interface UpdateDecisionsRequest {
  expectedJobRevision: number
  expectedDecisionVersion: number
  decisions: JobSkillDecisionInput[]
}

export interface FinalizeJobRequest {
  analysisRunId: string
  expectedJobRevision: number
  expectedDecisionVersion: number
  confirmNoStandardSkills?: boolean
}

export interface JobAnalysisStatusDto {
  jobId: string
  analysisRunId: string
  inputRevision: number
  currentJobRevision: number
  status: JobAnalysisStatus
  analysisQuality?: JdAnalysisQuality | null
  analysisCoverage?: JdAnalysisCoverage | null
  analysisDiagnostics?: JdAnalysisDiagnostic[]
  usesRawTextFallback?: boolean
  failureCode?: string | null
  message?: string | null
  isReused: boolean
  isQueued: boolean
  createdAt: string
  completedAt?: string | null
}

export interface JobSkillDecisionDto {
  id: string
  rawMention: string
  normalizedMention: string
  category: string
  importance: string
  sourceSection: string
  evidenceText: string
  suggestedSkillId?: number | null
  suggestedSkillName?: string | null
  resolvedSkillId?: number | null
  resolvedSkillName?: string | null
  resolutionStatus: SkillResolutionStatus
  decisionStatus: SkillDecisionStatus
  confidence?: number | null
}

export interface OtherRequirementDto {
  category: string
  importance: string
  skillName: string
  detailVerbatim: string
  evidence: string
}

export interface JobAnalysisPreviewDto {
  jobId: string
  hasAnalysisRun: boolean
  analysisRunId: string
  inputRevision: number
  currentJobRevision: number
  lifecycleState: JobAnalysisLifecycleState
  isCurrentAnalysis: boolean
  status: JobAnalysisStatus
  analysisQuality?: JdAnalysisQuality | null
  analysisCoverage?: JdAnalysisCoverage | null
  analysisDiagnostics?: JdAnalysisDiagnostic[]
  usesRawTextFallback?: boolean
  decisionVersion: number
  failureCode?: string | null
  suggestions: JobSkillDecisionDto[]
  otherRequirements: OtherRequirementDto[]
  canFinalize: boolean
  blockingReasons: string[]

  finalActionLabel: string
  finalTargetStatus: string
}

export interface FinalizeJobResponseDto {
  success: boolean
  message: string
  jobId: string
  status: string
  publishedAt?: string | null
  skillCount: number
}
