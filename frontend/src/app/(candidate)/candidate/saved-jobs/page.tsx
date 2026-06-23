'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useSavedJobs } from '@/hooks/useSavedJobs';
import { useJobActions } from '@/hooks/useJobActions';
import { SavedJobCard } from '@/components/shared/SavedJobCard';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight, Bookmark } from 'lucide-react';

export default function SavedJobsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const { data, isLoading, isError } = useSavedJobs(page, pageSize);
  const { unsaveJob, isUnsaving } = useJobActions();

  if (isLoading) return <PageLoader />;
  if (isError) return <EmptyState title="Failed to load saved jobs" description="Please try again later." />;

  const jobs = data?.data || [];
  const meta = data?.meta;

  if (jobs.length === 0) {
    return (
      <div className="py-6 max-w-4xl mx-auto space-y-6">
        <h1 className="text-2xl font-bold tracking-tight">Your Saved Jobs</h1>
        <EmptyState 
          title="No saved jobs yet" 
          description="Keep track of jobs you're interested in by clicking the save icon."
          icon={<Bookmark className="w-12 h-12 text-slate-300" />}
        >
          <Link href="/candidate/jobs">
            <Button className="mt-4">Browse Jobs</Button>
          </Link>
        </EmptyState>
      </div>
    );
  }

  return (
    <div className="py-6 max-w-4xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Your Saved Jobs</h1>
        <p className="text-muted-foreground mt-1">
          You have saved {meta?.totalItems || 0} jobs
        </p>
      </div>

      <div className="flex flex-col gap-4">
        {jobs.map((job) => (
          <SavedJobCard 
            key={job.jobId} 
            job={job} 
            onUnsave={unsaveJob} 
            isUnsaving={isUnsaving} 
          />
        ))}
      </div>

      {/* Pagination */}
      {meta && meta.totalPages > 1 && (
        <div className="flex justify-center items-center gap-4 pt-4">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
          >
            <ChevronLeft className="w-4 h-4 mr-2" /> Previous
          </Button>
          <span className="text-sm font-medium">
            Page {page} of {meta.totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage(p => p + 1)}
            disabled={page === meta.totalPages}
          >
            Next <ChevronRight className="w-4 h-4 ml-2" />
          </Button>
        </div>
      )}
    </div>
  );
}
