import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { recruiterService } from '@/services/recruiter.service';

export function useRecruiterCvScan(jobId?: string, page: number = 1, pageSize: number = 10) {
  return useQuery({
    queryKey: ['recruiter-cv-scan', jobId, page, pageSize],
    queryFn: () => recruiterService.getJobMatches(jobId!, page, pageSize),
    enabled: Boolean(jobId),
    staleTime: 60 * 1000,
  });
}

export function useScanRecruiterCvs() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobId: string) => recruiterService.matchJobWithCvsHardcode(jobId),
    onSuccess: (_, jobId) => {
      queryClient.invalidateQueries({ queryKey: ['recruiter-cv-scan', jobId] });
    },
  });
}

export function useUnlockCandidateCv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (scanResultId: string) => recruiterService.unlockCandidateCv(scanResultId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recruiter-cv-scan'] });
      queryClient.invalidateQueries({ queryKey: ['wallet'] });
      queryClient.invalidateQueries({ queryKey: ['user-subscription'] });
    },
  });
}
