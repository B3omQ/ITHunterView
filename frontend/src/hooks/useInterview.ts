import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { interviewService } from '@/services/interview.service';

export function useGetInterviewSessions() {
  return useQuery({
    queryKey: ['interview-sessions'],
    queryFn: () => interviewService.getSessions(),
  });
}

export function useGetInterviewSessionDetail(sessionId: string) {
  return useQuery({
    queryKey: ['interview-session', sessionId],
    queryFn: () => interviewService.getSessionDetail(sessionId),
    enabled: !!sessionId,
  });
}

export function useCreateInterviewSession() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: interviewService.createSession,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['interview-sessions'] });
      }
    },
  });
}

export function useSubmitInterviewReply(sessionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { message: string }) =>
      interviewService.submitReply(sessionId, data),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['interview-session', sessionId] });
      }
    },
  });
}

export function useSwitchInterviewModel(sessionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { aiProvider: string }) =>
      interviewService.switchModel(sessionId, data),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['interview-session', sessionId] });
        queryClient.invalidateQueries({ queryKey: ['interview-sessions'] });
      }
    },
  });
}

export function useCompleteInterviewSession(sessionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => interviewService.completeSession(sessionId),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['interview-session', sessionId] });
        queryClient.invalidateQueries({ queryKey: ['interview-sessions'] });
      }
    },
  });
}

export function useTranscribeAudio() {
  return useMutation({
    mutationFn: (file: File) => interviewService.transcribeAudio(file),
  });
}

export function useDeleteInterviewSession() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (sessionId: string) => interviewService.deleteSession(sessionId),
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['interview-sessions'] });
      }
    },
  });
}
