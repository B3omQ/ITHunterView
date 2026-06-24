import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { candidateService } from '@/services/candidate.service';
import type {
  UpdateVisibilityRequest,
  PersonalInfoUpdateRequest,
  SkillAddRequest,
  ExperienceUpsertRequest,
  EducationUpsertRequest,
  CertificationUpsertRequest,
} from '@/types/candidate.types';

export const CANDIDATE_PROFILE_KEYS = {
  summary: ['candidate-profile-summary'] as const,
  personalInfo: ['candidate-personal-info'] as const,
  skills: ['candidate-skills'] as const,
  experiences: ['candidate-experiences'] as const,
  majors: ['candidate-majors'] as const,
  educations: ['candidate-educations'] as const,
  certifications: ['candidate-certifications'] as const,
};

export function useProfileSummary() {
  return useQuery({
    queryKey: CANDIDATE_PROFILE_KEYS.summary,
    queryFn: () => candidateService.getProfileSummary().then((res) => res.data),
  });
}

export function useUpdateVisibility() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateVisibilityRequest) => candidateService.updateVisibility(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useUploadAvatar() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (file: File) => candidateService.uploadAvatar(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function usePersonalInfo() {
  return useQuery({
    queryKey: CANDIDATE_PROFILE_KEYS.personalInfo,
    queryFn: () => candidateService.getPersonalInfo().then((res) => res.data),
  });
}

export function useUpdatePersonalInfo() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: PersonalInfoUpdateRequest) => candidateService.updatePersonalInfo(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.personalInfo });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useCandidateSkills() {
  return useQuery({
    queryKey: CANDIDATE_PROFILE_KEYS.skills,
    queryFn: () => candidateService.getSkills().then((res) => res.data),
  });
}

export function useAddSkill() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SkillAddRequest) => candidateService.addSkill(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.skills });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useRemoveSkill() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (skillId: number) => candidateService.removeSkill(skillId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.skills });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useCandidateExperiences() {
  return useQuery({
    queryKey: CANDIDATE_PROFILE_KEYS.experiences,
    queryFn: () => candidateService.getExperiences().then((res) => res.data),
  });
}

export function useCreateExperience() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: ExperienceUpsertRequest) => candidateService.createExperience(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.experiences });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useUpdateExperience() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ExperienceUpsertRequest }) =>
      candidateService.updateExperience(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.experiences });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useDeleteExperience() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => candidateService.deleteExperience(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.experiences });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useMajors() {
  return useQuery({
    queryKey: CANDIDATE_PROFILE_KEYS.majors,
    queryFn: () => candidateService.getMajors().then((res) => res.data),
  });
}

export function useCandidateEducations() {
  return useQuery({
    queryKey: CANDIDATE_PROFILE_KEYS.educations,
    queryFn: () => candidateService.getEducations().then((res) => res.data),
  });
}

export function useCreateEducation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: EducationUpsertRequest) => candidateService.createEducation(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.educations });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useUpdateEducation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: EducationUpsertRequest }) =>
      candidateService.updateEducation(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.educations });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useDeleteEducation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => candidateService.deleteEducation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.educations });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useCandidateCertifications() {
  return useQuery({
    queryKey: CANDIDATE_PROFILE_KEYS.certifications,
    queryFn: () => candidateService.getCertifications().then((res) => res.data),
  });
}

export function useCreateCertification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CertificationUpsertRequest) => candidateService.createCertification(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.certifications });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useUpdateCertification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CertificationUpsertRequest }) =>
      candidateService.updateCertification(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.certifications });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}

export function useDeleteCertification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => candidateService.deleteCertification(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.certifications });
      queryClient.invalidateQueries({ queryKey: CANDIDATE_PROFILE_KEYS.summary });
    },
  });
}
