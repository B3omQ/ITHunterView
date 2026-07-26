import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { jobAnalysisService } from '@/services/job-analysis.service'
import type {
  AnalyzeJobRequest,
  FinalizeJobRequest,
  UpdateDecisionsRequest
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
      const status = query.state.data?.status
      if (status === 'PENDING' || status === 'PROCESSING') {
        return 2500
      }
      return false
    },
  })
}

export function useRequestJobAnalysis(jobId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: AnalyzeJobRequest) =>
      jobAnalysisService.requestAnalysis(jobId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: jobAnalysisKeys.detail(jobId) })
    },
  })
}

export function useUpdateJobDecisions(jobId: string, runId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: UpdateDecisionsRequest) =>
      jobAnalysisService.updateDecisions(jobId, runId, payload),
    onSuccess: (res) => {
      if (res.data) {
        queryClient.setQueryData(jobAnalysisKeys.detail(jobId), res.data)
      } else {
        queryClient.invalidateQueries({ queryKey: jobAnalysisKeys.detail(jobId) })
      }
    },
  })
}

export function useFinalizeJob(jobId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: FinalizeJobRequest) =>
      jobAnalysisService.finalize(jobId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: jobAnalysisKeys.detail(jobId) })
      queryClient.invalidateQueries({ queryKey: ['recruiter-jobs'] })
    },
  })
}
