import api from './api-client';
import type { ApiResponse } from '@/types/api.types';
import type {
  ProfileSummary,
  UpdateVisibilityRequest,
  AvatarUploadResponse,
  PersonalInfo,
  PersonalInfoUpdateRequest,
  CandidateSkill,
  SkillAddRequest,
  SkillSearchResponse,
  CandidateExperience,
  ExperienceUpsertRequest,
  Major,
  CandidateEducation,
  EducationUpsertRequest,
  CandidateCertification,
  CertificationUpsertRequest,
} from '@/types/candidate.types';

export const candidateService = {
  getProfileSummary: () =>
    api
      .get<ApiResponse<ProfileSummary>>('/api/v1/candidate/profile/summary')
      .then((r) => r.data),

  updateVisibility: (payload: UpdateVisibilityRequest) =>
    api
      .patch<ApiResponse<boolean>>('/api/v1/candidate/profile/visibility', payload)
      .then((r) => r.data),

  uploadAvatar: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api
      .post<ApiResponse<AvatarUploadResponse>>('/api/v1/candidate/profile/avatar', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      })
      .then((r) => r.data);
  },

  getPersonalInfo: () =>
    api
      .get<ApiResponse<PersonalInfo>>('/api/v1/candidate/profile/personal-info')
      .then((r) => r.data),

  updatePersonalInfo: (payload: PersonalInfoUpdateRequest) =>
    api
      .put<ApiResponse<PersonalInfo>>('/api/v1/candidate/profile/personal-info', payload)
      .then((r) => r.data),

  getSkills: () =>
    api
      .get<ApiResponse<CandidateSkill[]>>('/api/v1/candidate/profile/skills')
      .then((r) => r.data),

  searchSkills: (keyword: string, excludeOwned = false) =>
    api
      .get<ApiResponse<SkillSearchResponse[]>>('/api/v1/skills/search', {
        params: { keyword, excludeOwned },
      })
      .then((r) => r.data),

  getSkillSuggestions: () =>
    api
      .get<ApiResponse<CandidateSkill[]>>('/api/v1/candidate/profile/skills/suggestions')
      .then((r) => r.data),

  addSkill: (payload: SkillAddRequest) =>
    api
      .post<ApiResponse<CandidateSkill>>('/api/v1/candidate/profile/skills', payload)
      .then((r) => r.data),

  removeSkill: (skillId: number) =>
    api
      .delete<ApiResponse<boolean>>(`/api/v1/candidate/profile/skills/${skillId}`)
      .then((r) => r.data),

  getExperiences: () =>
    api
      .get<ApiResponse<CandidateExperience[]>>('/api/v1/candidate/profile/experiences')
      .then((r) => r.data),

  createExperience: (payload: ExperienceUpsertRequest) =>
    api
      .post<ApiResponse<CandidateExperience>>('/api/v1/candidate/profile/experiences', payload)
      .then((r) => r.data),

  updateExperience: (id: string, payload: ExperienceUpsertRequest) =>
    api
      .put<ApiResponse<CandidateExperience>>(`/api/v1/candidate/profile/experiences/${id}`, payload)
      .then((r) => r.data),

  deleteExperience: (id: string) =>
    api
      .delete<ApiResponse<boolean>>(`/api/v1/candidate/profile/experiences/${id}`)
      .then((r) => r.data),

  getMajors: () =>
    api
      .get<ApiResponse<Major[]>>('/api/v1/majors')
      .then((r) => r.data),

  getEducations: () =>
    api
      .get<ApiResponse<CandidateEducation[]>>('/api/v1/candidate/profile/educations')
      .then((r) => r.data),

  createEducation: (payload: EducationUpsertRequest) =>
    api
      .post<ApiResponse<CandidateEducation>>('/api/v1/candidate/profile/educations', payload)
      .then((r) => r.data),

  updateEducation: (id: string, payload: EducationUpsertRequest) =>
    api
      .put<ApiResponse<CandidateEducation>>(`/api/v1/candidate/profile/educations/${id}`, payload)
      .then((r) => r.data),

  deleteEducation: (id: string) =>
    api
      .delete<ApiResponse<boolean>>(`/api/v1/candidate/profile/educations/${id}`)
      .then((r) => r.data),

  getCertifications: () =>
    api
      .get<ApiResponse<CandidateCertification[]>>('/api/v1/candidate/profile/certifications')
      .then((r) => r.data),

  createCertification: (payload: CertificationUpsertRequest) =>
    api
      .post<ApiResponse<CandidateCertification>>('/api/v1/candidate/profile/certifications', payload)
      .then((r) => r.data),

  updateCertification: (id: string, payload: CertificationUpsertRequest) =>
    api
      .put<ApiResponse<CandidateCertification>>(`/api/v1/candidate/profile/certifications/${id}`, payload)
      .then((r) => r.data),

  deleteCertification: (id: string) =>
    api
      .delete<ApiResponse<boolean>>(`/api/v1/candidate/profile/certifications/${id}`)
      .then((r) => r.data),
};




