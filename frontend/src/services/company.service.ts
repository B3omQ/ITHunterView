import api from './api-client';
import { Company, CreateCompanyDto, VerifyCompanyDto } from '@/types/company.types';

export interface ApiResponse<T> {
  data: T;
  message: string;
  success: boolean;
}

export const companyService = {
  getMyCompany: () => 
    api.get<ApiResponse<Company>>('/api/companies/me').then(res => res.data.data),
    
  createOrUpdateProfile: (dto: CreateCompanyDto) => 
    api.post<ApiResponse<Company>>('/api/companies', dto).then(res => res.data.data),
    
  verifyCompanyLegal: (id: string, dto: VerifyCompanyDto) => 
    api.post<ApiResponse<Company>>(`/api/companies/${id}/verify`, dto).then(res => res.data.data),
};
