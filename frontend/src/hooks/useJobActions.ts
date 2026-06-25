import { useMutation, useQueryClient } from '@tanstack/react-query';
import { jobService } from '@/services/job.service';

export function useJobActions() {
  const queryClient = useQueryClient();

  const saveJobMutation = useMutation({
    mutationFn: (jobId: string) => jobService.saveJob(jobId),
    onSuccess: () => {
      // Invalidate relevant queries to update UI
      queryClient.invalidateQueries({ queryKey: ['candidate-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['saved-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['job-detail'] });
    },
  });

  const unsaveJobMutation = useMutation({
    mutationFn: (jobId: string) => jobService.unsaveJob(jobId),
    onSuccess: () => {
      // Invalidate relevant queries to update UI
      queryClient.invalidateQueries({ queryKey: ['candidate-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['saved-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['job-detail'] });
    },
  });

  return {
    saveJob: saveJobMutation.mutateAsync,
    isSaving: saveJobMutation.isPending,
    unsaveJob: unsaveJobMutation.mutateAsync,
    isUnsaving: unsaveJobMutation.isPending,
  };
}
