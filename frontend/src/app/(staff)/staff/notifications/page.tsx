'use client';

import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import {
  Plus,
  Trash2,
  Bell,
  Search,
  ChevronLeft,
  ChevronRight,
  X,
  RotateCcw,
  SearchX,
} from 'lucide-react';
import { useDebounce } from '@/hooks/use-debounce';

import { notificationService, type SystemNotificationDto } from '@/services/notification.service';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { toast } from 'sonner';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogClose,
} from '@/components/ui/dialog';

export default function StaffNotificationsPage() {
  const queryClient = useQueryClient();
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [deleteDialog, setDeleteDialog] = useState<{
    open: boolean;
    item: SystemNotificationDto | null;
  }>({
    open: false,
    item: null,
  });

  const [searchTerm, setSearchTerm] = useState('');
  const debouncedSearchTerm = useDebounce(searchTerm, 350);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['system-notifications', pageIndex, pageSize, debouncedSearchTerm],
    queryFn: () => notificationService.getSystemNotifications(pageIndex, pageSize, debouncedSearchTerm),
  });

  const deleteMutation = useMutation({
    mutationFn: (item: SystemNotificationDto) =>
      notificationService.deleteSystemNotification(item.title, item.message),
    onSuccess: () => {
      toast.success('System notification deleted successfully');
      queryClient.invalidateQueries({ queryKey: ['system-notifications'] });
      setDeleteDialog({ open: false, item: null });
    },
    onError: () => {
      toast.error('Failed to delete notification');
    },
  });

  const notifications = data?.data || [];
  const meta = data?.meta;

  const totalPages = meta?.totalPages || 1;
  const totalCount = meta?.totalItems || notifications.length;

  const handleResetFilters = () => {
    setSearchTerm('');
    setPageIndex(1);
  };

  const isFilterActive = searchTerm !== '';

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const startResult = totalCount > 0 ? (pageIndex - 1) * pageSize + 1 : 0;
  const endResult = Math.min(pageIndex * pageSize, totalCount);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Bell className="text-[#1877F2] shrink-0 h-8 w-8" />
              System Notifications (Staff)
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Manage announcements and notifications sent to candidates and recruiters.
            </p>
          </div>

          <Link href="/staff/notifications/create" className="w-full sm:w-auto">
            <Button className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto">
              <Plus className="h-4 w-4" />
              Create Notification
            </Button>
          </Link>
        </div>

        {/* TẦNG 1: TOOLBAR (Search Bar, Reset Filters) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-80 md:w-96">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setPageIndex(1);
                }}
                placeholder="Search notifications by title..."
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {searchTerm && (
                <button
                  onClick={() => {
                    setSearchTerm('');
                    setPageIndex(1);
                  }}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1 cursor-pointer"
                  title="Clear search"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>

            {/* Clear Filters Button */}
            {isFilterActive && (
              <Button
                onClick={handleResetFilters}
                variant="ghost"
                className="h-10 px-3 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 font-medium transition-colors cursor-pointer"
              >
                <RotateCcw className="h-3.5 w-3.5 mr-1.5" /> Clear Filters
              </Button>
            )}
          </div>
        </div>

        {/* TẦNG 2: MAIN TABLE CONTAINER (TABLE_STANDARD - SHADCN TABLE) */}
        <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
          <Table className="w-full text-left border-collapse table-fixed">
            {/* Table Header */}
            <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
              <TableRow className="hover:bg-transparent border-none">
                <TableHead className="w-[25%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  NOTIFICATION TITLE
                </TableHead>

                <TableHead className="w-[42%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  MESSAGE
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  STATUS
                </TableHead>

                <TableHead className="w-[13%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  SENT DATE
                </TableHead>

                <TableHead className="w-[8%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  ACTIONS
                </TableHead>
              </TableRow>
            </TableHeader>

            {/* Table Body */}
            <TableBody>
              {isLoading ? (
                // Loading Skeleton State (6 rows)
                Array.from({ length: pageSize || 6 }).map((_, index) => (
                  <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-40 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-4/5 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-6 w-16 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-28 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-2 text-center">
                      <Skeleton className="h-8 w-8 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
                    </TableCell>
                  </TableRow>
                ))
              ) : isError ? (
                // Error State
                <TableRow>
                  <TableCell colSpan={5} className="h-64 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">
                        Failed to load system notifications
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        An error occurred while fetching notification history. Please try again.
                      </p>
                      <Button
                        onClick={() => refetch()}
                        variant="outline"
                        className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                      >
                        <RotateCcw className="h-4 w-4 mr-2" /> Retry Loading
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ) : notifications.length === 0 ? (
                // Empty State
                <TableRow>
                  <TableCell colSpan={5} className="h-72 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                        <SearchX className="h-6 w-6" />
                      </div>
                      <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                        No notifications found
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        {isFilterActive
                          ? 'No notifications match the search query. Try clearing or adjusting your search filter.'
                          : 'No system notifications have been broadcasted yet.'}
                      </p>
                      {isFilterActive && (
                        <Button
                          onClick={handleResetFilters}
                          variant="outline"
                          className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                        >
                          <RotateCcw className="h-4 w-4 mr-2" /> Clear All Filters
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ) : (
                // Actual Data Rows
                notifications.map((notification, idx) => (
                  <TableRow
                    key={idx}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Title */}
                    <TableCell className="py-3.5 px-3 align-middle">
                      <span
                        className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors truncate block max-w-[220px]"
                        title={notification.title}
                      >
                        {notification.title}
                      </span>
                    </TableCell>

                    {/* Message */}
                    <TableCell className="py-3.5 px-3 align-middle">
                      <span
                        className="text-xs text-[#65676B] dark:text-zinc-400 truncate block max-w-[380px]"
                        title={notification.message}
                      >
                        {notification.message}
                      </span>
                    </TableCell>

                    {/* Status */}
                    <TableCell className="py-3.5 px-3 align-middle text-center">
                      <div className="flex justify-center">
                        {notification.isHidden ? (
                          <Badge variant="outline" className="rounded-full px-2.5 py-0.5 text-xs font-medium text-[#65676B]">
                            Hidden
                          </Badge>
                        ) : (
                          <Badge className="bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
                            Active
                          </Badge>
                        )}
                      </div>
                    </TableCell>

                    {/* Sent Date */}
                    <TableCell className="py-3.5 px-3 align-middle text-xs text-[#65676B] dark:text-zinc-300 font-medium">
                      {formatDate(notification.createdAt)}
                    </TableCell>

                    {/* Actions (Icon-only button) */}
                    <TableCell className="py-3.5 px-2 align-middle text-center">
                      <div className="flex items-center justify-center">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer disabled:opacity-40"
                          onClick={() => setDeleteDialog({ open: true, item: notification })}
                          disabled={notification.isHidden}
                          title="Delete Notification"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {/* TẦNG 3: PAGINATION FOOTER */}
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-1 px-1">
          <div className="flex items-center space-x-3 text-sm text-[#65676B] dark:text-zinc-400">
            <div>
              Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{totalCount}</span> system notifications
            </div>
            <Select
              value={String(pageSize)}
              onValueChange={(val) => {
                if (val) setPageSize(Number(val));
                setPageIndex(1);
              }}
            >
              <SelectTrigger className="h-8 w-[110px] border-[#CED0D4] dark:border-zinc-800 text-xs font-medium focus:ring-[#1877F2]">
                <SelectValue placeholder="Page size" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="10">10 / page</SelectItem>
                <SelectItem value="20">20 / page</SelectItem>
                <SelectItem value="50">50 / page</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Page Buttons */}
          <div className="flex items-center space-x-1.5">
            <Button
              variant="outline"
              size="icon"
              disabled={pageIndex === 1 || isLoading}
              onClick={() => setPageIndex((prev) => Math.max(1, prev - 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>

            {Array.from({ length: totalPages }).map((_, index) => {
              const pageNum = index + 1;
              if (
                totalPages <= 5 ||
                pageNum === 1 ||
                pageNum === totalPages ||
                Math.abs(pageNum - pageIndex) <= 1
              ) {
                const isCurrent = pageNum === pageIndex;
                return (
                  <Button
                    key={pageNum}
                    variant={isCurrent ? 'default' : 'outline'}
                    disabled={isLoading}
                    onClick={() => setPageIndex(pageNum)}
                    className={`h-8 w-8 text-xs font-semibold rounded-md shadow-2xs transition-all cursor-pointer ${
                      isCurrent
                        ? 'bg-[#1877F2] hover:bg-[#166FE5] text-white border-[#1877F2]'
                        : 'border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400'
                    }`}
                  >
                    {pageNum}
                  </Button>
                );
              }
              if (
                (pageNum === 2 && pageIndex > 3) ||
                (pageNum === totalPages - 1 && pageIndex < totalPages - 2)
              ) {
                return (
                  <span key={pageNum} className="px-1 text-xs text-[#65676B]">
                    ...
                  </span>
                );
              }
              return null;
            })}

            <Button
              variant="outline"
              size="icon"
              disabled={pageIndex >= totalPages || isLoading}
              onClick={() => setPageIndex((prev) => Math.min(totalPages, prev + 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Delete Dialog Modal */}
      <Dialog open={deleteDialog.open} onOpenChange={(open) => setDeleteDialog((prev) => ({ ...prev, open }))}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete System Notification</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this notification? It will be removed from all users' inboxes immediately. This action cannot be undone.
            </DialogDescription>
          </DialogHeader>

          <div className="bg-muted/50 p-4 rounded-md my-2 border border-border">
            <h4 className="font-medium text-sm mb-1">{deleteDialog.item?.title}</h4>
            <p className="text-sm text-muted-foreground line-clamp-3">{deleteDialog.item?.message}</p>
          </div>

          <DialogFooter className="mt-4">
            <DialogClose render={<Button variant="outline" />}>
              Cancel
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => deleteDialog.item && deleteMutation.mutate(deleteDialog.item)}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? 'Deleting...' : 'Delete Notification'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
