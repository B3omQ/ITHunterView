import type {
  JobAnalysisLifecycleState,
  JobAnalysisPreviewDto,
} from '@/types/job-analysis.types'

const LIFECYCLE_STATES: ReadonlySet<string> = new Set([
  'NOT_REQUESTED',
  'PENDING',
  'PROCESSING',
  'READY',
  'FAILED',
  'STALE',
])

/**
 * Reads the explicit lifecycle returned by the current API and retains a
 * conservative fallback for an older additive API response during rollout.
 * A no-run response must never be inferred as PENDING from its default
 * run-status enum value.
 */
export function getEffectiveJobAnalysisLifecycle(
  preview: Partial<JobAnalysisPreviewDto> | null | undefined,
): JobAnalysisLifecycleState {
  if (preview?.lifecycleState && LIFECYCLE_STATES.has(preview.lifecycleState)) {
    return preview.lifecycleState
  }

  if (!preview?.hasAnalysisRun) {
    return 'NOT_REQUESTED'
  }

  switch (preview.status) {
    case 'PENDING':
    case 'PROCESSING':
    case 'READY':
    case 'FAILED':
      return preview.status
    default:
      return 'STALE'
  }
}

export function isCurrentJobAnalysis(
  preview: Partial<JobAnalysisPreviewDto> | null | undefined,
  jobRevision?: number,
): boolean {
  if (!preview?.hasAnalysisRun || !preview.analysisRunId) {
    return false
  }

  if (typeof preview.isCurrentAnalysis === 'boolean') {
    return preview.isCurrentAnalysis
  }

  const currentRevision = preview.currentJobRevision ?? jobRevision
  return currentRevision !== undefined && preview.inputRevision === currentRevision
}

export function shouldPollJobAnalysis(
  preview: Partial<JobAnalysisPreviewDto> | null | undefined,
  jobRevision?: number,
): boolean {
  const lifecycle = getEffectiveJobAnalysisLifecycle(preview)
  return (
    preview?.hasAnalysisRun === true &&
    isCurrentJobAnalysis(preview, jobRevision) &&
    (lifecycle === 'PENDING' || lifecycle === 'PROCESSING')
  )
}
