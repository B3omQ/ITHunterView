import api from './api-client';
import { ApiResponse } from '@/types/api.types';
import { GeneratePathRequest, LearningPath } from '@/types/learning-path.types';

export const learningPathService = {
  generate: (data: GeneratePathRequest) => 
    api.post<ApiResponse<LearningPath>>('/api/learning-paths/generate', data).then(r => r.data),
    
  getMyPaths: () => 
    api.get<ApiResponse<LearningPath[]>>('/api/learning-paths').then(r => r.data),
    
  getById: (id: string) => 
    api.get<ApiResponse<LearningPath>>(`/api/learning-paths/${id}`).then(r => r.data),
};
