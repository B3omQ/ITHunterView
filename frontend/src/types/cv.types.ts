export interface Cv {
  id: string;
  userId: string;
  fileUrl: string;
  fileName: string;
  fileSize: number | null;
  fileType: string;
  isPrimary: boolean;
  parsedData: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCvRequest {
  fileUrl: string;
  fileName: string;
  fileSize: number | null;
  fileType: string;
  isPrimary: boolean;
  parsedData: string;
}

export interface MatchJdRequest {
  cvId?: string;
  cvUrl?: string;
  cvText?: string;
  jobId?: string;
  rawJdText?: string;
}

export interface CvJobMatchScoreResponse {
  id: string;
  cvId: string;
  jobId?: string;
  overallScore: number;
  skillMatchScore: number;
  experienceMatchScore: number;
  domainMatchScore: number;
  matchDetails: string;
}
