import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { learningPathService } from '@/services/learning-path.service';
import { GeneratePathRequest, GenerateFromCvJdRequest, GenerateFromInterviewRequest } from '@/types/learning-path.types';
import { toast } from 'sonner';

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
      toast.success('Learning path deleted successfully.');
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || 'Failed to delete learning path.');
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

export function useToggleModule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ pathId, moduleIndex }: { pathId: string; moduleIndex: number }) => 
      learningPathService.toggleModuleCompletion(pathId, moduleIndex),
    onSuccess: (_, { pathId }) => {
      queryClient.invalidateQueries({ queryKey: ['learning-paths'] });
      queryClient.invalidateQueries({ queryKey: ['learning-paths', pathId] });
    },
  });
}
