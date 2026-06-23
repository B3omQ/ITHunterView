import api from './api-client';
import type { ApiResponse } from '@/types/api.types';

export const uploadService = {
  uploadFile: (file: File, folderName: string = 'general') => {
    const formData = new FormData();
    formData.append('file', file);
    return api
      .post<ApiResponse<string>>('/api/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        params: {
          folderName,
        },
      })
      .then((r) => r.data);
  },
};
