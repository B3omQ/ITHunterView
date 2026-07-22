import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cvService } from '@/services/cv.service';

export function useGetMyCvs() {
  return useQuery({
    queryKey: ['cvs'],
    queryFn: () => cvService.getMyCvs(),
    refetchInterval: (query) => {
      const cvs = query.state.data?.data || [];
      const hasPendingOrProcessing = cvs.some(
        (cv) => !cv.parseStatus || cv.parseStatus === 'PENDING' || cv.parseStatus === 'PROCESSING'
      );
      return hasPendingOrProcessing ? 3000 : false;
    },
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
