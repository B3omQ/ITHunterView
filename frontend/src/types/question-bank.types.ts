export interface QuestionBankDto {
  id: string;
  categoryId?: number;
  industry: string;
  level: string;
  questionText: string;
}

export interface CreateQuestionBankDto {
  categoryId?: number;
  industry: string;
  level: string;
  questionText: string;
}

export interface UpdateQuestionBankDto {
  categoryId?: number;
  industry: string;
  level: string;
  questionText: string;
}
