'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { JobApplicationService } from '@/services/job-application.service';
import { CandidateAppliedJobDto } from '@/types/job-application.types';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { ChevronLeft, ChevronRight, Briefcase, Building2, Calendar, CheckCircle, Clock, Eye, XCircle } from 'lucide-react';

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
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-blue-50 text-blue-700"><CheckCircle className="w-3.5 h-3.5" /> Applied</span>;
      case 'VIEWED':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-indigo-50 text-indigo-700"><Eye className="w-3.5 h-3.5" /> Viewed by Employer</span>;
      case 'REJECTED':
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-rose-50 text-rose-700"><XCircle className="w-3.5 h-3.5" /> Rejected</span>;
      default:
        return <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-zinc-100 text-zinc-700"><Clock className="w-3.5 h-3.5" /> {status}</span>;
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

  if (isLoading && page === 1) return <PageLoader />;
  if (isError && jobs.length === 0) return <EmptyState title="Failed to load applied jobs" description="Please try again later." />;

  if (jobs.length === 0) {
    return (
      <div className="py-6 max-w-4xl mx-auto space-y-6">
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
    <div className="py-6 max-w-4xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Applied Jobs</h1>
        <p className="text-muted-foreground mt-1">
          You have applied to {totalCount} {totalCount === 1 ? 'job' : 'jobs'}
        </p>
      </div>

      <div className="flex flex-col gap-4">
        {jobs.map((job) => (
          <Card key={job.id} className="overflow-hidden hover:shadow-md transition-shadow">
            <CardContent className="p-0">
              <div className="flex flex-col sm:flex-row p-6 gap-6 items-start sm:items-center">
                {job.companyLogoUrl ? (
                  <div className="w-16 h-16 rounded-md bg-white border border-zinc-100 flex items-center justify-center shrink-0 overflow-hidden">
                    <img src={job.companyLogoUrl} alt={job.companyName} className="max-w-full max-h-full object-contain" />
                  </div>
                ) : (
                  <div className="w-16 h-16 rounded-md bg-zinc-100 flex items-center justify-center shrink-0 text-zinc-400">
                    <Building2 className="w-8 h-8" />
                  </div>
                )}
                
                <div className="flex-grow space-y-1">
                  <Link href={`/jobs/${job.jobId}`} className="text-lg font-bold text-zinc-900 hover:text-blue-600 transition-colors line-clamp-1">
                    {job.jobTitle}
                  </Link>
                  <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-zinc-500">
                    <div className="flex items-center gap-1.5 font-medium">
                      <Building2 className="w-4 h-4" />
                      {job.companyName}
                    </div>
                    <div className="flex items-center gap-1.5">
                      <Calendar className="w-4 h-4" />
                      Applied {getRelativeTime(job.applyDate)}
                    </div>
                  </div>
                </div>

                <div className="shrink-0 flex flex-col sm:items-end gap-3 mt-2 sm:mt-0 w-full sm:w-auto">
                  {getStatusBadge(job.status)}
                  <Link href={`/jobs/${job.jobId}`} className="w-full sm:w-auto">
                    <Button variant="outline" size="sm" className="w-full sm:w-auto">View Job</Button>
                  </Link>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
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
            Page {page} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage(p => p + 1)}
            disabled={page === totalPages}
          >
            Next <ChevronRight className="w-4 h-4 ml-2" />
          </Button>
        </div>
      )}
    </div>
  );
}
