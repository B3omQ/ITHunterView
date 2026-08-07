export interface SectionAnalysis {
  sectionName: string;
  isPresent: boolean;
  status: 'Good' | 'Warning' | 'Missing';
  feedback: string;
}

export interface PriorityOrderCheck {
  candidateLevel: 'Student/Fresher' | 'Experienced' | string;
  isOrderOptimal: boolean;
  currentOrderDescription: string;
  recommendedOrderDescription: string;
  advice: string;
}

export interface CvImprovementRecommendation {
  category: 'Structure' | 'Contact' | 'Experience' | 'Skills' | 'Formatting' | string;
  title: string;
  description: string;
  priority: 'High' | 'Medium' | 'Low' | string;
  exampleBefore?: string;
  exampleAfter?: string;
}

export interface CvOptimizationResult {
  sessionId: string;
  cvId?: string;
  cvFileName?: string;
  overallScore: number;
  summary: string;
  sections: SectionAnalysis[];
  priorityOrder: PriorityOrderCheck;
  recommendations: CvImprovementRecommendation[];
}

export interface OptimizeHistoryItem {
  sessionId: string;
  cvId?: string;
  cvFileName?: string;
  originalFileType: string;
  overallScore: number;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
