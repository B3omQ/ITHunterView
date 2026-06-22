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
