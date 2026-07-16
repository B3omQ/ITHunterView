export type DifficultyLevel = 'EASY' | 'MEDIUM' | 'HARD';

export type InterviewSessionStatus = 'IN_PROGRESS' | 'COMPLETED' | 'CANCELLED';

export interface InterviewSession {
  id: string;
  candidateId: string;
  jobId?: string;
  jobTitle?: string;
  cvId?: string;
  cvFileName?: string;
  difficultyLevel: DifficultyLevel;
  status: InterviewSessionStatus;
  startedAt?: string;
  endedAt?: string;
  aiProvider?: string;
}

export interface InterviewAnswer {
  id: string;
  sessionId: string;
  questionId?: string;
  parentAnswerId?: string;
  questionText: string;
  audioUrl?: string;
  candidateTranscript?: string;
  aiFeedback?: string;
  scoreLogic?: number;
  scoreTech?: number;
  scoreCommunication?: number;
  createdAt: string;
}

export interface InterviewReport {
  id: string;
  sessionId: string;
  totalScore?: number;
  overallFeedback: string;
}

export interface InterviewSessionDetail {
  session: InterviewSession;
  messages: InterviewAnswer[];
  report?: InterviewReport;
}

export interface CreateInterviewSessionRequest {
  difficultyLevel: DifficultyLevel;
  jobId?: string;
  cvId?: string;
  aiProvider?: string;
}

export interface SubmitReplyRequest {
  message: string;
}

export interface SwitchModelRequest {
  aiProvider: string;
}
