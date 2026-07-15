import api from './api-client';
import { ApiResponse } from '@/types/api.types';
import { GeneratePathRequest, GenerateFromCvJdRequest, GenerateFromInterviewRequest, LearningPath, HistoryContextPreviewDto } from '@/types/learning-path.types';

export const learningPathService = {
  generate: (data: GeneratePathRequest) =>
    api.post<ApiResponse<LearningPath>>('/api/learning-paths/generate', data, { timeout: 120000 }).then(r => r.data),

  generateFromCvJd: (data: GenerateFromCvJdRequest) =>
    api.post<ApiResponse<LearningPath>>('/api/learning-paths/generate-from-cv-jd', data, { timeout: 120000 }).then(r => r.data),

  generateFromInterview: (data: GenerateFromInterviewRequest) =>
    api.post<ApiResponse<LearningPath>>('/api/learning-paths/generate-from-interview', data, { timeout: 120000 }).then(r => r.data),

  getMyPaths: () =>
    api.get<ApiResponse<LearningPath[]>>('/api/learning-paths').then(r => r.data),

  getById: (id: string) =>
    api.get<ApiResponse<LearningPath>>(`/api/learning-paths/${id}`).then(r => r.data),

  deleteLearningPath: (id: string) =>
    api.delete<ApiResponse<string>>(`/api/learning-paths/${id}`).then(r => r.data),

  previewHistoryContext: (type: 'cv-jd' | 'interview', sourceId: string) =>
    api.get<ApiResponse<HistoryContextPreviewDto>>(`/api/learning-paths/preview-context?type=${type}&sourceId=${sourceId}`).then(r => r.data),

  toggleTaskCompletion: (pathId: string, moduleIndex: number, taskIndex: number) =>
    api.put<ApiResponse<LearningPath>>(`/api/learning-paths/${pathId}/modules/${moduleIndex}/tasks/${taskIndex}/toggle`).then(r => r.data),
};
