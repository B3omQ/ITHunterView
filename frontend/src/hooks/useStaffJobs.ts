import { useState, useCallback } from 'react';
import { staffJobService, PagedResult } from '@/services/staff-job.service';
import { JobPostingSummary } from '@/services/recruiter.service';

export const useStaffJobs = () => {
  const [data, setData] = useState<PagedResult<JobPostingSummary>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 10
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchJobs = useCallback(async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    status?: string
  ) => {
    setLoading(true);
    setError(null);
    try {
      const response = await staffJobService.getJobs(page, pageSize, search, status);
      if (response.success && response.data) {
        setData(response.data);
      } else {
        setError(response.message || 'Lỗi lấy danh sách bài đăng');
      }
    } catch (err: any) {
      setError(err.message || 'Lỗi lấy danh sách bài đăng');
    } finally {
      setLoading(false);
    }
  }, []);

  const banJob = async (id: string, reason: string) => {
    const response = await staffJobService.banJob(id, reason);
    return response;
  };

  const unbanJob = async (id: string) => {
    const response = await staffJobService.unbanJob(id);
    return response;
  };

  const deleteSeedJobs = async () => {
    const response = await staffJobService.deleteSeedJobs();
    return response;
  };

  return {
    data,
    loading,
    error,
    fetchJobs,
    banJob,
    unbanJob,
    deleteSeedJobs
  };
};
