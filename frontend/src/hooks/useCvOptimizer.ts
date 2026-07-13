import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { cvOptimizerService } from '@/services/cv-optimizer.service';
import { OptimizeCvRequest } from '@/types/cv-optimizer.types';

export const useOptimizeCv = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: OptimizeCvRequest) => cvOptimizerService.optimize(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cv-optimizations'] });
    },
  });
};

export const useCvOptimizationHistory = () => {
  return useQuery({
    queryKey: ['cv-optimizations'],
    queryFn: () => cvOptimizerService.getHistory(),
  });
};

export const useCvOptimizationById = (id: string) => {
  return useQuery({
    queryKey: ['cv-optimization', id],
    queryFn: () => cvOptimizerService.getById(id),
    enabled: !!id,
  });
};
