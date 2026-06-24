import { useQuery } from '@tanstack/react-query';
import { jobService } from '@/services/job.service';
import type { JobSearchQuery } from '@/types/job.types';

export function usePublicJobs(query: JobSearchQuery) {
  return useQuery({
    queryKey: ['public-jobs', query],
    queryFn: () => jobService.getPublicJobs(query),
    staleTime: 60000,
  });
}
