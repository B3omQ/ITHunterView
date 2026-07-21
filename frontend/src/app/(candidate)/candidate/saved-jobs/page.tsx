'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useSavedJobs } from '@/hooks/useSavedJobs';
import { useJobActions } from '@/hooks/useJobActions';
import { SavedJobCard } from '@/components/shared/SavedJobCard';
import { ListPagination } from '@/components/shared/ListPagination';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight, Bookmark } from 'lucide-react';

export default function SavedJobsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const { data, isLoading, isError } = useSavedJobs(page, pageSize);
  const { unsaveJob, isUnsaving } = useJobActions();

  if (isLoading) return (
    <div className="w-full pb-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Your Saved Jobs</h1>
        <p className="text-muted-foreground mt-1">Loading saved jobs...</p>
      </div>
      <div className="flex flex-col gap-3">
        {[1, 2, 3].map(n => <CardSkeleton key={n} />)}
      </div>
    </div>
  );
  if (isError) return <EmptyState title="Failed to load saved jobs" description="Please try again later." />;

  const jobs = data?.data || [];
  const meta = data?.meta;

  if (jobs.length === 0) {
    return (
    <div className="w-full pb-8 space-y-6">
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
    <div className="w-full pb-8 space-y-6">
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
      {meta && (
        <ListPagination page={page} totalPages={meta.totalPages} setPage={setPage} />
      )}
    </div>
  );
}
