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
import { ChevronLeft, ChevronRight, Bookmark, Sparkles } from 'lucide-react';
import { useTranslations } from 'next-intl';

export default function SavedJobsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const { data, isLoading, isError } = useSavedJobs(page, pageSize);
  const { unsaveJob, isUnsaving } = useJobActions();
  const t = useTranslations("CandidateSavedJobs");

  if (isLoading) return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('savedJobs')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">{t('loadingSavedJobs')}</p>
        </div>
      </div>
      <div className="flex flex-col gap-3">
        {[1, 2, 3].map(n => <CardSkeleton key={n} />)}
      </div>
    </div>
  );
  if (isError) return <EmptyState title={t('failedToLoadSavedJobs')} description={t('pleaseTryAgainLater')} />;

  const jobs = data?.data || [];
  const meta = data?.meta;

  if (jobs.length === 0) {
    return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('savedJobs')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            {t('keepTrackOfFavorite')}
          </p>
        </div>
      </div>
        <EmptyState 
          title={t('noSavedJobsYet')} 
          description={t('keepTrackByClicking')}
        >
          <Link href="/candidate/jobs">
            <Button className="mt-4">{t('browseJobs')}</Button>
          </Link>
        </EmptyState>
      </div>
    );
  }

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('savedJobs')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            {t('keepTrackOfFavorite')}
          </p>
        </div>
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
