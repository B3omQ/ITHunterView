import api from './api-client';
import type { ApiResponse, PaginatedResponse } from '@/types/api.types';
import type { QuestionBankDto, CreateQuestionBankDto, UpdateQuestionBankDto } from '@/types/question-bank.types';

export const questionBankService = {
  getPagedQuestions: (
    params: {
      page?: number;
      pageSize?: number;
      categoryId?: number;
      level?: string;
    },
    signal?: AbortSignal
  ) =>
    api
      .get<ApiResponse<PaginatedResponse<QuestionBankDto>>>('/api/interview-questions', { params, signal })
      .then((res) => res.data),

  getQuestionById: (id: string, signal?: AbortSignal) =>
    api
      .get<ApiResponse<QuestionBankDto>>(`/api/interview-questions/${id}`, { signal })
      .then((res) => res.data),

  createQuestion: (data: CreateQuestionBankDto) =>
    api
      .post<ApiResponse<QuestionBankDto>>('/api/interview-questions', data)
      .then((res) => res.data),

  importExcel: (industry: string, level: string, file: File) => {
    const formData = new FormData();
    formData.append('industry', industry);
    formData.append('level', level);
    formData.append('file', file);
    return api
      .post<{importedCount: number}>('/api/interview-questions/import', formData)
      .then((res) => res.data);
  },

  updateQuestion: (id: string, data: UpdateQuestionBankDto) =>
    api
      .put<ApiResponse<QuestionBankDto>>(`/api/interview-questions/${id}`, data)
      .then((res) => res.data),

  deleteQuestion: (id: string) =>
    api
      .delete<ApiResponse<any>>(`/api/interview-questions/${id}`)
      .then((res) => res.data),
};
