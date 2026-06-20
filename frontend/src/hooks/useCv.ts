import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cvService } from '@/services/cv.service';

export function useGetMyCvs() {
  return useQuery({
    queryKey: ['cvs'],
    queryFn: () => cvService.getMyCvs(),
  });
}

export function useCreateCv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cvService.createCv,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['cvs'] });
      }
    },
  });
}

export function useDeleteCv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cvService.deleteCv,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['cvs'] });
      }
    },
  });
}
