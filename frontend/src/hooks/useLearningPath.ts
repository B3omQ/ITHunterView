import { useQuery, useMutation } from '@tanstack/react-query';
import { learningPathService } from '@/services/learning-path.service';
import { GeneratePathRequest } from '@/types/learning-path.types';

export function useGenerateLearningPath() {
  return useMutation({
    mutationFn: (data: GeneratePathRequest) => learningPathService.generate(data),
  });
}

export function useMyLearningPaths() {
  return useQuery({
    queryKey: ['learning-paths', 'my-paths'],
    queryFn: () => learningPathService.getMyPaths(),
  });
}

export function useLearningPath(id: string) {
  return useQuery({
    queryKey: ['learning-paths', id],
    queryFn: () => learningPathService.getById(id),
    enabled: !!id,
  });
}
