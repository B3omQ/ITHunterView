export interface PromptVersionDto {
  id: string;
  promptId: string;
  versionTag: string;
  content: string;
  modelConfig?: string;
  isActive: boolean;
  createdBy: string;
  createdAt: string;
}

export interface PromptDto {
  id: string;
  promptKey: string;
  description?: string;
  activeVersionTag?: string;
  createdAt: string;
  updatedAt?: string;
  versions?: PromptVersionDto[];
}

export interface CreatePromptVersionDto {
  versionTag: string;
  content: string;
  modelConfig?: string;
  makeActive: boolean;
}

export interface CvAnalysisPromptPairDto {
  systemPrompt: PromptDto;
  userPrompt: PromptDto;
}

export interface ActivateCvAnalysisPromptPairDto {
  systemVersionId: string;
  userVersionId: string;
}
