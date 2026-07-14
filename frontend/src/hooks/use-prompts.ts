import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PromptService } from '@/services/prompt.service';
import { CreatePromptVersionDto } from '@/types/prompt.types';
import { toast } from 'sonner';

export const usePrompts = (page: number = 1, size: number = 10) => {
  return useQuery({
    queryKey: ['prompts', page, size],
    queryFn: () => PromptService.getPagedPrompts(page, size),
  });
};

export const usePromptHistory = (promptId: string) => {
  return useQuery({
    queryKey: ['prompt-history', promptId],
    queryFn: () => PromptService.getPromptHistory(promptId),
    enabled: !!promptId,
  });
};

export const usePromptVersion = (versionId: string) => {
  return useQuery({
    queryKey: ['prompt-version', versionId],
    queryFn: () => PromptService.getPromptVersion(versionId),
    enabled: !!versionId,
  });
};

export const useCreatePromptVersion = (promptId: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: CreatePromptVersionDto) => PromptService.createPromptVersion(promptId, dto),
    onSuccess: () => {
      toast.success('Prompt version created successfully');
      queryClient.invalidateQueries({ queryKey: ['prompt-history', promptId] });
      queryClient.invalidateQueries({ queryKey: ['prompts'] });
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Failed to create prompt version';
      toast.error(message);
    },
  });
};

export const useActivatePromptVersion = (promptId: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (versionId: string) => PromptService.activatePromptVersion(promptId, versionId),
    onSuccess: () => {
      toast.success('Prompt version activated successfully');
      queryClient.invalidateQueries({ queryKey: ['prompt-history', promptId] });
      queryClient.invalidateQueries({ queryKey: ['prompts'] });
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Failed to activate prompt version';
      toast.error(message);
    },
  });
};
