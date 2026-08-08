'use client';

import React, { useState, useEffect } from 'react';
import {
  Search,
  Clock,
  Trash2,
  Eye,
  ChevronLeft,
  ChevronRight,
  Shield,
  AlertTriangle,
  CheckCircle,
  RotateCcw,
  X,
  SearchX,
} from 'lucide-react';
import { useAuditLogs, usePurgeAuditLogs } from '@/hooks/useAuditLogs';
import { AuditLogDto } from '@/types/audit-log.types';
import { format } from 'date-fns';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
import { LogDetailsModal } from './components/log-details-modal';
import { PurgeModal } from './components/purge-modal';
import { useTranslations } from 'next-intl';

export default function AdminAuditLogsPage() {
  const t = useTranslations('AdminAuditLogs');
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedOperation, setSelectedOperation] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

  // Date filter (Default: Last 7 days)
  const getPastDateStr = (daysAgo: number) => {
    const d = new Date();
    d.setDate(d.getDate() - daysAgo);
    return d.toISOString().split('T')[0];
  };
  const getTodayStr = () => new Date().toISOString().split('T')[0];

  const [startDateStr, setStartDateStr] = useState<string>(getPastDateStr(7));
  const [endDateStr, setEndDateStr] = useState<string>(getTodayStr());
  const [dateError, setDateError] = useState<string>('');

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Selected Log for detail Modal
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);
  const [isPurgeModalOpen, setIsPurgeModalOpen] = useState(false);
  const [purgeDays, setPurgeDays] = useState(30);
  const [toast, setToast] = useState<{
    message: string;
    type: 'success' | 'error' | 'warning';
  } | null>(null);

  const showToast = (message: string, type: 'success' | 'error' | 'warning' = 'success') => {
    setToast({ message, type });
  };

  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Validate dates
  useEffect(() => {
    if (startDateStr && endDateStr) {
      const start = new Date(startDateStr);
      const end = new Date(endDateStr);
      const diffTime = end.getTime() - start.getTime();
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

      if (diffDays < 0) {
        setDateError('Start date cannot be after end date.');
      } else if (diffDays > 30) {
        setDateError(
          'Time range too large. Please limit search range within 30 days to ensure performance.'
        );
      } else {
        setDateError('');
      }
    } else {
      setDateError('');
    }
  }, [startDateStr, endDateStr]);

  // Formats Dates to ISO for API
  const startIso = startDateStr ? new Date(`${startDateStr}T00:00:00`).toISOString() : undefined;
  const endIso = endDateStr ? new Date(`${endDateStr}T23:59:59`).toISOString() : undefined;

  // Fetch data complying with kinh-mantra.md
  const { data, isLoading, isError, refetch } = useAuditLogs({
    page,
    pageSize,
    search: debouncedSearch || undefined,
    operationType: selectedOperation || undefined,
    category: selectedCategory || undefined,
    startDate: dateError ? undefined : startIso,
    endDate: dateError ? undefined : endIso,
  });

  const purgeMutation = usePurgeAuditLogs();

  const handlePurgeSubmit = (days: number) => {
    if (days < 1) {
      showToast(t('purgeModal.minDayError'), 'error');
      return;
    }

    purgeMutation.mutate(days, {
      onSuccess: (res) => {
        if (res.success) {
          showToast(res.message || `Logs older than ${days} days successfully purged.`, 'success');
          setIsPurgeModalOpen(false);
          refetch();
        } else {
          showToast(res.message || 'An error occurred while purging logs.', 'error');
        }
      },
      onError: (err: any) => {
        showToast(
          err.response?.data?.message || 'An error occurred when calling purge logs API.',
          'error'
        );
      },
    });
  };

  const handleResetFilters = () => {
    setSearchQuery('');
    setSelectedOperation(null);
    setSelectedCategory(null);
    setStartDateStr(getPastDateStr(7));
    setEndDateStr(getTodayStr());
    setPage(1);
  };

  const getOperationBadgeColor = (op: string | null) => {
    switch (op?.toUpperCase()) {
      case 'CREATE':
        return 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none';
      case 'UPDATE':
        return 'bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none';
      case 'DELETE':
        return 'bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none';
      default:
        return 'bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700 rounded-full px-2.5 py-0.5 text-xs font-medium';
    }
  };

  const getCategoryBadgeColor = (cat: string) => {
    switch (cat?.toUpperCase()) {
      case 'DATA_MUTATION':
        return 'bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold';
      case 'SECURITY':
        return 'bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold';
      case 'AUTH':
        return 'bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold';
      case 'SYSTEM':
        return 'bg-zinc-100 dark:bg-zinc-800 text-zinc-700 dark:text-zinc-300 border border-zinc-200 dark:border-zinc-700 rounded-full px-2 py-0.5 text-[10px] font-medium';
      default:
        return 'bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700 rounded-full px-2 py-0.5 text-[10px] font-medium';
    }
  };

  const renderActionText = (action: string) => {
    if (!action) return null;
    const match = action.match(/^(.*?)\s*(\(.*?\))\s*$/);
    if (match && match[1] && match[2]) {
      return (
        <div className="flex flex-col gap-0.5">
          <span className="text-xs font-semibold text-[#050505] dark:text-zinc-100 leading-snug">
            {match[1]}
          </span>
          <span className="text-[11px] font-mono text-[#65676B] dark:text-zinc-400 leading-snug">
            {match[2]}
          </span>
        </div>
      );
    }
    return (
      <span className="text-xs font-semibold text-[#050505] dark:text-zinc-100 whitespace-normal break-words leading-snug">
        {action}
      </span>
    );
  };

  const isFilterActive =
    searchQuery !== '' ||
    selectedCategory !== null ||
    selectedOperation !== null;

  const totalPages = data?.data?.totalPages || 1;
  const items = data?.data?.items || [];
  const totalItems = data?.data?.total || 0;

  const startResult = totalItems > 0 ? (page - 1) * pageSize + 1 : 0;
  const endResult = Math.min(page * pageSize, totalItems);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      {/* Toast Notification */}
      {toast && (
        <div className="fixed top-6 right-6 z-50 animate-in fade-in slide-in-from-top-4 duration-300">
          <div
            className={`flex items-center gap-3 px-4 py-3 rounded-lg border shadow-lg ${
              toast.type === 'success'
                ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800'
                : toast.type === 'error'
                ? 'bg-rose-50 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border-rose-200 dark:border-rose-800'
                : 'bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border-amber-200 dark:border-amber-800'
            }`}
          >
            {toast.type === 'success' ? (
              <CheckCircle className="h-5 w-5 text-emerald-500 shrink-0" />
            ) : (
              <AlertTriangle className="h-5 w-5 text-rose-500 shrink-0" />
            )}
            <span className="text-sm font-medium">{toast.message}</span>
          </div>
        </div>
      )}

      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Shield className="text-[#1877F2] shrink-0 h-8 w-8" />
              {t('adminTitle')}
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              {t('adminDesc')}
            </p>
          </div>

          <Button
            variant="destructive"
            onClick={() => setIsPurgeModalOpen(true)}
            className="h-10 px-4 font-medium gap-2 cursor-pointer w-full sm:w-auto"
          >
            <Trash2 className="h-4 w-4" />
            {t('purgeLogsBtn')}
          </Button>
        </div>

        {/* TẦNG 1: TOOLBAR (Search, Operation, Category, Date Filters) */}
        <div className="flex flex-col gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-72 md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder={t('adminSearchPlaceholder')}
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1 cursor-pointer"
                  title={t('clearSearch')}
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>

            {/* Operation Filter */}
            <Select
              value={selectedOperation || 'ALL'}
              onValueChange={(val) => {
                setSelectedOperation(val === 'ALL' ? null : val);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[160px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder={t('operationPlaceholder')} />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">{t('allOperations')}</SelectItem>
                <SelectItem value="CREATE">CREATE</SelectItem>
                <SelectItem value="UPDATE">UPDATE</SelectItem>
                <SelectItem value="DELETE">DELETE</SelectItem>
              </SelectContent>
            </Select>

            {/* Category Filter */}
            <Select
              value={selectedCategory || 'ALL'}
              onValueChange={(val) => {
                setSelectedCategory(val === 'ALL' ? null : val);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[170px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder={t('categoryPlaceholder')} />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">{t('allCategories')}</SelectItem>
                <SelectItem value="DATA_MUTATION">DATA_MUTATION</SelectItem>
                <SelectItem value="SECURITY">SECURITY</SelectItem>
                <SelectItem value="AUTH">AUTH</SelectItem>
                <SelectItem value="SYSTEM">SYSTEM</SelectItem>
              </SelectContent>
            </Select>

            {/* Start Date */}
            <div className="flex items-center gap-1.5 w-full sm:w-auto">
              <Label htmlFor="start-date" className="text-xs text-[#65676B] dark:text-zinc-400 font-medium shrink-0 flex items-center gap-1">
                <Clock className="h-3 w-3" /> {t('fromLabel')}
              </Label>
              <Input
                id="start-date"
                type="date"
                value={startDateStr}
                onChange={(e) => {
                  setStartDateStr(e.target.value);
                  setPage(1);
                }}
                className="!h-10 w-full sm:w-[145px] border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs"
              />
            </div>

            {/* End Date */}
            <div className="flex items-center gap-1.5 w-full sm:w-auto">
              <Label htmlFor="end-date" className="text-xs text-[#65676B] dark:text-zinc-400 font-medium shrink-0 flex items-center gap-1">
                {t('toLabel')}
              </Label>
              <Input
                id="end-date"
                type="date"
                value={endDateStr}
                onChange={(e) => {
                  setEndDateStr(e.target.value);
                  setPage(1);
                }}
                className="!h-10 w-full sm:w-[145px] border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs"
              />
            </div>

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

          {dateError && (
            <div className="flex items-center gap-2 text-amber-600 dark:text-amber-400 text-xs">
              <AlertTriangle className="h-4 w-4 shrink-0" />
              <span>{dateError}</span>
            </div>
          )}
        </div>

        {/* TẦNG 2: MAIN TABLE CONTAINER (TABLE_STANDARD - SHADCN TABLE) */}
        <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
          <Table className="w-full text-left border-collapse table-fixed">
            {/* Table Header */}
            <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
              <TableRow className="hover:bg-transparent border-none">
                <TableHead className="w-[12%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colTimestamp')}
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colActor')}
                </TableHead>

                <TableHead className="w-[31%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colActionCategory')}
                </TableHead>

                <TableHead className="w-[11%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colOperation')}
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colTarget')}
                </TableHead>

                <TableHead className="w-[14%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colIpAddress')}
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
                      <Skeleton className="h-4 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md mb-1" />
                      <Skeleton className="h-3 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-28 bg-slate-100 dark:bg-zinc-800 rounded-md mb-1" />
                      <Skeleton className="h-3 w-12 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md mb-1" />
                      <Skeleton className="h-3 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-5 w-16 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-2 text-center">
                      <Skeleton className="h-8 w-8 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
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
                        {t('adminLoadFailDesc')}
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
              ) : items.length === 0 ? (
                // Empty State
                <TableRow>
                  <TableCell colSpan={7} className="h-72 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                        <SearchX className="h-6 w-6" />
                      </div>
                      <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                        {t('noLogsTitle')}
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        {isFilterActive
                          ? t('adminNoLogsFilterDesc')
                          : t('adminNoLogsEmptyDesc')}
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
                items.map((log: AuditLogDto) => {
                  const createdAtDate = new Date(log.createdAt);
                  return (
                    <TableRow
                      key={log.id}
                      className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                    >
                      {/* Timestamp (2-line Date & Time format) */}
                      <TableCell className="py-3.5 px-3 align-top">
                        <div className="flex flex-col">
                          <span className="text-xs font-semibold text-[#050505] dark:text-zinc-200">
                            {format(createdAtDate, 'MM/dd/yyyy')}
                          </span>
                          <span className="text-[11px] font-mono text-[#65676B] dark:text-zinc-400">
                            {format(createdAtDate, 'hh:mm:ss a')}
                          </span>
                        </div>
                      </TableCell>

                      {/* Actor (Email / Role) */}
                      <TableCell className="py-3.5 px-3 align-top">
                        <div className="flex flex-col gap-0.5">
                          <span className="font-semibold text-xs text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors truncate max-w-[140px]" title={log.actorEmail}>
                            {log.actorEmail}
                          </span>
                          <span className="text-[10px] text-[#1877F2] dark:text-blue-400 font-bold uppercase tracking-wider">
                            {log.actorRole}
                          </span>
                        </div>
                      </TableCell>

                      {/* Action & Category */}
                      <TableCell className="py-3.5 px-3 align-top">
                        <div className="flex flex-col gap-1 items-start">
                          {renderActionText(log.action)}
                          <span className={getCategoryBadgeColor(log.actionCategory)}>
                            {log.actionCategory}
                          </span>
                        </div>
                      </TableCell>

                      {/* Operation */}
                      <TableCell className="py-3.5 px-3 align-top text-center">
                        <div className="flex justify-center">
                          {log.operationType ? (
                            <span className={getOperationBadgeColor(log.operationType)}>
                              {log.operationType}
                            </span>
                          ) : (
                            <span className="text-[#65676B] text-xs">-</span>
                          )}
                        </div>
                      </TableCell>

                      {/* Target (Table) */}
                      <TableCell className="py-3.5 px-3 align-top">
                        {log.tableName ? (
                          <span className="font-mono text-[10px] bg-slate-100 dark:bg-zinc-800 text-[#050505] dark:text-zinc-200 px-2 py-0.5 rounded-md border border-[#CED0D4]/60 dark:border-zinc-700">
                            {log.tableName}
                          </span>
                        ) : (
                          <span className="text-[#65676B] italic text-xs">N/A</span>
                        )}
                      </TableCell>

                      {/* IP Address */}
                      <TableCell className="py-3.5 px-3 align-top font-mono text-xs text-[#65676B] dark:text-zinc-300">
                        {log.ipAddress || '-'}
                      </TableCell>

                      {/* Actions (Icon-only button) */}
                      <TableCell className="py-3.5 px-2 align-top text-center">
                        <div className="flex justify-center">
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => setSelectedLog(log)}
                            className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                            title={t('viewLogTitle')}
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </div>

        {/* TẦNG 3: PAGINATION FOOTER */}
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-1 px-1">
          <div className="flex items-center space-x-3 text-sm text-[#65676B] dark:text-zinc-400">
            <div dangerouslySetInnerHTML={{ __html: t.raw('showingText').replace('{start}', startResult.toString()).replace('{end}', endResult.toString()).replace('{total}', totalItems.toString()) }} />
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
      </div>

      {/* Log Detail Modal */}
      <LogDetailsModal
        log={selectedLog}
        onClose={() => setSelectedLog(null)}
        getOperationBadgeColor={getOperationBadgeColor}
        getCategoryBadgeColor={getCategoryBadgeColor}
      />

      {/* Purge Confirmation Modal */}
      <PurgeModal
        isOpen={isPurgeModalOpen}
        onClose={() => setIsPurgeModalOpen(false)}
        onSubmit={handlePurgeSubmit}
        isPending={purgeMutation.isPending}
        purgeDays={purgeDays}
        setPurgeDays={setPurgeDays}
        getPastDateStr={getPastDateStr}
      />
    </div>
  );
}
