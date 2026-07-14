export interface GeneratePathRequest {
  // Core Information
  targetRole: string;
  specificGoal: string;
  experienceLevel: string;

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
}

export interface GenerateFromInterviewRequest {
  sessionId?: string;
}

export interface LearningTask {
  title: string;
  description: string;
  completed?: boolean;
}

export interface LearningModule {
  title: string;
  description: string;
  skills?: string[];
  gapSource?: 'cv-jd-match' | 'interview' | 'both';
  tasks: LearningTask[];
  completed?: boolean;
}

export interface LearningPath {
  id: string;
  candidateId: string;
  sessionId: string | null;
  title: string;
  status: string;
  pathData: LearningModule[];
  createdAt: string;
}

export interface HistoryContextPreviewDto {
  contextPreview: string;
}
