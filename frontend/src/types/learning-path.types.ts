export interface GeneratePathRequest {
  targetRole: string;
  currentSkills: string;
  targetSkills: string;
  timeframeInWeeks: number;
}

export interface GenerateFromHistoryRequest {
  matchScoreId?: string;
  sessionId?: string;
  timeframeInWeeks?: number;
}

export interface LearningModule {
  title: string;
  description: string;
  durationWeeks: number;
  skills: string[];
  gapSource?: 'cv-jd-match' | 'interview' | 'both';
}

export interface LearningPath {
  id: string;
  candidateId: string;
  sessionId: string | null;
  pathData: LearningModule[];
  createdAt: string;
}
