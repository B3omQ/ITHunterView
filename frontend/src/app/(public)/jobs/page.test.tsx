import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import PublicJobsPage from './page';
import CandidateJobsPage from '@/app/(candidate)/candidate/jobs/page';
import { usePublicJobs } from '@/hooks/usePublicJobs';
import { useCandidateJobs } from '@/hooks/useCandidateJobs';
import { useSearchParams } from 'next/navigation';

vi.mock('next/navigation', () => ({
  useSearchParams: vi.fn(),
  useRouter: vi.fn(() => ({
    replace: vi.fn(),
    push: vi.fn(),
  })),
  usePathname: vi.fn(() => '/jobs'),
}));

vi.mock('next-intl', () => ({
  useTranslations: vi.fn(() => (key: string) => key),
}));

vi.mock('@/store/auth.store', () => ({
  useAuthStore: vi.fn(() => ({ user: null })),
}));

vi.mock('@/hooks/usePublicJobs', () => ({
  usePublicJobs: vi.fn(),
}));

vi.mock('@/hooks/useCandidateJobs', () => ({
  useCandidateJobs: vi.fn(),
}));

vi.mock('@/hooks/useSignalR', () => ({
  useSignalR: vi.fn(() => null),
}));

vi.mock('@/components/jobs/JobSearchFilter', () => ({
  JobSearchFilter: () => <div data-testid="job-search-filter" />,
}));

vi.mock('@/components/shared/JobCard', () => ({
  JobCard: () => <div data-testid="job-card" />,
}));

vi.mock('@/components/jobs/JobDetailPanel', () => ({
  JobDetailPanel: () => <div data-testid="job-detail-panel" />,
}));

vi.mock('@/components/jobs/JobDetailModal', () => ({
  default: () => <div data-testid="job-detail-modal" />,
}));

describe('Public Jobs Page Search Query Mapping and Parity', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(usePublicJobs).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
    } as unknown as ReturnType<typeof usePublicJobs>);
    vi.mocked(useCandidateJobs).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as unknown as ReturnType<typeof useCandidateJobs>);
  });

  it('FILTER-02: Public page forwards includeNegotiable from URL query params', () => {
    const params = new URLSearchParams({
      page: '3',
      jobExpertises: 'Backend,Data',
      levels: 'Senior',
      minSalary: '100',
      maxSalary: '150',
      includeNegotiable: 'true',
    });

    vi.mocked(useSearchParams).mockReturnValue(
      params as unknown as ReturnType<typeof useSearchParams>,
    );

    render(<PublicJobsPage />);

    expect(usePublicJobs).toHaveBeenCalledWith(
      expect.objectContaining({
        page: 3,
        pageSize: 10,
        jobExpertises: ['Backend', 'Data'],
        levels: ['Senior'],
        minSalary: 100,
        maxSalary: 150,
        includeNegotiable: true,
      })
    );
  });

  it('FILTER-03: Candidate and Public pages produce identical query payload for same URL params', () => {
    const fullParams = new URLSearchParams({
      query: 'dotnet',
      location: 'Ho Chi Minh',
      skill: 'C#',
      companyName: 'TechCorp',
      minSalary: '100',
      maxSalary: '150',
      levels: 'Senior,Lead',
      workingModels: 'Remote,Hybrid',
      jobDomains: 'Fintech,Ecommerce',
      jobExpertises: 'Backend,Data',
      companyIndustries: 'Software',
      companyTypes: 'Product',
      postedWithinDays: '7',
      includeNegotiable: 'true',
      page: '2',
    });

    vi.mocked(useSearchParams).mockReturnValue(
      fullParams as unknown as ReturnType<typeof useSearchParams>,
    );

    render(<PublicJobsPage />);
    const publicQuery = vi.mocked(usePublicJobs).mock.calls[0][0];

    render(<CandidateJobsPage />);
    const candidateQuery = vi.mocked(useCandidateJobs).mock.calls[0][0];

    expect(candidateQuery).toEqual(publicQuery);
  });
});
