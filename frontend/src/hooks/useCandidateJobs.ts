import { useQuery } from '@tanstack/react-query';
import { jobService } from '@/services/job.service';
import type { JobSearchQuery } from '@/types/job.types';

export function useCandidateJobs(query: JobSearchQuery) {
  return useQuery({
    queryKey: ['candidate-jobs', query],
    queryFn: () => jobService.getCandidateJobs(query),
    staleTime: 60000,
  });
}
