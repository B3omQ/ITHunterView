export interface SuggestedEdit {
  section: string;
  originalText: string;
  suggestedText: string;
  reason: string;
}

export interface OptimizationFeedback {
  strengths: string[];
  weaknesses: string[];
  missingKeywords: string[];
  suggestedEdits: SuggestedEdit[];
  overallScore: number;
}

export interface CvOptimization {
  id: string;
  candidateId: string;
  cvId: string;
  targetJdText?: string;
  feedbackData: OptimizationFeedback;
  createdAt: string;
}

export interface OptimizeCvRequest {
  cvId: string;
  targetJdText?: string;
}
