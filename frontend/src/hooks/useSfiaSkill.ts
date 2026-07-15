import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { sfiaSkillService } from "@/services/sfia-skill.service";
import {
  CreateSfiaSkillDto,
  UpdateSfiaSkillDto,
} from "@/types/master-data.types";

export const useSfiaSkills = (page: number, pageSize: number, search?: string) => {
  return useQuery({
    queryKey: ["sfiaSkills", page, pageSize, search],
    queryFn: () => sfiaSkillService.getPagedSfiaSkills(page, pageSize, search),
    placeholderData: (previousData) => previousData,
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
