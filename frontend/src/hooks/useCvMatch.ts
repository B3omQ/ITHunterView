import { useMutation } from '@tanstack/react-query';
import { cvService } from '@/services/cv.service';
import type { MatchJdRequest, CvJobMatchScoreResponse } from '@/types/cv.types';

export const useMatchCvJd = () => {
  return useMutation<CvJobMatchScoreResponse, Error, MatchJdRequest>({
    mutationFn: (data: MatchJdRequest) => cvService.matchCvJd(data),
  });
};
