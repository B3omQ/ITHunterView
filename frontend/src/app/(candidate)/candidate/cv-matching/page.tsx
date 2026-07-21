'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useGetMatchHistory, useDeleteMatchHistory } from '@/hooks/useCvMatch';
import { APP_ROUTES } from '@/lib/constants';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ListPagination } from '@/components/shared/ListPagination';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
import { EmptyState } from '@/components/shared/EmptyState';
import { Loader2, FileText, Briefcase, Eye, Plus, Trash2, Activity, Calendar, ChevronLeft, ChevronRight } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogClose,
} from '@/components/ui/dialog';

export default function CvMatchingHistoryPage() {
  const router = useRouter();
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data: response, isLoading } = useGetMatchHistory(page, pageSize);
  const deleteMutation = useDeleteMatchHistory();
  const historyData = response?.data;
  const items = historyData?.items || [];
  const totalCount = historyData?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handlePageChange = (newPage: number) => {
    if (newPage > 0 && newPage <= totalPages) {
      setPage(newPage);
    }
  };

  const navigateToDetail = (jobId: string) => {
    router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${jobId}`);
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Completed':
        return <Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-emerald-500/10 text-emerald-700">Completed</Badge>;
      case 'Failed':
        return <Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-rose-500/10 text-rose-700">Failed</Badge>;
      default:
        return <Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-amber-500/10 text-amber-700">{status}</Badge>;
    }
  };

  const getScoreColor = (score: number) => {
    if (score >= 70) return 'text-emerald-700 bg-emerald-500/10 border-emerald-500/20';
    if (score >= 40) return 'text-amber-700 bg-amber-500/10 border-amber-500/20';
    return 'text-rose-700 bg-rose-500/10 border-rose-500/20';
  };

  if (isLoading) return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">CV Matching History</h1>
          <p className="text-muted-foreground mt-1 max-w-2xl">
            Loading your match history...
          </p>
        </div>
      </div>
      <div className="flex flex-col gap-3">
        {[1, 2, 3, 4].map(n => <CardSkeleton key={n} />)}
      </div>
    </div>
  );

  if (items.length === 0) {
    return (
      <div className="w-full pb-8 space-y-6">
        <h1 className="text-2xl font-bold tracking-tight">CV-JD Matching History</h1>
        <EmptyState 
          title="No matches found" 
          description="You haven't run any CV-JD matches yet. Start by creating a new match to see how well your resume fits job descriptions."
          icon={<Activity className="w-12 h-12 text-slate-300" />}
        >
          <Link href={`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new`}>
            <Button className="mt-4 gap-2">
              <Plus className="h-4 w-4" /> Create First Match
            </Button>
          </Link>
        </EmptyState>
      </div>
    );
  }

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">CV-JD Matching History</h1>
          <p className="text-sm text-muted-foreground mt-1">
            You have {totalCount} matching records
          </p>
        </div>
        <Button onClick={() => router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new`)} className="gap-2">
          <Plus className="h-4 w-4" />
          New Match
        </Button>
      </div>

      <div className="flex flex-col gap-3">
        {items.map((item) => (
          <Card key={item.jobId} className="group hover:border-primary/50 transition-colors">
            <CardContent className="flex flex-col gap-3">
              {/* Main Row */}
              <div className="flex items-center gap-3">
                <div className="w-11 h-11 rounded-lg bg-indigo-500/10 flex items-center justify-center shrink-0">
                  <Activity className="w-5 h-5 text-indigo-500" />
                </div>
                
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 min-w-0">
                    <span className="font-semibold text-base text-foreground group-hover:text-primary transition-colors truncate">
                      {item.jdTitle || 'Bypass JD'}
                    </span>
                    {getStatusBadge(item.status)}
                  </div>
                  <p className="text-sm text-muted-foreground truncate" title={item.cvFileName || 'Bypass CV'}>
                    {item.cvFileName || 'Bypass CV'}
                  </p>
                  <div className="flex items-center gap-3 flex-wrap text-xs text-muted-foreground mt-0.5">
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3 shrink-0" />
                      {new Date(item.updatedAt).toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                </div>
                
                <div className="flex items-center gap-1 shrink-0">
                  {item.status === 'Completed' && item.matchScore !== undefined ? (
                    <div className={`flex flex-col items-center justify-center font-bold w-12 h-12 rounded-full border ${getScoreColor(item.matchScore * 100)}`} title="Match Score">
                      <span className="text-base leading-none">{(item.matchScore * 100).toFixed(0)}</span>
                      <span className="text-[9px] font-normal leading-none mt-0.5 opacity-80">Score</span>
                    </div>
                  ) : (
                    <div className="flex flex-col items-center justify-center font-bold w-12 h-12 rounded-full border bg-muted text-muted-foreground" title="Score Unavailable">
                      <span className="text-base leading-none">-</span>
                    </div>
                  )}
                </div>
              </div>

              {/* Action Row */}
              <div className="flex flex-wrap items-center justify-between gap-2 pt-2.5 border-t border-border/50">
                <Button 
                  variant="outline" 
                  size="sm" 
                  className="flex-1 sm:flex-none gap-1.5"
                  onClick={() => navigateToDetail(item.jobId)}
                  disabled={item.status !== 'Completed'}
                >
                  <Eye className="w-3.5 h-3.5" /> View Full Report
                </Button>

                <Dialog>
                  <DialogTrigger render={
                    <Button 
                      variant="ghost" 
                      size="sm" 
                      className="text-muted-foreground hover:text-destructive hover:bg-destructive/10 gap-1.5"
                      title="Delete History"
                    >
                      <Trash2 className="h-3.5 w-3.5" /> Delete
                    </Button>
                  } />
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>Delete Matching History</DialogTitle>
                      <DialogDescription>
                        Are you sure you want to delete this matching history? This action cannot be undone.
                      </DialogDescription>
                    </DialogHeader>
                    <DialogFooter className="mt-4">
                      <DialogClose render={
                        <Button variant="outline">
                          Cancel
                        </Button>
                      } />
                      <DialogClose render={
                        <Button 
                          variant="destructive"
                          onClick={() => deleteMutation.mutate(item.jobId)}
                          disabled={deleteMutation.isPending}
                        >
                          {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                          Delete
                        </Button>
                      } />
                    </DialogFooter>
                  </DialogContent>
                </Dialog>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Pagination */}
      <ListPagination page={page} totalPages={totalPages} setPage={handlePageChange} />
    </div>
  );
}
