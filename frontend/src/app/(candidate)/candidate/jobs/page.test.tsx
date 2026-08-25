import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import CandidateJobsPage from './page';
import { useCandidateJobs } from '@/hooks/useCandidateJobs';
import { useSearchParams } from 'next/navigation';

vi.mock('next/navigation', () => ({
  useSearchParams: vi.fn(),
  useRouter: vi.fn(() => ({
    replace: vi.fn(),
    push: vi.fn(),
  })),
  usePathname: vi.fn(() => '/candidate/jobs'),
}));

vi.mock('next-intl', () => ({
  useTranslations: vi.fn(() => (key: string) => key),
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

describe('Candidate Jobs Page Search Query Mapping', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useCandidateJobs).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as unknown as ReturnType<typeof useCandidateJobs>);
  });

  it('FILTER-01: Candidate page forwards both jobExpertises and includeNegotiable from URL query params', () => {
    // URL with jobExpertises and includeNegotiable
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

    render(<CandidateJobsPage />);

    expect(useCandidateJobs).toHaveBeenCalledWith(
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
});
