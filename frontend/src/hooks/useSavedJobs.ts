import { useQuery } from '@tanstack/react-query';
import { jobService } from '@/services/job.service';

export function useSavedJobs(page: number, pageSize: number) {
  return useQuery({
    queryKey: ['saved-jobs', page, pageSize],
    queryFn: () => jobService.getSavedJobs(page, pageSize),
    staleTime: 0, // Always fresh to reflect unsaves quickly
  });
}
