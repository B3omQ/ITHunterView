import { useState, useCallback, useEffect } from 'react';
import { questionBankService } from '@/services/question-bank.service';
import type { QuestionBankDto, CreateQuestionBankDto, UpdateQuestionBankDto } from '@/types/question-bank.types';

export const useQuestionBank = (initialPage = 1, initialPageSize = 10) => {
  const [questions, setQuestions] = useState<QuestionBankDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [categoryId, setCategoryId] = useState<number | ''>('');
  const [industry, setIndustry] = useState<string>('');
  const [level, setLevel] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchQuestions = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const controller = new AbortController();
      const params: any = { page, pageSize };
      if (categoryId !== '') params.categoryId = categoryId;
      if (industry && industry !== 'ALL') params.industry = industry;
      if (level && level !== 'ALL') params.level = level;

      const response = await questionBankService.getPagedQuestions(params, controller.signal);
      if (response.success && response.data) {
        setQuestions(response.data.items || []);
        setTotalCount(response.data.totalCount || 0);
      }
    } catch (err: any) {
      if (err.name !== 'CanceledError') {
        setError(err.message || 'Failed to fetch questions');
      }
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, categoryId, industry, level]);

  useEffect(() => {
    fetchQuestions();
  }, [fetchQuestions]);

  const createQuestion = async (data: CreateQuestionBankDto) => {
    try {
      const response = await questionBankService.createQuestion(data);
      if (response.success) {
        await fetchQuestions();
        return { success: true };
      }
      return { success: false, message: response.message };
    } catch (err: any) {
      return { success: false, message: err.message };
    }
  };

  const importExcel = async (industry: string, level: string, file: File) => {
    try {
      const response = await questionBankService.importExcel(industry, level, file);
      await fetchQuestions();
      return { success: true, importedCount: response.importedCount };
    } catch (err: any) {
      return { success: false, message: err.response?.data?.message || err.message };
    }
  };

  const updateQuestion = async (id: string, data: UpdateQuestionBankDto) => {
    try {
      const response = await questionBankService.updateQuestion(id, data);
      if (response.success) {
        await fetchQuestions();
        return { success: true };
      }
      return { success: false, message: response.message };
    } catch (err: any) {
      return { success: false, message: err.message };
    }
  };

  const deleteQuestion = async (id: string) => {
    try {
      const response = await questionBankService.deleteQuestion(id);
      if (response.success) {
        await fetchQuestions();
        return { success: true };
      }
      return { success: false, message: response.message };
    } catch (err: any) {
      return { success: false, message: err.message };
    }
  };

  return {
    questions,
    totalCount,
    page,
    setPage,
    pageSize,
    setPageSize,
    categoryId,
    setCategoryId,
    industry,
    setIndustry,
    level,
    setLevel,
    loading,
    error,
    createQuestion,
    importExcel,
    updateQuestion,
    deleteQuestion,
    refresh: fetchQuestions
  };
};
