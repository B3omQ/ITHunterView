import { describe, expect, it } from "vitest"
import {
  canFinalizePublishedJobWithoutAnalysis,
  canSaveJobAsDraft,
  getJobPreviewRoute,
  shouldAutoRequestJobAnalysis,
} from "./recruiter-job-edit-flow"

describe("recruiter published job edit flow", () => {
  it("keeps Save Draft for drafts and hides it for published jobs", () => {
    expect(canSaveJobAsDraft("DRAFT")).toBe(true)
    expect(canSaveJobAsDraft("PUBLISHED")).toBe(false)
  })

  it("routes to preview and only adds the AI trigger from the backend contract", () => {
    expect(getJobPreviewRoute("job-1", true)).toBe("/recruiter/jobs/job-1/preview?publish=1")
    expect(getJobPreviewRoute("job-1", false)).toBe("/recruiter/jobs/job-1/preview")
  })

  it("only auto-starts AI when both the backend contract and lifecycle require it", () => {
    expect(shouldAutoRequestJobAnalysis(true, "STALE")).toBe(true)
    expect(shouldAutoRequestJobAnalysis(true, "NOT_REQUESTED")).toBe(true)
    expect(shouldAutoRequestJobAnalysis(false, "STALE")).toBe(false)
    expect(shouldAutoRequestJobAnalysis(true, "READY")).toBe(false)
  })

  it("allows a published job with no pending analysis to complete preview directly", () => {
    expect(canFinalizePublishedJobWithoutAnalysis("PUBLISHED", false)).toBe(true)
    expect(canFinalizePublishedJobWithoutAnalysis("PUBLISHED", true)).toBe(false)
    expect(canFinalizePublishedJobWithoutAnalysis("DRAFT", false)).toBe(false)
  })
})
