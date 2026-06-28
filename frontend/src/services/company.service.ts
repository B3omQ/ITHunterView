import api from './api-client';
import { Company, CreateCompanyDto, UpdateCompanyDto, VerifyCompanyDto, UpdateCompanyStatusDto } from '@/types/company.types';
import { ApiResponse, PaginatedResponse } from '@/types/api.types';

export const companyService = {
  getMyCompany: () => 
    api.get<ApiResponse<Company>>('/api/companies/me').then(res => res.data.data),
    
  createOrUpdateProfile: (dto: CreateCompanyDto) => 
    api.post<ApiResponse<Company>>('/api/companies', dto).then(res => res.data.data),

  updateProfile: (id: string, dto: UpdateCompanyDto) =>
    api.put<ApiResponse<Company>>(`/api/companies/${id}`, dto).then(res => res.data.data),
    
  verifyCompanyLegal: (id: string, dto: VerifyCompanyDto) => 
    api.post<ApiResponse<Company>>(`/api/companies/${id}/verify`, dto).then(res => res.data.data),

  submitUpdateRequest: (id: string, dto: VerifyCompanyDto) => 
    api.post<ApiResponse<Company>>(`/api/companies/${id}/update-request`, dto).then(res => res.data.data),

  getPagedCompanies: (params: { page?: number; pageSize?: number; search?: string; status?: string }) =>
    api.get<ApiResponse<PaginatedResponse<Company>>>('/api/companies', { params }).then(res => res.data.data),

  updateCompanyStatus: ({ id, dto }: { id: string; dto: UpdateCompanyStatusDto }) =>
    api.put<ApiResponse<Company>>(`/api/companies/${id}/status`, dto).then(res => res.data.data),
};
