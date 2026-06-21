export interface ProfileSummary {
  id: string;
  fullName: string;
  avatarUrl: string | null;
  currentTitle: string | null;
  location: string | null;
  isVisibleToRecruiters: boolean;
  profileCompletionPercentage: number;
  completionHint: string | null;
  lastSavedAt: string | null;
}

export interface UpdateVisibilityRequest {
  isVisibleToRecruiters: boolean;
}

export interface AvatarUploadResponse {
  avatarUrl: string;
}

export interface PersonalInfo {
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  location: string | null;
  aboutMe: string | null;
  portfolioUrl: string | null;
  linkedInUrl: string | null;
  githubUrl: string | null;
  updatedAt: string | null;
}

export interface PersonalInfoUpdateRequest {
  firstName: string;
  lastName: string;
  phone: string | null;
  location: string | null;
  aboutMe: string | null;
  portfolioUrl: string | null;
  linkedInUrl: string | null;
  githubUrl: string | null;
}

export interface CandidateSkill {
  skillId: number;
  name: string;
  proficiencyLevel: number | null;
}

export interface SkillAddRequest {
  skillId: number;
  proficiencyLevel?: number | null;
}

export interface SkillSearchResponse {
  id: number;
  name: string;
  categoryId: number | null;
}

export interface CandidateExperience {
  id: string;
  title: string;
  companyName: string | null;
  companyId: string | null;
  location: string | null;
  employmentType: string | null;
  startDate: string | null; // ISO Date string format (e.g. YYYY-MM-DD)
  endDate: string | null;   // ISO Date string format (e.g. YYYY-MM-DD)
  isCurrent: boolean;
  description: string | null;
}

export interface ExperienceUpsertRequest {
  title: string;
  companyName?: string | null;
  companyId?: string | null;
  location?: string | null;
  employmentType?: string | null; // e.g. FULL_TIME, PART_TIME
  startDate?: string | null;
  endDate?: string | null;
  isCurrent: boolean;
  description?: string | null;
}

export interface Major {
  id: number;
  name: string;
  code: string | null;
}

export interface CandidateEducation {
  id: string;
  degree: string;
  majorId: number | null;
  institutionName: string;
  gpa: number | null;
  maxGpa: number | null;
  startDate: string | null;
  endDate: string | null;
  description: string | null;
  majorName?: string | null; // Added dynamically on FE/BE for visualization
}

export interface EducationUpsertRequest {
  degree: string;
  majorId?: number | null;
  institutionName: string;
  gpa?: number | null;
  maxGpa?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  description?: string | null;
}

export interface CandidateCertification {
  id: string;
  name: string;
  issuingOrganization: string;
  issueDate: string | null;
  expirationDate: string | null;
  credentialUrl: string | null;
}

export interface CertificationUpsertRequest {
  name: string;
  issuingOrganization: string;
  issueDate?: string | null;
  expirationDate?: string | null;
  credentialUrl?: string | null;
}

