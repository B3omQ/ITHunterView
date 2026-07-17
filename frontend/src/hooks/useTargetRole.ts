import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { targetRoleService } from '@/services/target-role.service';
import type { 
  CreateTargetRoleTemplateDto, 
  UpdateTargetRoleTemplateDto 
} from '@/types/master-data.types';

export const TARGET_ROLE_KEYS = {
  all: ['target-roles'] as const,
  paged: (page: number, pageSize: number, search?: string) => 
    [...TARGET_ROLE_KEYS.all, 'paged', page, pageSize, search] as const,
  sfiaSkills: ['sfia-skills'] as const,
};

export const usePagedTargetRoles = ({
  page,
  pageSize,
  search,
}: {
  page: number;
  pageSize: number;
  search?: string;
}) => {
  return useQuery({
    queryKey: TARGET_ROLE_KEYS.paged(page, pageSize, search),
    queryFn: () => targetRoleService.getPagedRoles(page, pageSize, search),
    placeholderData: (previousData) => previousData, // keep previous data while fetching new page
  });
};

export const useAllSfiaSkills = () => {
  return useQuery({
    queryKey: TARGET_ROLE_KEYS.sfiaSkills,
    queryFn: () => targetRoleService.getAllSfiaSkills(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useCreateTargetRole = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: CreateTargetRoleTemplateDto) => targetRoleService.createRole(dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: TARGET_ROLE_KEYS.all });
    },
  });
};

export const useUpdateTargetRole = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateTargetRoleTemplateDto }) => 
      targetRoleService.updateRole(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: TARGET_ROLE_KEYS.all });
    },
  });
};

export const useDeleteTargetRole = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => targetRoleService.deleteRole(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: TARGET_ROLE_KEYS.all });
    },
  });
};

export const useImportTargetRoles = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (file: File) => targetRoleService.importTargetRoles(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: TARGET_ROLE_KEYS.all });
    },
  });
};
