import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
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

export const useGetMatchHistory = (page: number = 1, pageSize: number = 10) => {
  return useQuery({
    queryKey: ['match-history', page, pageSize],
    queryFn: () => cvService.getMatchHistory(page, pageSize),
  });
};

export const useDeleteMatchHistory = () => {
  const queryClient = useQueryClient();
  return useMutation<ApiResponse<string>, Error, string>({
    mutationFn: (jobId: string) => cvService.deleteMatchHistory(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['match-history'] });
    },
  });
};
