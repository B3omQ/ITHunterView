import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cvService } from '@/services/cv.service';

export function useCandidateJobScan(cvId?: string, page: number = 1, pageSize: number = 20) {
  return useQuery({
    queryKey: ['candidate-job-scan', cvId, page, pageSize],
    queryFn: () => cvService.getLatestJobScan(cvId!, page, pageSize),
    enabled: Boolean(cvId),
    staleTime: 60 * 1000,
  });
}

export function useScanCandidateJobs() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (cvId: string) => cvService.matchJobsHardcode(cvId),
    onSuccess: (_, cvId) => {
      queryClient.invalidateQueries({ queryKey: ['candidate-job-scan', cvId] });
    },
  });
}
