'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { useGetMatchHistory, useDeleteMatchHistory } from '@/hooks/useCvMatch';
import { APP_ROUTES } from '@/lib/constants';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ListPagination } from '@/components/shared/ListPagination';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
import { EmptyState } from '@/components/shared/EmptyState';
import { FileText, Eye, Plus, Trash2, Calendar, MoreHorizontal, Sparkles } from 'lucide-react';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useTranslations } from 'next-intl';

export default function CvMatchingHistoryPage() {
  const router = useRouter();
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const [itemToDelete, setItemToDelete] = useState<string | null>(null);
  const t = useTranslations("CandidateCVMatching");

  const { data: response, isLoading } = useGetMatchHistory(page, pageSize);
  const deleteMutation = useDeleteMatchHistory();
  const historyData = response?.data;
  const items = historyData?.items || [];
  const totalCount = historyData?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const navigateToDetail = (jobId: string) => {
    router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${jobId}`);
  };

  const handleConfirmDelete = () => {
    if (itemToDelete) {
      deleteMutation.mutate(itemToDelete);
      setItemToDelete(null);
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Completed':
        return <Badge className="shrink-0 text-xs px-2 py-0.5 border-none font-medium bg-emerald-500/10 text-emerald-700">{t('completed')}</Badge>;
      case 'Failed':
        return <Badge className="shrink-0 text-xs px-2 py-0.5 border-none font-medium bg-rose-500/10 text-rose-700">{t('failed')}</Badge>;
      default:
        return <Badge className="shrink-0 text-xs px-2 py-0.5 border-none font-medium bg-amber-500/10 text-amber-700">{status}</Badge>;
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
          <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            {t('loading')}
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
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
            <p className="text-muted-foreground mt-2 max-w-2xl">
              {t('description')}
            </p>
          </div>
        </div>
        <EmptyState 
          title={t('noMatchesFound')} 
          description={t('noMatchesDesc')}
          imageUrl="/images/emptyMatching.png"
        >
          <Link href={`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new`}>
            <Button className="mt-4 bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
              <Plus className="mr-1 h-4 w-4" />
              {t('matchCvNow')}
              <Sparkles className="mr-2 h-4 w-4 ml-1" />
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
          <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            {t('description')}
          </p>
        </div>
        <Button onClick={() => router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new`)} className="bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
          <Plus className="mr-1 h-4 w-4" />
          {t('matchCvNow')}
          <Sparkles className="mr-2 h-4 w-4 ml-1" />
        </Button>
      </div>

      <div className="flex flex-col gap-3">
        {items.map((item) => (
          <Card key={item.jobId} className="group hover:border-primary/50 transition-colors">
            <CardContent className="flex flex-col gap-3">
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                {/* Main Row */}
                <div className="flex items-center gap-3 flex-1 min-w-0">
                  <Image src="/images/mascotAvatarMatching.png" alt="Mascot" width={44} height={44} className={`w-11 h-11 rounded-lg shrink-0 object-cover border bg-white dark:bg-slate-900 ${
                    item.status === 'Completed'
                      ? 'border-emerald-200 dark:border-emerald-800 ring-1 ring-emerald-500/20'
                      : item.status === 'Failed'
                        ? 'border-rose-200 dark:border-rose-800 ring-1 ring-rose-500/20'
                        : 'border-amber-200 dark:border-amber-800 ring-1 ring-amber-500/20'
                  }`} />
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 min-w-0">
                      <span className="font-medium text-base text-foreground group-hover:text-primary transition-colors line-clamp-1 leading-snug">
                        {item.jdTitle || t('bypassJd')}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 flex-wrap mt-1 text-sm text-slate-600">
                      <div className="flex items-center">
                        {getStatusBadge(item.status)}
                      </div>
                      <span className="flex items-center gap-1.5 truncate max-w-[200px]" title={item.cvFileName || t('bypassCv')}>
                        <FileText className="h-4 w-4 shrink-0 text-slate-400" />
                        {item.cvFileName || t('bypassCv')}
                      </span>
                      <span className="flex items-center gap-1.5">
                        <Calendar className="h-4 w-4 shrink-0 text-slate-400" />
                        {new Date(item.updatedAt).toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                  </div>
                </div>
                
                {/* Right Zone */}
                <div className="flex items-center gap-3 shrink-0">
                  {item.status === 'Completed' && item.matchScore !== undefined ? (
                    <Badge className={`rounded-full px-3 py-1 text-xs font-semibold pointer-events-none ${getScoreColor(item.matchScore * 100)}`} title="Match Score">
                      {t('matchScore', { score: (item.matchScore * 100).toFixed(0) })}
                    </Badge>
                  ) : (
                    <Badge className="rounded-full px-3 py-1 text-xs font-semibold bg-muted/50 text-muted-foreground border border-border/50 pointer-events-none" title="Score Unavailable">
                      {t('naMatch')}
                    </Badge>
                  )}

                  <Button 
                    variant="outline" 
                    size="sm" 
                    className="gap-1.5 h-9"
                    onClick={() => navigateToDetail(item.jobId)}
                    disabled={item.status !== 'Completed'}
                  >
                    <Eye className="w-4 h-4" /> {t('viewReport')}
                  </Button>

                  {item.status === 'Completed' || item.status === 'Failed' ? (
                    <Popover>
                      <PopoverTrigger className="inline-flex items-center justify-center h-9 w-9 text-slate-500 hover:text-foreground shrink-0 border border-transparent hover:border-border hover:bg-muted/50 rounded-lg transition-colors focus-visible:outline-hidden focus-visible:ring-1 focus-visible:ring-ring">
                        <MoreHorizontal className="h-4 w-4" />
                      </PopoverTrigger>
                      <PopoverContent align="end" className="w-48 p-1">
                        <div className="flex flex-col">
                          <Button
                            variant="ghost"
                            className="w-full justify-start gap-2 h-9 text-rose-600 hover:text-rose-700 hover:bg-rose-50"
                            onClick={() => setItemToDelete(item.jobId)}
                          >
                            <Trash2 className="h-4 w-4" />
                            <span>Delete History</span>
                          </Button>
                        </div>
                      </PopoverContent>
                    </Popover>
                  ) : null}
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Pagination */}
      <ListPagination page={page} totalPages={totalPages} setPage={setPage} />

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!itemToDelete} onOpenChange={(open) => !open && setItemToDelete(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('deleteHistoryTitle')}</DialogTitle>
            <DialogDescription>
              {t('deleteHistoryConfirm')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 flex flex-col sm:flex-row gap-2 justify-end">
            <Button variant="outline" onClick={() => setItemToDelete(null)} disabled={deleteMutation.isPending}>
              {t('cancel')}
            </Button>
            <Button variant="destructive" onClick={handleConfirmDelete} disabled={deleteMutation.isPending}>
              {deleteMutation.isPending ? t('deleting') : t('delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
