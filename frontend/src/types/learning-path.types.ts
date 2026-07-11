export interface GeneratePathRequest {
  targetRole: string;
  currentSkills: string;
  targetSkills: string;
  timeframeInWeeks: number;
}

export interface LearningModule {
  title: string;
  description: string;
  durationWeeks: number;
  skills: string[];
}

export interface LearningPath {
  id: string;
  candidateId: string;
  sessionId: string | null;
  pathData: LearningModule[];
  createdAt: string;
}
