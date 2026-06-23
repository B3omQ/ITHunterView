'use client';

import React, { useState } from 'react';
import { usePublicJobs } from '@/hooks/usePublicJobs';
import { JobCard } from '@/components/shared/JobCard';
import { JobSearchFilter } from '@/components/shared/JobSearchFilter';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight, SearchX } from 'lucide-react';
import type { JobSearchQuery } from '@/types/job.types';

export default function PublicJobsPage() {
  const [query, setQuery] = useState<JobSearchQuery>({ page: 1, pageSize: 9 });
  const { data, isLoading, isError } = usePublicJobs(query);

  const handleSearch = (newQuery: JobSearchQuery) => {
    setQuery({ ...query, ...newQuery });
  };

  const handlePageChange = (newPage: number) => {
    setQuery({ ...query, page: newPage });
  };

  return (
    <div className="container mx-auto py-8 max-w-6xl">
      <div className="mb-8 text-center">
        <h1 className="text-3xl font-bold tracking-tight mb-2">Discover Your Next Opportunity</h1>
        <p className="text-muted-foreground">Search through thousands of top tech jobs in Vietnam</p>
      </div>

      <div className="mb-8">
        <JobSearchFilter initialQuery={query} onSearch={handleSearch} />
      </div>

      {isLoading ? (
        <PageLoader />
      ) : isError ? (
        <EmptyState title="Failed to load jobs" description="Please try again later." icon={<SearchX className="w-12 h-12 text-slate-300" />} />
      ) : data?.data?.length ? (
        <>
          <div className="mb-4 text-sm text-muted-foreground">
            Showing {data.data.length} of {data.meta.totalItems} jobs
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {data.data.map((job) => (
              <JobCard key={job.id} job={job} isCandidateMode={false} />
            ))}
          </div>

          {/* Pagination */}
          {data.meta.totalPages > 1 && (
            <div className="flex justify-center items-center gap-4 mt-8">
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(data.meta.currentPage - 1)}
                disabled={data.meta.currentPage === 1}
              >
                <ChevronLeft className="w-4 h-4 mr-2" /> Previous
              </Button>
              <span className="text-sm">
                Page {data.meta.currentPage} of {data.meta.totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(data.meta.currentPage + 1)}
                disabled={data.meta.currentPage === data.meta.totalPages}
              >
                Next <ChevronRight className="w-4 h-4 ml-2" />
              </Button>
            </div>
          )}
        </>
      ) : (
        <EmptyState title="No jobs found" description="Try adjusting your search filters to find more jobs." icon={<SearchX className="w-12 h-12 text-slate-300" />} />
      )}
    </div>
  );
}
