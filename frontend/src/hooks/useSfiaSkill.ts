import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { sfiaSkillService } from "@/services/sfia-skill.service";
import {
  CreateSfiaSkillDto,
  UpdateSfiaSkillDto,
} from "@/types/master-data.types";

export const useSfiaSkills = (search?: string) => {
  return useQuery({
    queryKey: ["sfiaSkills", search],
    queryFn: () => sfiaSkillService.getAllSfiaSkills(search),
    placeholderData: (previousData) => previousData,
  });
};

export const useSfiaSkill = (id: string) => {
  return useQuery({
    queryKey: ["sfiaSkill", id],
    queryFn: () => sfiaSkillService.getSfiaSkillById(id),
    enabled: !!id,
  });
};

export const useCreateSfiaSkill = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateSfiaSkillDto) => sfiaSkillService.createSfiaSkill(dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sfiaSkills"] });
    },
  });
};

export const useUpdateSfiaSkill = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateSfiaSkillDto }) =>
      sfiaSkillService.updateSfiaSkill(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sfiaSkills"] });
    },
  });
};

export const useDeleteSfiaSkill = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => sfiaSkillService.deleteSfiaSkill(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sfiaSkills"] });
    },
  });
};

export const useImportSfiaSkills = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => sfiaSkillService.importSfiaSkills(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sfiaSkills"] });
    },
  });
};
