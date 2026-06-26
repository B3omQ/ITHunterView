export enum ApplicationStatus {
  APPLIED = 'APPLIED',
  VIEWED = 'VIEWED',
  SHORTLISTED = 'SHORTLISTED',
  INTERVIEWING = 'INTERVIEWING',
  OFFERED = 'OFFERED',
  HIRED = 'HIRED',
  REJECTED = 'REJECTED',
  WITHDRAWN = 'WITHDRAWN',
}

export interface ApplicantDto {
  id: string;
  candidateId: string;
  candidateName: string;
  email: string;
  status: ApplicationStatus;
  applyDate: string;
  avatarUrl?: string;
  cvId?: string;
}

export interface JobApplicationDetailDto {
  id: string;
  candidateId: string;
  candidateName: string;
  email: string;
  status: ApplicationStatus;
  applyDate: string;
  avatarUrl?: string;
  coverLetter?: string;
  cvId?: string;
  cvUrl?: string;
  cvFileName?: string;
}

export interface CandidateAppliedJobDto {
  id: string;
  jobId: string;
  jobTitle: string;
  companyName: string;
  companyLogoUrl?: string;
  status: ApplicationStatus;
  applyDate: string;
}
