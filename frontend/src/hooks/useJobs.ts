import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  recruiterService,
  type ApiResponse,
  type CreateJobPostingDto,
  type JobCategory,
  type JobPosting,
  type JobPostingSummary,
  type PaginatedResult,
  type Skill,
  type UpdateJobPostingDto,
} from '@/services/recruiter.service'

export const recruiterJobKeys = {
  all: ['recruiter-jobs'] as const,
  lists: () => [...recruiterJobKeys.all, 'list'] as const,
  list: (page: number, pageSize: number, status: string, search: string) =>
    [...recruiterJobKeys.lists(), page, pageSize, status, search] as const,
  details: () => [...recruiterJobKeys.all, 'detail'] as const,
  detail: (jobId: string) => [...recruiterJobKeys.details(), jobId] as const,
  metadata: () => [...recruiterJobKeys.all, 'metadata'] as const,
  categories: () => [...recruiterJobKeys.metadata(), 'categories'] as const,
  skills: () => [...recruiterJobKeys.metadata(), 'skills'] as const,
  majors: () => [...recruiterJobKeys.metadata(), 'majors'] as const,
}

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error && error.message ? error.message : fallback
}

function unwrap<T>(result: { success: boolean; data?: ApiResponse<T>; message?: string }, fallback: string): T {
  if (!result.success || !result.data?.success || result.data.data === undefined || result.data.data === null) {
    throw new Error(result.data?.message || result.message || fallback)
  }
  return result.data.data
}

export function useJobs(initialPage = 1, initialPageSize = 7, initialStatus = 'ALL') {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(initialPage)
  const [pageSize, setPageSize] = useState(initialPageSize)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [status, setStatus] = useState(initialStatus)

  useEffect(() => {
    const handler = window.setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 400)
    return () => window.clearTimeout(handler)
  }, [search])

  const jobsQuery = useQuery({
    queryKey: recruiterJobKeys.list(page, pageSize, status, debouncedSearch),
    queryFn: async () =>
      unwrap<PaginatedResult<JobPostingSummary>>(
        await recruiterService.getJobs(page, pageSize, status, debouncedSearch),
        'Failed to load job postings',
      ),
    refetchInterval: (query) => {
      const hasActiveAnalysis = query.state.data?.items.some(
        (job) => job.parseStatus === 'PENDING' || job.parseStatus === 'PROCESSING',
      )
      return hasActiveAnalysis ? 3000 : false
    },
  })

  const closeMutation = useMutation({
    mutationFn: async (id: string) =>
      unwrap<boolean>(await recruiterService.closeJob(id), 'Failed to close job posting'),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: recruiterJobKeys.lists() })
    },
  })

  const extendMutation = useMutation({
    mutationFn: async (id: string) => await recruiterService.extendJob(id),
    onSuccess: (res) => {
      if (res.success) {
        void queryClient.invalidateQueries({ queryKey: recruiterJobKeys.lists() })
      }
    },
  })

  const pushTopMutation = useMutation({
    mutationFn: async (id: string) => await recruiterService.pushTopJob(id),
    onSuccess: (res) => {
      if (res.success) {
        void queryClient.invalidateQueries({ queryKey: recruiterJobKeys.lists() })
      }
    },
  })

  const closeJob = async (id: string) => {
    try {
      await closeMutation.mutateAsync(id)
      return { success: true }
    } catch (error) {
      return { success: false, message: getErrorMessage(error, 'Failed to close job posting') }
    }
  }

  const extendJob = async (id: string) => {
    try {
      return await extendMutation.mutateAsync(id)
    } catch (error) {
      return { success: false, message: getErrorMessage(error, 'Gia hạn không thành công.') }
    }
  }

  const pushTopJob = async (id: string) => {
    try {
      return await pushTopMutation.mutateAsync(id)
    } catch (error) {
      return { success: false, message: getErrorMessage(error, 'Đẩy Top không thành công.') }
    }
  }

  return {
    jobs: jobsQuery.data?.items ?? [],
    totalCount: jobsQuery.data?.totalCount ?? 0,
    page,
    setPage,
    pageSize,
    setPageSize,
    search,
    setSearch,
    status,
    setStatus,
    loading: jobsQuery.isLoading || jobsQuery.isFetching,
    error: jobsQuery.isError ? getErrorMessage(jobsQuery.error, 'Failed to load job postings') : '',
    refresh: jobsQuery.refetch,
    closeJob,
    isClosing: closeMutation.isPending,
    extendJob,
    isExtending: extendMutation.isPending,
    pushTopJob,
    isPushingTop: pushTopMutation.isPending,
  }
}

export function useJobDetails(jobId?: string) {
  const queryClient = useQueryClient()
  const detailQuery = useQuery({
    queryKey: recruiterJobKeys.detail(jobId ?? ''),
    enabled: Boolean(jobId),
    queryFn: async () =>
      unwrap<JobPosting>(await recruiterService.getJobById(jobId!), 'Failed to load job details'),
  })

  const createMutation = useMutation({
    mutationFn: (payload: CreateJobPostingDto) => recruiterService.createJob(payload),
    onSuccess: (result) => {
      if (result.success && result.data?.success && result.data.data) {
        queryClient.setQueryData(recruiterJobKeys.detail(result.data.data.id), result.data.data)
      }
      void queryClient.invalidateQueries({ queryKey: recruiterJobKeys.lists() })
    },
  })

  const updateMutation = useMutation({
    mutationFn: (payload: UpdateJobPostingDto) => recruiterService.updateJob(jobId!, payload),
    onSuccess: (result) => {
      if (result.success && result.data?.success && result.data.data && jobId) {
        queryClient.setQueryData(recruiterJobKeys.detail(jobId), result.data.data)
      }
      if (jobId) {
        void queryClient.invalidateQueries({ queryKey: ['job-analysis', jobId] })
      }
      void queryClient.invalidateQueries({ queryKey: recruiterJobKeys.lists() })
    },
  })

  const createJob = async (payload: CreateJobPostingDto) => {
    try {
      const result = await createMutation.mutateAsync(payload)
      const data = unwrap<JobPosting>(result, 'Failed to create job posting')
      return { success: true, data }
    } catch (error) {
      return { success: false, message: getErrorMessage(error, 'Failed to create job posting') }
    }
  }

  const updateJob = async (payload: UpdateJobPostingDto) => {
    if (!jobId) return { success: false, message: 'Job ID is required for updating' }
    try {
      const result = await updateMutation.mutateAsync(payload)
      const data = unwrap<JobPosting>(result, 'Failed to update job posting')
      return { success: true, data }
    } catch (error) {
      return { success: false, message: getErrorMessage(error, 'Failed to update job posting') }
    }
  }

  return {
    job: detailQuery.data ?? null,
    loading: detailQuery.isLoading || detailQuery.isFetching,
    saving: createMutation.isPending || updateMutation.isPending,
    error: detailQuery.isError ? getErrorMessage(detailQuery.error, 'Failed to load job details') : '',
    setError: () => undefined,
    refresh: detailQuery.refetch,
    createJob,
    updateJob,
  }
}

export function useJobMetadata() {
  const categoriesQuery = useQuery({
    queryKey: recruiterJobKeys.categories(),
    queryFn: async () =>
      unwrap<JobCategory[]>(await recruiterService.getCategories(), 'Failed to load job categories'),
    staleTime: 5 * 60 * 1000,
  })
  const skillsQuery = useQuery({
    queryKey: recruiterJobKeys.skills(),
    queryFn: async () => unwrap<Skill[]>(await recruiterService.getSkills(), 'Failed to load skills'),
    staleTime: 5 * 60 * 1000,
  })
  const majorsQuery = useQuery({
    queryKey: recruiterJobKeys.majors(),
    queryFn: async () => {
      const result = unwrap<PaginatedResult<{ id: number; name: string; parentId?: number; parentName?: string }>>(
        await recruiterService.getMajors(),
        'Failed to load majors',
      )
      return result?.items ?? (result as any)?.Items ?? []
    },
    staleTime: 5 * 60 * 1000,
  })

  const firstError = categoriesQuery.error || skillsQuery.error || majorsQuery.error
  return {
    categories: categoriesQuery.data ?? [],
    availableSkills: skillsQuery.data ?? [],
    majors: majorsQuery.data ?? [],
    loading: categoriesQuery.isLoading || skillsQuery.isLoading || majorsQuery.isLoading,
    error: firstError ? getErrorMessage(firstError, 'Failed to load job metadata') : '',
  }
}
