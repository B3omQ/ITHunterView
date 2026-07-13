import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { learningPathService } from '@/services/learning-path.service';
import { GeneratePathRequest, GenerateFromHistoryRequest } from '@/types/learning-path.types';

export function useGenerateLearningPath() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: GeneratePathRequest) => learningPathService.generate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['learning-paths'] });
    },
  });
}

export function useGenerateLearningPathFromHistory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: GenerateFromHistoryRequest) => learningPathService.generateFromHistory(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['learning-paths'] });
    },
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

export function useDeleteLearningPath() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => learningPathService.deleteLearningPath(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['learning-paths'] });
    },
  });
}
