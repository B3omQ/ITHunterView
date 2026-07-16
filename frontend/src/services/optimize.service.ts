import api from './api-client';
import { ApiResponse } from '@/types/api.types';

export interface ApplySuggestionPayload {
  action: 'accept' | 'edit' | 'skip';
  editedText?: string;
  originalText?: string;
  suggestedText?: string;
}

export const optimizeService = {
  createSession: (matchId: string, payload: { cvUrl?: string; cvId?: string }) =>
    api.post<ApiResponse<string>>(`/api/optimize-sessions/match-sessions/${matchId}`, payload).then(r => r.data),

  getSuggestions: (sessionId: string) =>
    api.get<ApiResponse<any>>(`/api/optimize-sessions/${sessionId}/suggestions`).then(r => r.data),

  applySuggestion: (sessionId: string, suggestionId: string, payload: ApplySuggestionPayload) =>
    api.patch<ApiResponse<{ newScore: number; previewImageUrl: string }>>(`/api/optimize-sessions/${sessionId}/suggestions/${suggestionId}`, payload).then(r => r.data),

  generateFile: (sessionId: string) =>
    api.post<ApiResponse<string>>(`/api/optimize-sessions/${sessionId}/generate`).then(r => r.data),

  getPreview: (sessionId: string) =>
    api.get<ApiResponse<string | null>>(`/api/optimize-sessions/${sessionId}/preview`).then(r => r.data),
};
