export type UserStatus = 'ACTIVE' | 'INACTIVE' | 'BANNED' | 'PENDING_VERIFICATION';

export enum SystemRole {
  Admin = 1,
  Staff = 2,
  Recruiter = 3,
  Candidate = 4,
}

export type ActivityLogCategory = 'AUTH' | 'SYSTEM' | 'DATA_MUTATION' | 'SECURITY';

export type ActivityLogStatus = 'SUCCESS' | 'FAIL';

export interface UserDto {
  id: string;
  email: string;
  roleId: number | null;
  roleName: string;
  status: UserStatus;
  fullName: string;
  phone: string | null;
  createdAt: string;
  deactiveAt: string | null;
}

export interface CandidateProfileDetailDto {
  firstName: string;
  lastName: string;
  phone: string;
  location: string;
  aboutMe: string;
  avatarUrl: string;
  githubUrl: string;
  linkedInUrl: string;
  portfolioUrl: string;
}

export interface CompanyDetailDto {
  id: string;
  name: string;
  headquartersAddress: string;
  industry: string;
  website: string;
  logoUrl: string;
}

export interface RecruiterProfileDetailDto {
  fullName: string;
  phone: string;
  positionTitle: string;
  avatarUrl: string;
  company: CompanyDetailDto | null;
}

export interface UserDetailDto {
  id: string;
  email: string;
  roleId: number | null;
  roleName: string;
  status: UserStatus;
  createdAt: string;
  deactiveAt: string | null;
  candidateProfile: CandidateProfileDetailDto | null;
  recruiterProfile: RecruiterProfileDetailDto | null;
}

export interface UpdateUserStatusDto {
  status: UserStatus;
  reason: string;
}

export interface UpdateUserRoleDto {
  roleId: number;
}

export interface CreateStaffDto {
  email: string;
  password: string;
}
