'use client';

import React, { useState, Suspense, useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { useCandidateJobs } from '@/hooks/useCandidateJobs';
import { useJobActions } from '@/hooks/useJobActions';
import { JobCard } from '@/components/shared/JobCard';
import { JobSearchFilter } from '@/components/shared/JobSearchFilter';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight, SearchX, MousePointerClick } from 'lucide-react';
import type { JobSearchQuery } from '@/types/job.types';
import { JobDetailPanel } from '@/components/jobs/JobDetailPanel';
import JobDetailModal from '@/components/jobs/JobDetailModal';

function CandidateJobsContent() {
  const searchParams = useSearchParams();
  
  const [query, setQuery] = useState<JobSearchQuery>({ 
    page: 1, 
    pageSize: 10,
    keyword: searchParams.get('query') || undefined,
    location: searchParams.get('location') || undefined
  });

  const { data, isLoading, isError } = useCandidateJobs(query);
  const { saveJob, unsaveJob, isSaving, isUnsaving } = useJobActions();
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [isMobileModalOpen, setIsMobileModalOpen] = useState(false);

  // Auto-select first job on desktop when data loads
  useEffect(() => {
    if (data?.data && data.data.length > 0 && !selectedJobId) {
      if (window.innerWidth >= 1024) {
        setSelectedJobId(data.data[0].id);
      }
    }
  }, [data, selectedJobId]);

  const handleSearch = (newQuery: JobSearchQuery) => {
    setQuery({ ...query, ...newQuery });
    setSelectedJobId(null);
  };

  const handlePageChange = (newPage: number) => {
    setQuery({ ...query, page: newPage });
    setSelectedJobId(null);
  };

  const handleJobClick = (e: React.MouseEvent, jobId: string) => {
    e.preventDefault();
    setSelectedJobId(jobId);
    if (window.innerWidth < 1024) {
      setIsMobileModalOpen(true);
    }
  };

  return (
    <div className="flex flex-col h-[calc(100vh-65px)] bg-slate-50/50">
      {/* Sticky Top Filter */}
      <div className="sticky top-0 z-20 bg-white border-b shadow-sm w-full">
        <div className="container mx-auto p-4 lg:py-4 lg:px-6">
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-4 lg:hidden">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">Job Search</h1>
              <p className="text-muted-foreground text-sm">Find the perfect match for your career.</p>
            </div>
          </div>
          <JobSearchFilter initialQuery={query} onSearch={handleSearch} />
        </div>
      </div>

      {/* Main Split Content */}
      <div className="flex flex-1 overflow-hidden container mx-auto lg:px-6">
        {/* Left Column: Job List */}
        <div className="w-full lg:w-[45%] xl:w-[40%] flex flex-col overflow-y-auto bg-slate-50/50 lg:border-r border-slate-200">
          <div className="p-4 lg:p-6 flex-1">
            <div className="hidden lg:flex mb-6 justify-between items-end">
              <div>
                <h1 className="text-2xl font-bold tracking-tight text-slate-900">Job Search</h1>
                {data?.meta ? (
                  <p className="text-sm text-slate-500 mt-1">
                    Showing {data.data.length} of {data.meta.totalItems} opportunities
                  </p>
                ) : (
                  <p className="text-sm text-slate-500 mt-1">Find the perfect match for your career.</p>
                )}
              </div>
            </div>

            {isLoading ? (
              <div className="py-20"><PageLoader /></div>
            ) : isError ? (
              <EmptyState title="Failed to load jobs" description="Please try again later." icon={<SearchX className="w-12 h-12 text-slate-300" />} />
            ) : data?.data?.length ? (
              <>
                <div className="flex flex-col gap-4">
                  {data.data.map((job) => (
                    <JobCard 
                      key={job.id} 
                      job={job} 
                      isCandidateMode={true} 
                      onSave={saveJob}
                      onUnsave={unsaveJob}
                      isLoadingAction={isSaving || isUnsaving}
                      isActive={selectedJobId === job.id}
                      onClick={(e) => handleJobClick(e, job.id)}
                    />
                  ))}
                </div>

                {/* Pagination */}
                {data.meta.totalPages > 1 && (
                  <div className="flex justify-between items-center mt-8 p-4 bg-white rounded-lg border shadow-sm">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handlePageChange(data.meta.currentPage - 1)}
                      disabled={data.meta.currentPage === 1}
                    >
                      <ChevronLeft className="w-4 h-4 mr-1" /> Prev
                    </Button>
                    <span className="text-sm font-medium text-slate-600">
                      {data.meta.currentPage} / {data.meta.totalPages}
                    </span>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handlePageChange(data.meta.currentPage + 1)}
                      disabled={data.meta.currentPage === data.meta.totalPages}
                    >
                      Next <ChevronRight className="w-4 h-4 ml-1" />
                    </Button>
                  </div>
                )}
              </>
            ) : (
              <EmptyState title="No jobs found" description="Try adjusting your search filters to find more jobs." icon={<SearchX className="w-12 h-12 text-slate-300" />} />
            )}
          </div>
        </div>

        {/* Right Column: Job Detail (Desktop Only) */}
        <div className="hidden lg:flex lg:w-[55%] xl:w-[60%] flex-col overflow-hidden bg-white">
          {selectedJobId ? (
            <div className="h-full overflow-y-auto">
               <JobDetailPanel jobId={selectedJobId} isCandidateMode={true} />
            </div>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center p-8 text-center bg-slate-50/50">
               <div className="w-20 h-20 bg-blue-50 rounded-full flex items-center justify-center mb-6">
                 <MousePointerClick className="w-10 h-10 text-primary" />
               </div>
               <h3 className="text-xl font-semibold text-slate-900 mb-2">Select a job to view details</h3>
               <p className="text-slate-500 max-w-sm">Click on any job card from the list on the left to see the full job description and apply.</p>
            </div>
          )}
        </div>
      </div>

      {/* Mobile Job Detail Modal */}
      <JobDetailModal 
        isOpen={isMobileModalOpen} 
        onClose={() => setIsMobileModalOpen(false)} 
        jobId={selectedJobId || undefined} 
        isCandidateMode={true} 
      />
    </div>
  );
}

export default function CandidateJobsPage() {
  return (
    <Suspense fallback={<PageLoader />}>
      <CandidateJobsContent />
    </Suspense>
  );
}
