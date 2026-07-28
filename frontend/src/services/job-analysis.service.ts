import api from '@/services/api-client'
import type { ApiResponse } from '@/services/recruiter.service'

import type {
  AnalyzeJobRequest,
  FinalizeJobRequest,
  FinalizeJobResponseDto,
  JobAnalysisPreviewDto,
  JobAnalysisStatusDto,
  UpdateDecisionsRequest
} from '@/types/job-analysis.types'

export const jobAnalysisService = {
  requestAnalysis: (jobId: string, payload: AnalyzeJobRequest) =>
    api.post<ApiResponse<JobAnalysisStatusDto>>(`/api/jobpostings/${jobId}/analysis`, payload).then(r => r.data),

  retryAnalysis: (jobId: string, runId: string, payload: AnalyzeJobRequest) =>
    api.post<ApiResponse<JobAnalysisStatusDto>>(`/api/jobpostings/${jobId}/analysis/${runId}/retry`, payload).then(r => r.data),

  getPreview: (jobId: string) =>
    api.get<ApiResponse<JobAnalysisPreviewDto>>(`/api/jobpostings/${jobId}/analysis`).then(r => r.data),

  updateDecisions: (jobId: string, runId: string, payload: UpdateDecisionsRequest) =>
    api.put<ApiResponse<JobAnalysisPreviewDto>>(`/api/jobpostings/${jobId}/analysis/${runId}/decisions`, payload).then(r => r.data),

  finalize: (jobId: string, payload: FinalizeJobRequest) =>
    api.post<ApiResponse<FinalizeJobResponseDto>>(`/api/jobpostings/${jobId}/finalize`, payload).then(r => r.data),
}
