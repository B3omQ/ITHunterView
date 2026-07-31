import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PromptService } from '@/services/prompt.service';
import { ActivateCvAnalysisPromptPairDto, CreatePromptVersionDto } from '@/types/prompt.types';
import { toast } from 'sonner';

const getErrorMessage = (error: unknown, fallback: string) => {
  if (typeof error !== 'object' || error === null || !('response' in error)) {
    return fallback;
  }

  const response = error.response as { data?: { message?: unknown } } | undefined;
  return typeof response?.data?.message === 'string' ? response.data.message : fallback;
};

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
    onError: (error: unknown) => {
      toast.error(getErrorMessage(error, 'Failed to create prompt version'));
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
    onError: (error: unknown) => {
      toast.error(getErrorMessage(error, 'Failed to activate prompt version'));
    },
  });
};

export const useCvAnalysisPromptPair = () => {
  return useQuery({
    queryKey: ['cv-analysis-prompt-pair'],
    queryFn: () => PromptService.getCvAnalysisPromptPair(),
  });
};

export const useActivateCvAnalysisPromptPair = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: ActivateCvAnalysisPromptPairDto) => PromptService.activateCvAnalysisPromptPair(dto),
    onSuccess: () => {
      toast.success('CV analysis prompt pair activated successfully');
      queryClient.invalidateQueries({ queryKey: ['cv-analysis-prompt-pair'] });
      queryClient.invalidateQueries({ queryKey: ['prompt-history'] });
      queryClient.invalidateQueries({ queryKey: ['prompts'] });
    },
    onError: (error: unknown) => {
      toast.error(getErrorMessage(error, 'Failed to activate CV analysis prompt pair'));
    },
  });
};
