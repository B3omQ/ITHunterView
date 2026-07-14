export interface GeneratePathRequest {
  // Core Information
  targetRole: string;
  specificGoal: string;
  experienceLevel: string;
  timeframeInWeeks: number;
  hoursPerWeek: number;

  // Technical Information
  currentSkills: string;
  targetCompanyType?: string;
  strengths?: string;
  weaknesses?: string;

  // Personalization
  learningStyle?: string;
  additionalPreferences?: string;
}

export interface GenerateFromCvJdRequest {
  matchScoreId?: string;
  timeframeInWeeks?: number;
}

export interface GenerateFromInterviewRequest {
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

export interface HistoryContextPreviewDto {
  contextPreview: string;
}
