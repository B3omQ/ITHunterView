'use client';

import React, { Suspense, useEffect, useState } from 'react';
import { useSearchParams, useRouter, usePathname } from 'next/navigation';
import { usePublicJobs } from '@/hooks/usePublicJobs';
import { JobCard } from '@/components/shared/JobCard';
import { JobCardSkeleton } from '@/components/jobs/JobCardSkeleton';
import { JobSearchFilter } from '@/components/jobs/JobSearchFilter';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight, SearchX, MousePointerClick } from 'lucide-react';
import type { JobSearchQuery } from '@/types/job.types';
import { JobDetailPanel } from '@/components/jobs/JobDetailPanel';
import JobDetailModal from '@/components/jobs/JobDetailModal';

function PublicJobsContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  
  const parseArray = (param: string | null) => param ? param.split(',').filter(Boolean) : undefined;

  const query: JobSearchQuery = { 
    page: parseInt(searchParams.get('page') || '1', 10), 
    pageSize: 10,
    keyword: searchParams.get('query') || undefined,
    location: searchParams.get('location') || undefined,
    jobType: searchParams.get('jobType') || undefined,
    skill: searchParams.get('skill') || undefined,
    companyName: searchParams.get('companyName') || undefined,
    minSalary: searchParams.get('minSalary') ? parseFloat(searchParams.get('minSalary') as string) : undefined,
    maxSalary: searchParams.get('maxSalary') ? parseFloat(searchParams.get('maxSalary') as string) : undefined,
    levels: parseArray(searchParams.get('levels')),
    workingModels: parseArray(searchParams.get('workingModels')),
    jobDomains: parseArray(searchParams.get('jobDomains')),
    companyIndustries: parseArray(searchParams.get('companyIndustries')),
    companyTypes: parseArray(searchParams.get('companyTypes')),
    postedWithinDays: searchParams.get('postedWithinDays') ? parseInt(searchParams.get('postedWithinDays') as string, 10) : undefined,
  };

  const { data, isLoading, isError } = usePublicJobs(query);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [isMobileModalOpen, setIsMobileModalOpen] = useState(false);

  // Auto-select first job on desktop when data loads
  useEffect(() => {
    if (data?.data && data.data.length > 0 && !selectedJobId) {
      // Only auto-select if window is desktop size to avoid unnecessary requests on mobile
      if (window.innerWidth >= 1024) {
        setSelectedJobId(data.data[0].id);
      }
    }
  }, [data, selectedJobId]);

  const handlePageChange = (newPage: number) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set('page', newPage.toString());
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
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
      <div className="sticky top-0 z-20 bg-white border-b border-slate-200 w-full">
        <div className="container mx-auto p-4 lg:py-4 lg:px-6">
          <JobSearchFilter />
        </div>
      </div>

      {/* Main Split Content */}
      <div className="flex flex-1 overflow-hidden container mx-auto lg:px-6">
        {/* Left Column: Job List */}
        <div className="w-full lg:w-[45%] xl:w-[40%] flex flex-col overflow-y-auto bg-slate-50/50 lg:border-r border-slate-200">
          <div className="p-4 lg:p-6 flex-1">
            <div className="mb-6 flex justify-between items-end">
              <div>
                <h1 className="text-2xl font-bold tracking-tight text-slate-900">Explore Jobs</h1>
                {data?.meta && (
                  <p className="text-sm text-slate-500 mt-1">
                    Showing {data.data.length} of {data.meta.totalItems} opportunities
                  </p>
                )}
              </div>
            </div>

            {isLoading ? (
              <div className="flex flex-col gap-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <JobCardSkeleton key={i} />
                ))}
              </div>
            ) : isError ? (
              <EmptyState title="Failed to load jobs" description="Please try again later." icon={<SearchX className="w-12 h-12 text-slate-300" />} />
            ) : data?.data?.length ? (
              <>
                <div className="flex flex-col gap-4">
                  {data.data.map((job) => (
                    <JobCard 
                      key={job.id} 
                      job={job} 
                      isCandidateMode={false} 
                      isActive={selectedJobId === job.id}
                      onClick={(e) => handleJobClick(e, job.id)}
                    />
                  ))}
                </div>

                {/* Pagination */}
                {data.meta.totalPages > 1 && (
                  <div className="flex justify-between items-center mt-8 p-4 bg-white rounded-lg border border-slate-200">
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
              <EmptyState title="No jobs found" description="Try adjusting your search filters." icon={<SearchX className="w-12 h-12 text-slate-300" />} />
            )}
          </div>
        </div>

        {/* Right Column: Job Detail (Desktop Only) */}
        <div className="hidden lg:flex lg:w-[55%] xl:w-[60%] flex-col overflow-hidden bg-white">
          {selectedJobId ? (
            <div className="h-full overflow-y-auto">
               <JobDetailPanel jobId={selectedJobId} isCandidateMode={false} />
            </div>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center p-8 text-center bg-slate-50/50">
               <div className="w-20 h-20 bg-slate-100 rounded-full flex items-center justify-center mb-6">
                 <MousePointerClick className="w-10 h-10 text-slate-500" />
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
        isCandidateMode={false} 
      />
    </div>
  );
}

export default function PublicJobsPage() {
  return (
    <Suspense fallback={<PageLoader />}>
      <PublicJobsContent />
    </Suspense>
  );
}
