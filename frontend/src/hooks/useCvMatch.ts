import { useMutation, useQuery } from '@tanstack/react-query';
import { cvService } from '@/services/cv.service';
import type { MatchJdRequest, MatchingResultDto } from '@/types/cv.types';
import type { ApiResponse } from '@/types/api.types';

export const useMatchCvJd = () => {
  return useMutation<ApiResponse<string>, Error, MatchJdRequest>({
    mutationFn: (data: MatchJdRequest) => cvService.matchCvJd(data),
  });
};

export const useGetMatchResult = (jobId: string | null) => {
  return useQuery<ApiResponse<MatchingResultDto>, Error>({
    queryKey: ['match-result', jobId],
    queryFn: () => cvService.getMatchResult(jobId!),
    enabled: !!jobId,
    refetchInterval: (query) => {
      // Poll every 2 seconds if status is still processing or pending
      const status = query.state.data?.data?.status;
      if (status === 'Pending' || status === 'Processing') return 2000;
      return false;
    },
  });
};
