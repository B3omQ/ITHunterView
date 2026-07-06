import { useMutation, useQueryClient } from '@tanstack/react-query';
import { jobService } from '@/services/job.service';
import { toast } from 'sonner';

export function useJobActions() {
  const queryClient = useQueryClient();

  const saveJobMutation = useMutation({
    mutationFn: (jobId: string) => jobService.saveJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['candidate-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['saved-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['job-detail'] });
      toast.success('Job saved successfully');
    },
    onError: () => {
      toast.error('Failed to save job');
    }
  });

  const unsaveJobMutation = useMutation({
    mutationFn: (jobId: string) => jobService.unsaveJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['candidate-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['saved-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['job-detail'] });
      toast.success('Job removed from saved list');
    },
    onError: () => {
      toast.error('Failed to unsave job');
    }
  });

  return {
    saveJob: saveJobMutation.mutateAsync,
    isSaving: saveJobMutation.isPending,
    unsaveJob: unsaveJobMutation.mutateAsync,
    isUnsaving: unsaveJobMutation.isPending,
  };
}
