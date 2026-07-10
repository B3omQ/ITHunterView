import api from './api-client';
import type { ApiResponse } from '@/types/api.types';
import type {
  InterviewSession,
  InterviewAnswer,
  InterviewSessionDetail,
  CreateInterviewSessionRequest,
  SubmitReplyRequest,
  SwitchModelRequest,
} from '@/types/interview.types';

export const interviewService = {
  getSessions: () =>
    api.get<ApiResponse<InterviewSession[]>>('/api/interview/sessions').then((r) => r.data),

  getSessionDetail: (sessionId: string) =>
    api.get<ApiResponse<InterviewSessionDetail>>(`/api/interview/sessions/${sessionId}`).then((r) => r.data),

  createSession: (data: CreateInterviewSessionRequest) =>
    api.post<ApiResponse<InterviewSession>>('/api/interview/sessions', data).then((r) => r.data),

  submitReply: (sessionId: string, data: SubmitReplyRequest) =>
    api.post<ApiResponse<InterviewAnswer>>(`/api/interview/sessions/${sessionId}/reply`, data).then((r) => r.data),

  switchModel: (sessionId: string, data: SwitchModelRequest) =>
    api.post<ApiResponse<string>>(`/api/interview/sessions/${sessionId}/switch-model`, data).then((r) => r.data),

  completeSession: (sessionId: string) =>
    api.post<ApiResponse<string>>(`/api/interview/sessions/${sessionId}/complete`).then((r) => r.data),

  transcribeAudio: (file: File) => {
    const formData = new FormData();
    formData.append('audio', file);
    return api
      .post<ApiResponse<string>>('/api/interview/transcribe', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data);
  },

  deleteSession: (sessionId: string) =>
    api.delete<ApiResponse<string>>(`/api/interview/sessions/${sessionId}`).then((r) => r.data),
};
