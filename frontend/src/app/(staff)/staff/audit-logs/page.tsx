'use client';

import React, { useState, useEffect } from 'react';
import {
  Search,
  CheckCircle,
  XCircle,
  ChevronLeft,
  ChevronRight,
  ClipboardList,
  RotateCcw,
  X,
  Eye,
  SearchX,
} from 'lucide-react';
import { useAuditLogs } from '@/hooks/useAuditLogs';
import { AuditLogDto } from '@/types/audit-log.types';
import { LogDetailsModal } from '@/app/(admin)/admin/audit-logs/components/log-details-modal';
import { format } from 'date-fns';
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
import { useTranslations } from 'next-intl';

export default function StaffAuditLogsPage() {
  const t = useTranslations('StaffAuditLogs');
  // Audit Logs Filters
  const [auditSearch, setAuditSearch] = useState('');
  const [debouncedAuditSearch, setDebouncedAuditSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [selectedAuditStatus, setSelectedAuditStatus] = useState<string | null>(null);
  const [selectedOperation, setSelectedOperation] = useState<string | null>(null);
  const [auditPage, setAuditPage] = useState(1);
  const [auditPageSize, setAuditPageSize] = useState(10);

  // Selected Log for detail Modal
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);

  // Debounce search query
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedAuditSearch(auditSearch);
      setAuditPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [auditSearch]);

  // Fetch Audit Logs complying with kinh-mantra.md (page -> hook -> service -> api-client -> backend)
  const {
    data: auditData,
    isLoading: isAuditLoading,
    isError: isAuditError,
    refetch,
    isFetching,
  } = useAuditLogs({
    page: auditPage,
    pageSize: auditPageSize,
    search: debouncedAuditSearch || undefined,
    category: selectedCategory || undefined,
    status: selectedAuditStatus || undefined,
    operationType: selectedOperation || undefined,
  });

  const handleResetFilters = () => {
    setAuditSearch('');
    setSelectedCategory(null);
    setSelectedAuditStatus(null);
    setSelectedOperation(null);
    setAuditPage(1);
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
    auditSearch !== '' ||
    selectedCategory !== null ||
    selectedAuditStatus !== null ||
    selectedOperation !== null;

  const auditTotalPages = auditData?.data?.totalPages || 1;
  const auditTotal = auditData?.data?.total || 0;
  const logsList = auditData?.data?.items || [];

  const startResult = auditTotal > 0 ? (auditPage - 1) * auditPageSize + 1 : 0;
  const endResult = Math.min(auditPage * auditPageSize, auditTotal);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <ClipboardList className="text-[#1877F2] shrink-0 h-8 w-8" />
              {t('staffTitle')}
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              {t('staffDesc')}
            </p>
          </div>
        </div>

        {/* TẦNG 1: TOOLBAR (Search, Operation, Category, Status Filters, Reset, Refresh) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-72 md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={auditSearch}
                onChange={(e) => setAuditSearch(e.target.value)}
                placeholder={t('searchPlaceholder')}
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {auditSearch && (
                <button
                  onClick={() => setAuditSearch('')}
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
                setAuditPage(1);
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
                setAuditPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[160px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder={t('categoryPlaceholder')} />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">{t('allCategories')}</SelectItem>
                <SelectItem value="AUTH">{t('catAuthentication')}</SelectItem>
                <SelectItem value="DATA_MUTATION">{t('catDataMutation')}</SelectItem>
                <SelectItem value="SECURITY">{t('catSecurity')}</SelectItem>
                <SelectItem value="SYSTEM">{t('catSystem')}</SelectItem>
              </SelectContent>
            </Select>

            {/* Status Filter */}
            <Select
              value={selectedAuditStatus || 'ALL'}
              onValueChange={(val) => {
                setSelectedAuditStatus(val === 'ALL' ? null : val);
                setAuditPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[150px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder={t('statusPlaceholder')} />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">{t('allStatuses')}</SelectItem>
                <SelectItem value="SUCCESS">{t('statusSuccess')}</SelectItem>
                <SelectItem value="FAIL">{t('statusFail')}</SelectItem>
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
                <TableHead className="w-[12%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colTimestamp')}
                </TableHead>

                <TableHead className="w-[9%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colActor')}
                </TableHead>

                <TableHead className="w-[33%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colActionCategory')}
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colOperation')}
                </TableHead>

                <TableHead className="w-[13%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colTarget')}
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colStatus')}
                </TableHead>

                <TableHead className="w-[9%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t('colActions')}
                </TableHead>
              </TableRow>
            </TableHeader>

            {/* Table Body */}
            <TableBody>
              {isAuditLoading ? (
                // Loading Skeleton State (6 rows)
                Array.from({ length: auditPageSize || 6 }).map((_, index) => (
                  <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-28 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <div className="space-y-1">
                        <Skeleton className="h-4 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        <Skeleton className="h-3 w-1/3 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                      </div>
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <div className="space-y-1">
                        <Skeleton className="h-4 w-2/3 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        <Skeleton className="h-3 w-16 bg-slate-100 dark:bg-zinc-800 rounded-full" />
                      </div>
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-5 w-16 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-5 w-16 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-2 text-center">
                      <Skeleton className="h-8 w-8 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
                    </TableCell>
                  </TableRow>
                ))
              ) : isAuditError ? (
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
                    </div>
                  </TableCell>
                </TableRow>
              ) : logsList.length === 0 ? (
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
                          ? t('noLogsFilterDesc')
                          : t('noLogsEmptyDesc')}
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
                logsList.map((log: AuditLogDto) => (
                  <TableRow
                    key={log.id}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Timestamp */}
                    <TableCell className="py-3 px-3 align-top font-mono text-xs">
                      <div className="flex flex-col gap-0.5 mt-0.5">
                        <span className="font-semibold text-[#050505] dark:text-zinc-100">
                          {format(new Date(log.createdAt), 'MM/dd/yyyy')}
                        </span>
                        <span className="text-[11px] text-[#65676B] dark:text-zinc-400">
                          {format(new Date(log.createdAt), 'hh:mm:ss a')}
                        </span>
                      </div>
                    </TableCell>

                    {/* Actor */}
                    <TableCell className="py-3 px-3 align-top">
                      <div className="flex flex-col gap-0.5 mt-0.5">
                        <span className="font-bold text-xs text-[#050505] dark:text-zinc-100 truncate max-w-[180px]" title={log.actorEmail}>
                          {log.actorEmail}
                        </span>
                        <span className="text-[10px] text-[#1877F2] font-bold uppercase tracking-wider">
                          {log.actorRole}
                        </span>
                      </div>
                    </TableCell>

                    {/* Action & Category */}
                    <TableCell className="py-3 px-3 align-top">
                      <div className="flex flex-col gap-1 items-start mt-0.5">
                        {renderActionText(log.action)}
                        <span className={getCategoryBadgeColor(log.actionCategory)}>
                          {log.actionCategory}
                        </span>
                      </div>
                    </TableCell>

                    {/* Operation */}
                    <TableCell className="py-3 px-3 align-top text-center">
                      <div className="mt-0.5 flex justify-center">
                        {log.operationType ? (
                          <span className={getOperationBadgeColor(log.operationType)}>
                            {log.operationType}
                          </span>
                        ) : (
                          <span className="text-[#65676B] dark:text-zinc-400 text-xs">-</span>
                        )}
                      </div>
                    </TableCell>

                    {/* Target Table */}
                    <TableCell className="py-3 px-3 align-top">
                      <div className="mt-0.5">
                        {log.tableName ? (
                          <span className="font-mono text-[11px] bg-zinc-100 dark:bg-zinc-800 px-2 py-0.5 rounded text-zinc-700 dark:text-zinc-300 border border-zinc-200 dark:border-zinc-700">
                            {log.tableName}
                          </span>
                        ) : (
                          <span className="text-[#65676B] dark:text-zinc-400 italic text-xs">
                            N/A
                          </span>
                        )}
                      </div>
                    </TableCell>

                    {/* Status */}
                    <TableCell className="py-3 px-3 align-top text-center">
                      <div className="mt-0.5 flex justify-center">
                        {log.status === 'SUCCESS' ? (
                          <Badge className="bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
                            <CheckCircle size={11} />
                            <span>SUCCESS</span>
                          </Badge>
                        ) : (
                          <Badge className="bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
                            <XCircle size={11} />
                            <span>FAIL</span>
                          </Badge>
                        )}
                      </div>
                    </TableCell>

                    {/* Actions */}
                    <TableCell className="py-3 px-2 align-top text-center">
                      <div className="mt-0.5 flex justify-center">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setSelectedLog(log)}
                          className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                          title={t('viewDetailsTitle')}
                        >
                          <Eye className="h-4 w-4" />
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
            <div dangerouslySetInnerHTML={{ __html: t.raw('showingText').replace('{start}', startResult.toString()).replace('{end}', endResult.toString()).replace('{total}', auditTotal.toString()) }} />
            <Select
              value={String(auditPageSize)}
              onValueChange={(val) => {
                if (val) setAuditPageSize(Number(val));
                setAuditPage(1);
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
              disabled={auditPage === 1 || isAuditLoading}
              onClick={() => setAuditPage((prev) => Math.max(1, prev - 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>

            {Array.from({ length: auditTotalPages }).map((_, index) => {
              const pageNum = index + 1;
              if (
                auditTotalPages <= 5 ||
                pageNum === 1 ||
                pageNum === auditTotalPages ||
                Math.abs(pageNum - auditPage) <= 1
              ) {
                const isCurrent = pageNum === auditPage;
                return (
                  <Button
                    key={pageNum}
                    variant={isCurrent ? 'default' : 'outline'}
                    disabled={isAuditLoading}
                    onClick={() => setAuditPage(pageNum)}
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
                (pageNum === 2 && auditPage > 3) ||
                (pageNum === auditTotalPages - 1 && auditPage < auditTotalPages - 2)
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
              disabled={auditPage >= auditTotalPages || isAuditLoading}
              onClick={() => setAuditPage((prev) => Math.min(auditTotalPages, prev + 1))}
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
    </div>
  );
}
