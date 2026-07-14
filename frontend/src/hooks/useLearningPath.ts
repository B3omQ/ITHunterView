import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { learningPathService } from '@/services/learning-path.service';
import { GeneratePathRequest, GenerateFromCvJdRequest, GenerateFromInterviewRequest } from '@/types/learning-path.types';

export function useGenerateLearningPath() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: GeneratePathRequest) => learningPathService.generate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['learning-paths'] });
    },
  });
}

export function useGenerateFromCvJd() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: GenerateFromCvJdRequest) => learningPathService.generateFromCvJd(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['learning-paths'] });
    },
  });
}

export function useGenerateFromInterview() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: GenerateFromInterviewRequest) => learningPathService.generateFromInterview(data),
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

export function usePreviewHistoryContext(type: 'cv-jd' | 'interview', sourceId: string | null) {
  return useQuery({
    queryKey: ['learning-paths', 'preview', type, sourceId],
    queryFn: () => learningPathService.previewHistoryContext(type, sourceId!),
    enabled: !!sourceId && sourceId !== '',
  });
}
