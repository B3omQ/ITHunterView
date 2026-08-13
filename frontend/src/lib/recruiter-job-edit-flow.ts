import type { JobAnalysisLifecycleState } from "@/types/job-analysis.types"

export type EditableJobStatus = "DRAFT" | "PUBLISHED" | "CLOSED" | "PENDING_REVIEW"

export function canSaveJobAsDraft(status: EditableJobStatus): boolean {
  return status === "DRAFT"
}

export function getJobPreviewRoute(jobId: string, requiresAnalysis: boolean): string {
  return `/recruiter/jobs/${jobId}/preview${requiresAnalysis ? "?publish=1" : ""}`
}

export function shouldAutoRequestJobAnalysis(
  requiresAnalysis: boolean,
  lifecycle: JobAnalysisLifecycleState,
): boolean {
  return requiresAnalysis && (lifecycle === "NOT_REQUESTED" || lifecycle === "STALE")
}

export function canFinalizePublishedJobWithoutAnalysis(
  status: EditableJobStatus,
  requiresAnalysis: boolean,
): boolean {
  return status === "PUBLISHED" && !requiresAnalysis
}
