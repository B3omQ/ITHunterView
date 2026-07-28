import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { jobAnalysisService } from '@/services/job-analysis.service'
import { shouldPollJobAnalysis } from '@/lib/job-analysis-lifecycle'
import type {
  AnalyzeJobRequest,
  FinalizeJobRequest,
} from '@/types/job-analysis.types'

export const jobAnalysisKeys = {
  all: ['job-analysis'] as const,
  detail: (jobId: string) => [...jobAnalysisKeys.all, jobId] as const,
}

export function useJobAnalysis(jobId: string) {
  return useQuery({
    queryKey: jobAnalysisKeys.detail(jobId),
    queryFn: async () => {
      const res = await jobAnalysisService.getPreview(jobId)
      return res.data
    },
    enabled: Boolean(jobId),
    refetchInterval: (query) => {
      return shouldPollJobAnalysis(query.state.data) ? 2500 : false
    },
  })
}

function invalidateAnalysisState(queryClient: ReturnType<typeof useQueryClient>, jobId: string) {
  void queryClient.invalidateQueries({ queryKey: jobAnalysisKeys.detail(jobId) })
  void queryClient.invalidateQueries({ queryKey: ['recruiter-jobs'] })
}

export function useRequestJobAnalysis(jobId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: AnalyzeJobRequest) =>
      jobAnalysisService.requestAnalysis(jobId, payload),
    onSuccess: () => {
      invalidateAnalysisState(queryClient, jobId)
    },
  })
}

export function useRetryJobAnalysis(jobId: string, runId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: AnalyzeJobRequest) =>
      jobAnalysisService.retryAnalysis(jobId, runId, payload),
    onSuccess: () => {
      invalidateAnalysisState(queryClient, jobId)
    },
  })
}

export function useFinalizeJob(jobId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: FinalizeJobRequest) =>
      jobAnalysisService.finalize(jobId, payload),
    onSuccess: () => {
      invalidateAnalysisState(queryClient, jobId)
    },
  })
}
