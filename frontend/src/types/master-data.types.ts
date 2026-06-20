export type SkillStatus = 'ACTIVE' | 'DEACTIVE';

export interface SkillCategoryDto {
  id: number;
  name: string;
}

export interface SkillDto {
  id: number;
  categoryId?: number;
  categoryName: string;
  name: string;
  status: SkillStatus;
  createdBy?: string;
  updatedBy?: string;
}

export interface CreateSkillDto {
  name: string;
  categoryId: number;
  status?: SkillStatus;
}

export interface UpdateSkillDto {
  name: string;
  categoryId: number;
  status: SkillStatus;
}

export interface UpdateSkillStatusDto {
  status: SkillStatus;
  force: boolean;
}

export interface MajorDto {
  id: number;
  name: string;
  code: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface CreateMajorDto {
  name: string;
  code: string;
}

export interface UpdateMajorDto {
  name: string;
  code: string;
}
