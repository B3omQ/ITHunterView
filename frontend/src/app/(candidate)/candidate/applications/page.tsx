'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { JobApplicationService } from '@/services/job-application.service';
import { CandidateAppliedJobDto } from '@/types/job-application.types';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { ListPagination } from '@/components/shared/ListPagination';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { CompanyLogo } from '@/components/shared/CompanyLogo';
import { ChevronLeft, ChevronRight, Briefcase, Building2, Calendar, CheckCircle, Clock, Eye, XCircle, ArrowRight } from 'lucide-react';
import { useTranslations } from 'next-intl';

export default function AppliedJobsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const [jobs, setJobs] = useState<CandidateAppliedJobDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);
  const t = useTranslations("CandidateApplications");

  useEffect(() => {
    const fetchJobs = async () => {
      setIsLoading(true);
      setIsError(false);
      try {
        const response = await JobApplicationService.getCandidateAppliedJobs(page, pageSize);
        setJobs(response.items || []);
        setTotalCount(response.total || 0);
      } catch (err) {
        console.error("Failed to load applied jobs:", err);
        setIsError(true);
      } finally {
        setIsLoading(false);
      }
    };

    fetchJobs();
  }, [page]);

  const totalPages = Math.ceil(totalCount / pageSize);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'APPLIED':
        return <Badge className="shrink-0 text-xs px-2 py-0.5 border-none font-medium bg-emerald-500/10 text-emerald-700">{t('applied')}</Badge>;
      case 'VIEWED':
        return <Badge className="shrink-0 text-xs px-2 py-0.5 border-none font-medium bg-blue-500/10 text-blue-700">{t('viewedByRecruiter')}</Badge>;
      case 'REJECTED':
        return <Badge className="shrink-0 text-xs px-2 py-0.5 border-none font-medium bg-rose-500/10 text-rose-700">{t('rejected')}</Badge>;
      default:
        return <Badge className="shrink-0 text-xs px-2 py-0.5 border-none font-medium bg-muted text-muted-foreground">{status}</Badge>;
    }
  };

  const getRelativeTime = (dateStr: string) => {
    const rtf = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });
    const diff = (new Date(dateStr).getTime() - new Date().getTime()) / 1000;
    const days = Math.round(diff / (60 * 60 * 24));
    if (Math.abs(days) > 0) return rtf.format(days, 'day');
    
    const hours = Math.round(diff / (60 * 60));
    if (Math.abs(hours) > 0) return rtf.format(hours, 'hour');
    
    const minutes = Math.round(diff / 60);
    return rtf.format(minutes, 'minute');
  };

  if (isLoading && page === 1) return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('appliedJobs')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">{t('loadingApplications')}</p>
        </div>
      </div>
      <div className="flex flex-col gap-3">
        {[1, 2, 3, 4].map((n) => <CardSkeleton key={n} />)}
      </div>
    </div>
  );
  if (isError && jobs.length === 0) return <EmptyState title={t('failedToLoadApplications')} description={t('pleaseTryAgainLater')} />;

  if (jobs.length === 0) {
    return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('appliedJobs')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            {t('trackStatus')}
          </p>
        </div>
      </div>
        <EmptyState 
          title={t('noApplicationsYet')} 
          description={t('startSearchingAndApplying')}
        >
          <Link href="/jobs">
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
          <h1 className="text-3xl font-bold tracking-tight">{t('appliedJobs')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            {t('trackStatus')}
          </p>
        </div>
      </div>

      <div className="flex flex-col gap-4">
          {jobs.map((job) => (
            <Card key={job.id} className="group hover:border-primary/50 transition-colors">
              <CardContent className="flex flex-col gap-3">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <Link href={`/jobs/${job.jobId}`} className="shrink-0">
                      <div className="w-11 h-11 rounded-lg overflow-hidden bg-muted flex items-center justify-center border border-border">
                        <CompanyLogo src={job.companyLogoUrl} alt={job.companyName} fallbackType="building" fallbackIconClassName="text-slate-400 w-5 h-5" />
                      </div>
                    </Link>
                    
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 min-w-0">
                        <Link href={`/jobs/${job.jobId}`} className="font-medium text-base text-foreground group-hover:text-primary transition-colors line-clamp-1 leading-snug">
                          {job.jobTitle}
                        </Link>
                      </div>
                      <p className="text-slate-600 text-sm font-medium line-clamp-1 mt-0.5">{job.companyName}</p>
                      <div className="flex items-center gap-4 flex-wrap mt-1 text-sm text-slate-600">
                        <span className="flex items-center gap-1.5">
                          <Calendar className="h-4 w-4 shrink-0 text-slate-400" />
                          {t('appliedDate', { date: getRelativeTime(job.applyDate) })}
                        </span>
                        <div className="flex items-center">
                          {getStatusBadge(job.status)}
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Action Zone (Right side) */}
                  <div className="flex items-center gap-2 shrink-0">
                    <Link href={`/jobs/${job.jobId}`}>
                      <Button size="sm" variant="outline" className="gap-1.5 h-9">
                        <Eye className="w-4 h-4" /> {t('viewJob')}
                      </Button>
                    </Link>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Pagination */}
        <ListPagination page={page} totalPages={totalPages} setPage={setPage} />
    </div>
  );
}
