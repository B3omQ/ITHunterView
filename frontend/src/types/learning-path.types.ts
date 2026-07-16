export interface CandidateSfiaSkillDto {
  skillCode: string;
  currentLevel: number;
}

export interface GeneratePathRequest {
  targetRoleTemplateId: string;
  currentSkills: CandidateSfiaSkillDto[];
  personalContext?: string;
}

export interface ExtractSfiaProfileResponse {
  targetRoleTemplateId: string;
  currentSkills: CandidateSfiaSkillDto[];
}

export interface HistoryContextPreviewDto {
  contextPreview: string;
}

export interface TargetRoleSkillDto {
  skillCode: string;
  skillName: string;
  description?: string;
  availableLevels?: string;
  targetLevel: number;
}

export interface TargetRoleResponseDto {
  id: string;
  roleName: string;
  description: string;
  requiredSkills: TargetRoleSkillDto[];
}

export interface SfiaTarget {
  skill_code: string;
  from_level: number;
  to_level: number;
}

export interface LearningTask {
  task_index: number;
  title: string;
  description: string;
  estimated_hours?: number;
  completed?: boolean;
}

export interface LearningTask {
  title: string;
  description: string;
  completed?: boolean;
}

export interface LearningModule {
  module_index: number;
  title: string;
  description: string;
  sfia_target?: SfiaTarget;
  tasks: LearningTask[];
  completed?: boolean;
}

export interface GapSkill {
  skill_code: string;
  skill_name: string;
  current_level: number;
  target_level: number;
  gap_delta: number;
}

export interface GapSummary {
  total_gaps: number;
  gaps: GapSkill[];
}

export interface TargetProfile {
  role_name: string;
  description: string;
}

export interface PathProgress {
  total_modules: number;
  completed_modules: number;
  total_tasks: number;
  completed_tasks: number;
  percentage: number;
}

export interface PathData {
  title: string;
  target_profile?: TargetProfile;
  gap_summary?: GapSummary;
  modules: LearningModule[];
  progress?: PathProgress;
}

export interface LearningPath {
  id: string;
  candidateId: string;
  sessionId: string | null;
  title: string;
  status: string;
  pathData: PathData;
  createdAt: string;
}

export interface HistoryContextPreviewDto {
  contextPreview: string;
}
