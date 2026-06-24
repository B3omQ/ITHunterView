import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { companyService } from '@/services/company.service';
import { CreateCompanyDto, VerifyCompanyDto, UpdateCompanyStatusDto } from '@/types/company.types';

export function useGetMyCompany() {
  return useQuery({
    queryKey: ['my-company'],
    queryFn: companyService.getMyCompany,
    retry: false, // Don't retry if not found
  });
}

export function useCreateOrUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateCompanyDto) => companyService.createOrUpdateProfile(dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-company'] });
    },
  });
}

export function useVerifyCompanyLegal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: VerifyCompanyDto }) => 
      companyService.verifyCompanyLegal(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-company'] });
    },
  });
}

export function useSubmitCompanyUpdateRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: VerifyCompanyDto }) => 
      companyService.submitUpdateRequest(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-company'] });
    },
  });
}

export function useCompanies(params: { page?: number; pageSize?: number; search?: string; status?: string }) {
  return useQuery({
    queryKey: ['companies', params],
    queryFn: () => companyService.getPagedCompanies(params),
  });
}

export function useUpdateCompanyStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateCompanyStatusDto }) =>
      companyService.updateCompanyStatus({ id, dto }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['companies'] });
      queryClient.invalidateQueries({ queryKey: ['my-company'] });
    },
  });
}
