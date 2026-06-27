import { useState, useEffect, useCallback } from 'react';
import {
  recruiterService,
  JobPosting,
  JobPostingSummary,
  JobCategory,
  Skill,
  CreateJobPostingDto,
  UpdateJobPostingDto,
} from '@/services/recruiter.service';

// Hook to manage job postings list with filters, searching, and pagination
export function useJobs(initialPage = 1, initialPageSize = 7, initialStatus = 'ALL') {
  const [jobs, setJobs] = useState<JobPostingSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(initialPage);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [status, setStatus] = useState(initialStatus);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // Debounce search input
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1); // Reset to first page on search
    }, 400);
    return () => clearTimeout(handler);
  }, [search]);

  const fetchJobs = useCallback(async () => {
    setLoading(true);
    setError('');
    const res = await recruiterService.getJobs(page, initialPageSize, status, debouncedSearch);
    if (res.success && res.data) {
      if (res.data.success) {
        setJobs(res.data.data.items || []);
        setTotalCount(res.data.data.totalCount || 0);
      } else {
        setError(res.data.message || 'Failed to fetch jobs');
      }
    } else {
      setError(res.message || 'Error occurred while loading jobs');
    }
    setLoading(false);
  }, [page, initialPageSize, status, debouncedSearch]);

  useEffect(() => {
    fetchJobs();
  }, [fetchJobs]);

  const closeJob = useCallback(async (id: string) => {
    const res = await recruiterService.closeJob(id);
    if (res.success && res.data && res.data.success) {
      await fetchJobs();
      return { success: true };
    }
    return {
      success: false,
      message: res.data?.message || res.message || 'Failed to close job',
    };
  }, [fetchJobs]);

  return {
    jobs,
    totalCount,
    page,
    setPage,
    search,
    setSearch,
    status,
    setStatus,
    loading,
    error,
    refresh: fetchJobs,
    closeJob,
  };
}

// Hook to manage single job detail fetching and updating
export function useJobDetails(jobId?: string) {
  const [job, setJob] = useState<JobPosting | null>(null);
  const [loading, setLoading] = useState(!!jobId);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const fetchJobDetails = useCallback(async () => {
    if (!jobId) return;
    setLoading(true);
    setError('');
    const res = await recruiterService.getJobById(jobId);
    if (res.success && res.data) {
      if (res.data.success) {
        setJob(res.data.data);
      } else {
        setError(res.data.message || 'Failed to load job details');
      }
    } else {
      setError(res.message || 'Error fetching details from server');
    }
    setLoading(false);
  }, [jobId]);

  useEffect(() => {
    fetchJobDetails();
  }, [fetchJobDetails]);

  const createJob = useCallback(async (payload: CreateJobPostingDto) => {
    setSaving(true);
    setError('');
    const res = await recruiterService.createJob(payload);
    setSaving(false);
    if (res.success && res.data && res.data.success) {
      return { success: true, data: res.data.data };
    }
    return {
      success: false,
      message: res.data?.message || res.message || 'Failed to create job posting',
    };
  }, []);

  const updateJob = useCallback(async (payload: UpdateJobPostingDto) => {
    if (!jobId) return { success: false, message: 'Job ID is required for updating' };
    setSaving(true);
    setError('');
    const res = await recruiterService.updateJob(jobId, payload);
    setSaving(false);
    if (res.success && res.data && res.data.success) {
      return { success: true, data: res.data.data };
    }
    return {
      success: false,
      message: res.data?.message || res.message || 'Failed to update job posting',
    };
  }, [jobId]);

  return {
    job,
    loading,
    saving,
    error,
    setError,
    refresh: fetchJobDetails,
    createJob,
    updateJob,
  };
}

// Hook to load job categories and skills list (metadata)
export function useJobMetadata() {
  const [categories, setCategories] = useState<JobCategory[]>([]);
  const [availableSkills, setAvailableSkills] = useState<Skill[]>([]);
  const [majors, setMajors] = useState<{ id: number; name: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchMetadata = async () => {
      setLoading(true);
      setError('');
      try {
        const [catRes, skillRes, majorRes] = await Promise.all([
          recruiterService.getCategories(),
          recruiterService.getSkills(),
          recruiterService.getMajors(),
        ]);

        if (catRes.success && catRes.data?.success) {
          setCategories(catRes.data.data || []);
        } else {
          setError(prev => prev || catRes.message || 'Failed to load categories');
        }

        if (skillRes.success && skillRes.data?.success) {
          setAvailableSkills(skillRes.data.data || []);
        } else {
          setError(prev => prev || skillRes.message || 'Failed to load skills');
        }

        if (majorRes.success && majorRes.data?.success) {
          setMajors(majorRes.data.data.items || []);
        } else {
          setError(prev => prev || majorRes.message || 'Failed to load majors');
        }
      } catch (err: any) {
        setError(err.message || 'Error occurred while loading metadata');
      } finally {
        setLoading(false);
      }
    };

    fetchMetadata();
  }, []);

  return {
    categories,
    availableSkills,
    majors,
    loading,
    error,
  };
}
