import { useMutation } from '@tanstack/react-query';
import { uploadService } from '@/services/upload.service';

export function useUploadFile() {
  return useMutation({
    mutationFn: ({ file, folderName }: { file: File; folderName?: string }) =>
      uploadService.uploadFile(file, folderName),
  });
}
