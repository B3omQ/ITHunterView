import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { skillService } from '@/services/skill.service';
import type { SkillStatus } from '@/types/master-data.types';

export function useSkills(params: {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: number;
  status?: SkillStatus;
}) {
  return useQuery({
    queryKey: ['skills', params],
    queryFn: ({ signal }) => skillService.getPagedSkills(params, signal),
  });
}

export function useSkillCategories() {
  return useQuery({
    queryKey: ['skill-categories'],
    queryFn: skillService.getCategories,
  });
}

export function useCreateSkill() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: skillService.createSkill,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['skills'] });
      }
    },
  });
}

export function useUpdateSkill() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: skillService.updateSkill,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['skills'] });
      }
    },
  });
}

export function useUpdateSkillStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: skillService.updateSkillStatus,
    onMutate: async ({ id, dto }) => {
      await queryClient.cancelQueries({ queryKey: ['skills'] });

      // Save previous state to rollback on failure
      const previousQueries = queryClient.getQueriesData<any>({ queryKey: ['skills'] });

      queryClient.setQueriesData<any>({ queryKey: ['skills'] }, (old: any) => {
        if (!old || !old.data || !old.data.items) return old;
        return {
          ...old,
          data: {
            ...old.data,
            items: old.data.items.map((skill: any) =>
              skill.id === id ? { ...skill, status: dto.status } : skill
            ),
          },
        };
      });

      return { previousQueries };
    },
    onError: (err, variables, context) => {
      if (context?.previousQueries) {
        context.previousQueries.forEach(([queryKey, queryData]) => {
          queryClient.setQueryData(queryKey, queryData);
        });
      }
    },
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['skills'] });
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['skills'] });
    },
  });
}

export function useDeleteSkill() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: skillService.deleteSkill,
    onSuccess: (res) => {
      if (res.success) {
        queryClient.invalidateQueries({ queryKey: ['skills'] });
      }
    },
  });
}
