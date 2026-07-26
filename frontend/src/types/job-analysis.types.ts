export type JobAnalysisStatus = 'PENDING' | 'PROCESSING' | 'READY' | 'FAILED' | 'SUPERSEDED'
export type SkillResolutionStatus = 'EXACT_CANONICAL' | 'EXACT_ALIAS' | 'AMBIGUOUS' | 'UNMATCHED' | 'MANUAL'
export type SkillDecisionStatus = 'PENDING' | 'ACCEPTED' | 'REJECTED'

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
}

export interface JobAnalysisStatusDto {
  jobId: string
  analysisRunId: string
  inputRevision: number
  status: JobAnalysisStatus
  failureCode?: string | null
  message?: string | null
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
  analysisRunId: string
  inputRevision: number
  status: JobAnalysisStatus
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
  jobId: string
  status: string
  publishedAt?: string | null
  skillCount: number
}
