'use client';

import React, { useState } from 'react';
import {
  useSubscriptions,
  useCreateSubscription,
  useUpdateSubscription,
  useDuplicateSubscription,
  useUpdateSubscriptionStatus,
} from '@/hooks/useSubscription';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import { toast } from 'sonner';
import { SubscriptionForm } from './components/SubscriptionForm';
import { CoinConfigTab } from './components/CoinConfigTab';
import { CustomCoinTopupPriceTab } from './components/CustomCoinTopupPriceTab';
import type { SubscriptionDto, SubscriptionStatus } from '@/types/subscription.types';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import {
  Plus,
  Edit2,
  Copy,
  CreditCard,
  RotateCcw,
  SearchX,
  ChevronLeft,
  ChevronRight,
  Coins,
} from 'lucide-react';
import { useTranslations } from 'next-intl';

export default function SubscriptionsAdminPage() {
  const t = useTranslations('AdminSubscriptions');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [roleFilter, setRoleFilter] = useState<string>('ALL');
  const [statusFilter, setStatusFilter] = useState<string>('ALL');
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingSub, setEditingSub] = useState<SubscriptionDto | null>(null);

  const { data, isLoading, isError, refetch } = useSubscriptions({
    page,
    pageSize,
    role: roleFilter === 'ALL' ? undefined : roleFilter,
    status: statusFilter === 'ALL' ? undefined : (statusFilter as SubscriptionStatus),
  });

  const createMutation = useCreateSubscription();
  const updateMutation = useUpdateSubscription();
  const duplicateMutation = useDuplicateSubscription();
  const updateStatusMutation = useUpdateSubscriptionStatus();

  const getErrorMessage = (err: any, fallback: string) => {
    return (
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      (typeof err?.response?.data === 'string' ? err.response.data : null) ||
      err?.message ||
      fallback
    );
  };

  const handleCreateOrUpdate = async (formData: any) => {
    if (editingSub) {
      updateMutation.mutate(
        { id: editingSub.id, data: formData },
        {
          onSuccess: (res) => {
            if (res.success) {
              toast.success(t('toastUpdateSuccess'));
              setIsDialogOpen(false);
              setEditingSub(null);
            } else {
              toast.error(res.message || 'Failed to update package');
            }
          },
          onError: (err: any) => {
            toast.error(getErrorMessage(err, 'Failed to update package'));
          },
        }
      );
    } else {
      createMutation.mutate(formData, {
        onSuccess: (res) => {
          if (res.success) {
            toast.success(t('toastCreateSuccess'));
            setIsDialogOpen(false);
          } else {
            toast.error(res.message || 'Failed to create package');
          }
        },
        onError: (err: any) => {
          toast.error(getErrorMessage(err, 'Failed to create package'));
        },
      });
    }
  };

  const handleDuplicate = (id: number) => {
    duplicateMutation.mutate(id, {
      onSuccess: (res) => {
        if (res.success) {
          toast.success(t('toastDuplicateSuccess'));
        } else {
          toast.error(res.message || 'Failed to duplicate package');
        }
      },
      onError: (err: any) => {
        toast.error(getErrorMessage(err, 'Failed to duplicate package'));
      },
    });
  };

  const handleToggleStatus = (id: number, currentStatus: SubscriptionStatus) => {
    const nextStatus: SubscriptionStatus = currentStatus === 'ACTIVE' ? 'INACTIVE' : 'ACTIVE';
    updateStatusMutation.mutate(
      { id, status: nextStatus },
      {
        onSuccess: (res) => {
          if (res.success) {
            toast.success(t('toastStatusChangeSuccess', { status: nextStatus }));
          } else {
            toast.error(res.message || 'Failed to change status');
          }
        },
        onError: (err: any) => {
          toast.error(getErrorMessage(err, 'Failed to change status'));
        },
      }
    );
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);
  };

  const handleResetFilters = () => {
    setRoleFilter('ALL');
    setStatusFilter('ALL');
    setPage(1);
  };

  const isFilterActive = roleFilter !== 'ALL' || statusFilter !== 'ALL';

  const subItems = data?.data?.items || [];
  const totalCount = data?.data?.totalItems || 0;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  const startResult = totalCount > 0 ? (page - 1) * pageSize + 1 : 0;
  const endResult = Math.min(page * pageSize, totalCount);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <CreditCard className="text-[#1877F2] shrink-0 h-8 w-8" />
              {t('pageTitle')}
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              {t('pageDesc')}
            </p>
          </div>

          <Dialog
            open={isDialogOpen}
            onOpenChange={(open) => {
              setIsDialogOpen(open);
              if (!open) setEditingSub(null);
            }}
          >
            <DialogTrigger render={<Button className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto" onClick={() => setEditingSub(null)} />}>
              <Plus className="h-4 w-4" />
              {t('addNewPackage')}
            </DialogTrigger>
            <DialogContent className="max-w-lg overflow-y-auto max-h-[90vh]">
              <DialogHeader>
                <DialogTitle>
                  {editingSub ? t('editPackageTitle') : t('createPackageTitle')}
                </DialogTitle>
              </DialogHeader>
              <SubscriptionForm
                initialData={editingSub}
                onSubmit={handleCreateOrUpdate}
                isLoading={createMutation.isPending || updateMutation.isPending}
              />
            </DialogContent>
          </Dialog>
        </div>

        {/* Tabs Navigation */}
        <Tabs defaultValue="subscriptions" className="space-y-5">
          <TabsList className="border-b border-[#CED0D4] dark:border-zinc-800 rounded-none w-full justify-start bg-transparent h-auto p-0 gap-6">
            <TabsTrigger
              value="subscriptions"
              className="rounded-none border-b-2 border-transparent data-[state=active]:border-[#1877F2] data-[state=active]:text-[#1877F2] data-[state=active]:bg-transparent px-4 py-2 font-bold text-sm transition-all flex items-center gap-2 cursor-pointer"
            >
              <CreditCard className="h-4 w-4" />
              {t('tabSubscriptions')}
            </TabsTrigger>
            <TabsTrigger
              value="coin-config"
              className="rounded-none border-b-2 border-transparent data-[state=active]:border-[#1877F2] data-[state=active]:text-[#1877F2] data-[state=active]:bg-transparent px-4 py-2 font-bold text-sm transition-all flex items-center gap-2 cursor-pointer"
            >
              <Coins className="h-4 w-4" />
              {t('tabCoinConfig')}
            </TabsTrigger>
            <TabsTrigger
              value="custom-coin-price"
              className="rounded-none border-b-2 border-transparent data-[state=active]:border-[#1877F2] data-[state=active]:text-[#1877F2] data-[state=active]:bg-transparent px-4 py-2 font-bold text-sm transition-all flex items-center gap-2 cursor-pointer"
            >
              <Coins className="h-4 w-4" />
              {t('tabCustomCoinPrice')}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="subscriptions" className="space-y-5">
            {/* TẦNG 1: TOOLBAR (Filters & Clear Filters) */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="flex flex-wrap items-center gap-2.5 flex-1">
                {/* Target Role Filter */}
                <Select
                  value={roleFilter}
                  onValueChange={(val) => {
                    if (val) setRoleFilter(val);
                    setPage(1);
                  }}
                >
                  <SelectTrigger className="w-full sm:w-[170px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                    <SelectValue placeholder={t('targetRoleFilter')} />
                  </SelectTrigger>
                  <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                    <SelectItem value="ALL">{t('allTargetRoles')}</SelectItem>
                    <SelectItem value="CANDIDATE">{t('roleCandidate')}</SelectItem>
                    <SelectItem value="RECRUITER">{t('roleRecruiter')}</SelectItem>
                  </SelectContent>
                </Select>

                {/* Status Filter */}
                <Select
                  value={statusFilter}
                  onValueChange={(val) => {
                    if (val) setStatusFilter(val);
                    setPage(1);
                  }}
                >
                  <SelectTrigger className="w-full sm:w-[170px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                    <SelectValue placeholder={t('statusFilter')} />
                  </SelectTrigger>
                  <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                    <SelectItem value="ALL">{t('allStatuses')}</SelectItem>
                    <SelectItem value="ACTIVE">{t('statusActive')}</SelectItem>
                    <SelectItem value="INACTIVE">{t('statusInactive')}</SelectItem>
                  </SelectContent>
                </Select>

                {/* Clear Filters Button */}
                {isFilterActive && (
                  <Button
                    onClick={handleResetFilters}
                    variant="ghost"
                    className="h-10 px-3 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 font-medium transition-colors cursor-pointer"
                  >
                    <RotateCcw className="h-3.5 w-3.5 mr-1.5" /> {t('clearFilters')}
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
                    <TableHead className="w-[24%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                      {t('colPackageName')}
                    </TableHead>

                    <TableHead className="w-[15%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                      {t('colTargetRole')}
                    </TableHead>

                    <TableHead className="w-[16%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                      {t('colPrice')}
                    </TableHead>

                    <TableHead className="w-[13%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                      {t('colDuration')}
                    </TableHead>

                    <TableHead className="w-[14%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                      {t('colStatus')}
                    </TableHead>

                    <TableHead className="w-[10%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                      {t('colTransactions')}
                    </TableHead>

                    <TableHead className="w-[8%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                      {t('colActions')}
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
                          <Skeleton className="h-5 w-36 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        </TableCell>
                        <TableCell className="py-3.5 px-3">
                          <Skeleton className="h-6 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full" />
                        </TableCell>
                        <TableCell className="py-3.5 px-3">
                          <Skeleton className="h-5 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        </TableCell>
                        <TableCell className="py-3.5 px-3">
                          <Skeleton className="h-4 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        </TableCell>
                        <TableCell className="py-3.5 px-3 text-center">
                          <Skeleton className="h-5 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                        </TableCell>
                        <TableCell className="py-3.5 px-3 text-center">
                          <Skeleton className="h-5 w-14 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                        </TableCell>
                        <TableCell className="py-3.5 px-2 text-center">
                          <Skeleton className="h-8 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
                        </TableCell>
                      </TableRow>
                    ))
                  ) : isError ? (
                    // Error State
                    <TableRow>
                      <TableCell colSpan={7} className="h-64 text-center">
                        <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                          <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">
                            {t('loadFailTitle')}
                          </p>
                          <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                            {t('loadFailDesc')}
                          </p>
                          <Button
                            onClick={() => refetch()}
                            variant="outline"
                            className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                          >
                            <RotateCcw className="h-4 w-4 mr-2" /> {t('retryBtn')}
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ) : subItems.length === 0 ? (
                    // Empty State
                    <TableRow>
                      <TableCell colSpan={7} className="h-72 text-center">
                        <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                          <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                            <SearchX className="h-6 w-6" />
                          </div>
                          <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                            {t('noDataTitle')}
                          </p>
                          <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                            {isFilterActive
                              ? t('noDataFilterDesc')
                              : t('noDataEmptyDesc')}
                          </p>
                          {isFilterActive && (
                            <Button
                              onClick={handleResetFilters}
                              variant="outline"
                              className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                            >
                              <RotateCcw className="h-4 w-4 mr-2" /> {t('clearAllFilters')}
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ) : (
                    // Actual Data Rows
                    subItems.map((sub) => (
                      <TableRow
                        key={sub.id}
                        className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                      >
                        {/* Package Name */}
                        <TableCell className="py-3.5 px-3 align-middle">
                          <span
                            className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors truncate block max-w-[200px]"
                            title={sub.name}
                          >
                            {sub.name}
                          </span>
                        </TableCell>

                        {/* Target Role */}
                        <TableCell className="py-3.5 px-3 align-middle">
                          {sub.featuresConfig?.role === 'CANDIDATE' ? (
                            <Badge className="bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
                              {t('roleCandidate')}
                            </Badge>
                          ) : (
                            <Badge className="bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
                              {t('roleRecruiter')}
                            </Badge>
                          )}
                        </TableCell>

                        {/* Price */}
                        <TableCell className="py-3.5 px-3 align-middle font-bold text-sm text-emerald-600 dark:text-emerald-400">
                          {formatPrice(sub.price)}
                        </TableCell>

                        {/* Duration */}
                        <TableCell className="py-3.5 px-3 align-middle text-sm font-medium text-[#65676B] dark:text-zinc-300">
                          {sub.durationDays} {t('durationDays')}
                        </TableCell>

                        {/* Status (Switch + Label) */}
                        <TableCell className="py-3.5 px-3 align-middle text-center">
                          <div className="flex items-center justify-center gap-2">
                            <Switch
                              checked={sub.status === 'ACTIVE'}
                              onCheckedChange={() => handleToggleStatus(sub.id, sub.status)}
                            />
                            <span className="text-xs font-semibold text-[#050505] dark:text-zinc-300">
                              {sub.status === 'ACTIVE' ? t('statusActive') : t('statusInactive')}
                            </span>
                          </div>
                        </TableCell>

                        {/* Transactions (Sold / Not Sold) */}
                        <TableCell className="py-3.5 px-3 align-middle text-center">
                          <div className="flex justify-center">
                            {sub.isUsed ? (
                              <Badge className="bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
                                {t('badgeSold')}
                              </Badge>
                            ) : (
                              <Badge variant="outline" className="rounded-full px-2.5 py-0.5 text-xs font-medium text-[#65676B]">
                                {t('badgeNotSold')}
                              </Badge>
                            )}
                          </div>
                        </TableCell>

                        {/* Actions (Icon-only buttons) */}
                        <TableCell className="py-3.5 px-2 align-middle text-center">
                          <div className="flex items-center justify-center gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => {
                                setEditingSub(sub);
                                setIsDialogOpen(true);
                              }}
                              className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                              title={t('editPackage')}
                            >
                              <Edit2 className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleDuplicate(sub.id)}
                              className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                              title={t('duplicatePackage')}
                            >
                              <Copy className="h-4 w-4" />
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
                <div dangerouslySetInnerHTML={{ __html: t.raw('showingText').replace('{start}', startResult.toString()).replace('{end}', endResult.toString()).replace('{total}', totalCount.toString()) }} />
                <Select
                  value={String(pageSize)}
                  onValueChange={(val) => {
                    if (val) setPageSize(Number(val));
                    setPage(1);
                  }}
                >
                  <SelectTrigger className="h-8 w-[110px] border-[#CED0D4] dark:border-zinc-800 text-xs font-medium focus:ring-[#1877F2]">
                    <SelectValue placeholder={t('pageSize')} />
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
                  disabled={page === 1 || isLoading}
                  onClick={() => setPage((prev) => Math.max(1, prev - 1))}
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
                    Math.abs(pageNum - page) <= 1
                  ) {
                    const isCurrent = pageNum === page;
                    return (
                      <Button
                        key={pageNum}
                        variant={isCurrent ? 'default' : 'outline'}
                        disabled={isLoading}
                        onClick={() => setPage(pageNum)}
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
                    (pageNum === 2 && page > 3) ||
                    (pageNum === totalPages - 1 && page < totalPages - 2)
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
                  disabled={page >= totalPages || isLoading}
                  onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
                  className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </TabsContent>

          <TabsContent value="coin-config">
            <CoinConfigTab />
          </TabsContent>

          <TabsContent value="custom-coin-price">
            <CustomCoinTopupPriceTab />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
