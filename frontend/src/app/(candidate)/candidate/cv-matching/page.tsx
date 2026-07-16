'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useGetMatchHistory } from '@/hooks/useCvMatch';
import { APP_ROUTES } from '@/lib/constants';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Loader2, FileText, Briefcase, Eye, Plus } from 'lucide-react';

export default function CvMatchingHistoryPage() {
  const router = useRouter();
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data: response, isLoading } = useGetMatchHistory(page, pageSize);
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
        return <Badge className="bg-emerald-500/10 text-emerald-600 hover:bg-emerald-500/20 border-emerald-500/20">Completed</Badge>;
      case 'Failed':
        return <Badge variant="destructive" className="bg-red-500/10 text-red-600 hover:bg-red-500/20 border-red-500/20">Failed</Badge>;
      default:
        return <Badge variant="secondary" className="bg-amber-500/10 text-amber-600 hover:bg-amber-500/20 border-amber-500/20">{status}</Badge>;
    }
  };

  return (
    <div className="max-w-7xl mx-auto w-full pt-6 pb-10 px-4 md:px-8">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-8 gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Matching History</h1>
          <p className="text-sm text-muted-foreground">View your previous AI CV-JD matching results</p>
        </div>
        <Button onClick={() => router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new`)} className="gap-2">
          <Plus className="h-4 w-4" />
          New Match
        </Button>
      </div>

      <div className="bg-card border rounded-lg shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/50 hover:bg-muted/50">
                <TableHead className="w-[180px]">Date</TableHead>
                <TableHead>Resume</TableHead>
                <TableHead>Job Description</TableHead>
                <TableHead className="w-[120px] text-center">Score</TableHead>
                <TableHead className="w-[120px] text-center">Status</TableHead>
                <TableHead className="w-[100px] text-right">Action</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={6} className="h-48 text-center">
                    <div className="flex flex-col items-center justify-center text-muted-foreground gap-2">
                      <Loader2 className="h-6 w-6 animate-spin text-primary" />
                      Loading history...
                    </div>
                  </TableCell>
                </TableRow>
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="h-48 text-center text-muted-foreground">
                    No matching history found.
                  </TableCell>
                </TableRow>
              ) : (
                items.map((item) => (
                  <TableRow key={item.jobId} className="group hover:bg-muted/30">
                    <TableCell className="text-sm font-medium text-muted-foreground">
                      {new Date(item.updatedAt).toLocaleString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <FileText className="h-4 w-4 text-primary/70 shrink-0" />
                        <span className="font-medium truncate max-w-[200px]" title={item.cvFileName || 'Bypass CV'}>
                          {item.cvFileName || 'Bypass CV'}
                        </span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Briefcase className="h-4 w-4 text-primary/70 shrink-0" />
                        <span className="truncate max-w-[250px]" title={item.jdTitle || 'Bypass JD'}>
                          {item.jdTitle || 'Bypass JD'}
                        </span>
                      </div>
                    </TableCell>
                    <TableCell className="text-center">
                      {item.status === 'Completed' && item.matchScore !== undefined ? (
                        <span className="font-bold text-primary">{item.matchScore.toFixed(1)}</span>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell className="text-center">
                      {getStatusBadge(item.status)}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button 
                        variant="ghost" 
                        size="sm" 
                        className="opacity-0 group-hover:opacity-100 transition-opacity gap-1 text-primary hover:text-primary hover:bg-primary/10"
                        onClick={() => navigateToDetail(item.jobId)}
                        disabled={item.status !== 'Completed'}
                      >
                        <Eye className="h-4 w-4" />
                        View
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between border-t bg-muted/20 p-4">
            <span className="text-sm text-muted-foreground">
              Showing page <strong className="text-foreground">{page}</strong> of{' '}
              <strong className="text-foreground">{totalPages}</strong> ({totalCount} total)
            </span>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(page - 1)}
                disabled={page <= 1}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(page + 1)}
                disabled={page >= totalPages}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
