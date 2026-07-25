import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
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
        if (res.data?.warningMessage) {
          toast.warning(res.data.warningMessage);
        } else {
          toast.success('Thêm CV thành công');
        }
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
        toast.success('Xóa CV thành công');
      }
    },
  });
}

export function useSetPrimaryCv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ['set-primary-cv'],
    mutationFn: cvService.setPrimaryCv,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['cvs'] });
        toast.success('Cập nhật CV chính thành công');
      }
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.message || 'Không thể cập nhật CV chính');
    }
  });
}
