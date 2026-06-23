import { useQuery } from '@tanstack/react-query';
import { jobService } from '@/services/job.service';

export function useJobDetail(id: string, isCandidate: boolean) {
  return useQuery({
    queryKey: ['job-detail', id, isCandidate],
    queryFn: () => {
      if (isCandidate) {
        return jobService.getCandidateJobDetail(id);
      }
      return jobService.getPublicJobDetail(id);
    },
    enabled: !!id,
    staleTime: 60000,
  });
}
