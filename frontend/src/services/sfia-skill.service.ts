import api from "./api-client";
import {
  SfiaSkillDto,
  CreateSfiaSkillDto,
  UpdateSfiaSkillDto,
} from "@/types/master-data.types";
import { ApiResponse } from "@/types/api.types";

const BASE_URL = "/api/master-data/sfia-skills";

export const sfiaSkillService = {
  getAllSfiaSkills: async (
    search?: string
  ): Promise<ApiResponse<SfiaSkillDto[]>> => {
    const params = new URLSearchParams();
    if (search) params.append("search", search);

    const response = await api.get(`${BASE_URL}?${params.toString()}`);
    return response.data;
  },

  getSfiaSkillById: async (id: string): Promise<ApiResponse<SfiaSkillDto>> => {
    const response = await api.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  createSfiaSkill: async (
    dto: CreateSfiaSkillDto
  ): Promise<ApiResponse<SfiaSkillDto>> => {
    const response = await api.post(BASE_URL, dto);
    return response.data;
  },

  updateSfiaSkill: async (
    id: string,
    dto: UpdateSfiaSkillDto
  ): Promise<ApiResponse<SfiaSkillDto>> => {
    const response = await api.put(`${BASE_URL}/${id}`, dto);
    return response.data;
  },

  deleteSfiaSkill: async (id: string): Promise<ApiResponse<boolean>> => {
    const response = await api.delete(`${BASE_URL}/${id}`);
    return response.data;
  },

  importSfiaSkills: async (file: File): Promise<ApiResponse<number>> => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await api.post(`${BASE_URL}/import`, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
    return response.data;
  },
};
