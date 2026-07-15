import api from "./api-client";
import {
  SfiaSkillDto,
  CreateSfiaSkillDto,
  UpdateSfiaSkillDto,
} from "@/types/master-data.types";
import { PagedResponse, ResponseBase } from "@/types/common.types";

const BASE_URL = "/api/master-data/sfia-skills";

export const sfiaSkillService = {
  getAllSfiaSkills: async (
    search?: string
  ): Promise<ResponseBase<SfiaSkillDto[]>> => {
    const params = new URLSearchParams();
    if (search) params.append("search", search);

    const response = await api.get(`${BASE_URL}?${params.toString()}`);
    return response.data;
  },

  createSfiaSkill: async (
    dto: CreateSfiaSkillDto
  ): Promise<ResponseBase<SfiaSkillDto>> => {
    const response = await api.post(BASE_URL, dto);
    return response.data;
  },

  updateSfiaSkill: async (
    id: string,
    dto: UpdateSfiaSkillDto
  ): Promise<ResponseBase<SfiaSkillDto>> => {
    const response = await api.put(`${BASE_URL}/${id}`, dto);
    return response.data;
  },

  deleteSfiaSkill: async (id: string): Promise<ResponseBase<boolean>> => {
    const response = await api.delete(`${BASE_URL}/${id}`);
    return response.data;
  },

  importSfiaSkills: async (file: File): Promise<ResponseBase<number>> => {
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
