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
import { ChevronLeft, ChevronRight, Briefcase, Building2, Calendar, CheckCircle, Clock, Eye, XCircle, ArrowRight } from 'lucide-react';

export default function AppliedJobsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const [jobs, setJobs] = useState<CandidateAppliedJobDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);

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
        return <Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-emerald-500/10 text-emerald-700">Applied</Badge>;
      case 'VIEWED':
        return <Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-blue-500/10 text-blue-700">Viewed by Employer</Badge>;
      case 'REJECTED':
        return <Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-rose-500/10 text-rose-700">Rejected</Badge>;
      default:
        return <Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-muted text-muted-foreground">{status}</Badge>;
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
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Applied Jobs</h1>
        <p className="text-muted-foreground mt-1">Loading applications...</p>
      </div>
      <div className="flex flex-col gap-3">
        {[1, 2, 3, 4].map((n) => <CardSkeleton key={n} />)}
      </div>
    </div>
  );
  if (isError && jobs.length === 0) return <EmptyState title="Failed to load applied jobs" description="Please try again later." />;

  if (jobs.length === 0) {
    return (
    <div className="w-full pb-8 space-y-6">
        <h1 className="text-2xl font-bold tracking-tight">Applied Jobs</h1>
        <EmptyState 
          title="No applications yet" 
          description="You haven't applied to any jobs. Start searching and applying to land your dream job!"
          icon={<Briefcase className="w-12 h-12 text-slate-300" />}
        >
          <Link href="/candidate/jobs">
            <Button className="mt-4 bg-slate-900 hover:bg-slate-800 text-white">Browse Jobs</Button>
          </Link>
        </EmptyState>
      </div>
    );
  }

  return (
    <div className="w-full pb-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Applied Jobs</h1>
        <p className="text-muted-foreground mt-1">
          You have applied to {totalCount} {totalCount === 1 ? 'job' : 'jobs'}
        </p>
      </div>

      <div className="flex flex-col gap-4">
        {jobs.map((job) => (
          <Card key={job.id} className="group hover:border-primary/50 transition-colors">
            <CardContent className="p-4 flex flex-col gap-3">
              <div className="flex items-center gap-3">
                {job.companyLogoUrl ? (
                  <div className="w-11 h-11 rounded-lg overflow-hidden bg-muted flex items-center justify-center border border-border shrink-0">
                    <img src={job.companyLogoUrl} alt={job.companyName} className="w-full h-full object-contain" />
                  </div>
                ) : (
                  <div className="w-11 h-11 rounded-lg bg-muted flex items-center justify-center border border-border shrink-0 text-slate-400">
                    <Building2 className="w-5 h-5" />
                  </div>
                )}
                
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 min-w-0">
                    <Link href={`/jobs/${job.jobId}`} className="font-semibold text-base text-foreground group-hover:text-primary transition-colors truncate">
                      {job.jobTitle}
                    </Link>
                    {getStatusBadge(job.status)}
                  </div>
                  <p className="text-sm text-muted-foreground truncate">{job.companyName}</p>
                  <div className="flex items-center gap-3 flex-wrap text-xs text-muted-foreground mt-0.5">
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3 shrink-0" />
                      Applied {getRelativeTime(job.applyDate)}
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-1 shrink-0">
                  <Link href={`/jobs/${job.jobId}`} className="flex items-center justify-center">
                    <Button variant="ghost" size="icon" className="h-8 w-8 text-primary hover:text-primary/80 hover:bg-primary/10 transition-colors shrink-0">
                      <ArrowRight className="h-4 w-4 transform group-hover:translate-x-1 transition-transform" />
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
