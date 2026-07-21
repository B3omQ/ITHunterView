'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useGetMatchHistory, useDeleteMatchHistory } from '@/hooks/useCvMatch';
import { APP_ROUTES } from '@/lib/constants';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { PageLoader } from '@/components/shared/PageLoader';
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
        return <Badge className="bg-emerald-500/10 text-emerald-600 hover:bg-emerald-500/20 border-emerald-500/20 shadow-none">Completed</Badge>;
      case 'Failed':
        return <Badge variant="destructive" className="bg-red-500/10 text-red-600 hover:bg-red-500/20 border-red-500/20 shadow-none">Failed</Badge>;
      default:
        return <Badge variant="secondary" className="bg-amber-500/10 text-amber-600 hover:bg-amber-500/20 border-amber-500/20 shadow-none">{status}</Badge>;
    }
  };

  const getScoreColor = (score: number) => {
    if (score >= 70) return 'text-emerald-600 bg-emerald-500/10 border-emerald-500/20';
    if (score >= 40) return 'text-amber-600 bg-amber-500/10 border-amber-500/20';
    return 'text-red-600 bg-red-500/10 border-red-500/20';
  };

  if (isLoading) return <PageLoader />;

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
          <Card key={item.jobId} className="hover:border-primary/50 transition-colors group">
            <CardContent className="p-3 sm:p-4 flex flex-col gap-3">
              {/* Top Row */}
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-4 flex-1">
                  <div className="w-12 h-12 rounded overflow-hidden bg-primary/10 flex items-center justify-center border shrink-0">
                    <Activity className="w-6 h-6 text-primary" />
                  </div>
                  <div>
                    <h3 className="font-semibold text-primary line-clamp-1 text-base">
                      {item.jdTitle || 'Bypass JD'}
                    </h3>
                    <div className="flex items-center gap-2 mt-1">
                      <FileText className="w-4 h-4 text-muted-foreground shrink-0" />
                      <p className="text-sm text-muted-foreground truncate max-w-[300px]" title={item.cvFileName || 'Bypass CV'}>
                        {item.cvFileName || 'Bypass CV'}
                      </p>
                    </div>
                    <div className="flex items-center gap-4 mt-2 text-xs text-slate-500">
                      <span className="flex items-center gap-1">
                        <Calendar className="w-3 h-3" /> {new Date(item.updatedAt).toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                      </span>
                      {getStatusBadge(item.status)}
                    </div>
                  </div>
                </div>
                
                <div className="flex flex-col items-end gap-2 shrink-0">
                  {item.status === 'Completed' && item.matchScore !== undefined ? (
                    <div className={`flex flex-col items-center justify-center font-bold w-14 h-14 rounded-full border ${getScoreColor(item.matchScore * 100)}`} title="Match Score">
                      <span className="text-lg leading-none">{(item.matchScore * 100).toFixed(0)}</span>
                      <span className="text-[10px] font-normal leading-none mt-1 opacity-80">Score</span>
                    </div>
                  ) : (
                    <div className="flex flex-col items-center justify-center font-bold w-14 h-14 rounded-full border bg-muted text-muted-foreground" title="Score Unavailable">
                      <span className="text-lg leading-none">-</span>
                    </div>
                  )}
                </div>
              </div>

              {/* Action Row */}
              <div className="flex flex-wrap items-center justify-between gap-2 pt-2 sm:pt-3 border-t border-border/50">
                <Button 
                  variant="outline" 
                  size="sm" 
                  className="flex-1 sm:flex-none gap-2"
                  onClick={() => navigateToDetail(item.jobId)}
                  disabled={item.status !== 'Completed'}
                >
                  <Eye className="w-4 h-4" /> View Full Report
                </Button>

                <Dialog>
                  <DialogTrigger render={
                    <Button 
                      variant="ghost" 
                      size="sm" 
                      className="text-muted-foreground hover:text-destructive hover:bg-destructive/10"
                      title="Delete History"
                    />
                  }>
                    <Trash2 className="h-4 w-4" /> Delete
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>Delete Matching History</DialogTitle>
                      <DialogDescription>
                        Are you sure you want to delete this matching history? This action cannot be undone.
                      </DialogDescription>
                    </DialogHeader>
                    <DialogFooter className="mt-4">
                      <DialogClose render={<Button variant="outline" />}>
                        Cancel
                      </DialogClose>
                      <DialogClose render={
                        <Button 
                          variant="destructive"
                          onClick={() => deleteMutation.mutate(item.jobId)}
                          disabled={deleteMutation.isPending}
                        />
                      }>
                        {deleteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                        Delete
                      </DialogClose>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>
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
            onClick={() => handlePageChange(page - 1)}
            disabled={page <= 1}
          >
            <ChevronLeft className="w-4 h-4 mr-2" /> Previous
          </Button>
          <span className="text-sm font-medium">
            Page {page} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handlePageChange(page + 1)}
            disabled={page >= totalPages}
          >
            Next <ChevronRight className="w-4 h-4 ml-2" />
          </Button>
        </div>
      )}
    </div>
  );
}
