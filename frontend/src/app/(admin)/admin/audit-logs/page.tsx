'use client';

import React, { useState, useEffect } from 'react';
import {
  Search,
  Clock,
  Database,
  Trash2,
  Eye,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Shield,
  AlertTriangle,
  CheckCircle,
  RefreshCw,
} from 'lucide-react';
import { useAuditLogs, usePurgeAuditLogs } from '@/hooks/useAuditLogs';
import { AuditLogDto } from '@/types/audit-log.types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LogDetailsModal } from './components/log-details-modal';
import { PurgeModal } from './components/purge-modal';

export default function AuditLogsPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedOperation, setSelectedOperation] = useState<string>('');
  const [selectedCategory, setSelectedCategory] = useState<string>('');

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
  const pageSize = 10;

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
    }, 400);
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

  // Fetch data
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
      showToast('Minimum retention period is 1 day.', 'error');
      return;
    }

    purgeMutation.mutate(days, {
      onSuccess: (res) => {
        if (res.success) {
          showToast(res.message || `Logs older than ${days} days successfully purged.`, 'success');
          setIsPurgeModalOpen(false);
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

  const getOperationBadgeColor = (op: string | null) => {
    switch (op?.toUpperCase()) {
      case 'CREATE':
        return 'bg-emerald-500/5 text-emerald-700 dark:text-emerald-400 border border-emerald-500/20';
      case 'UPDATE':
        return 'bg-blue-500/5 text-blue-700 dark:text-blue-400 border border-blue-500/20';
      case 'DELETE':
        return 'bg-rose-500/5 text-rose-700 dark:text-rose-400 border border-rose-500/20';
      default:
        return 'bg-muted text-muted-foreground border border-border';
    }
  };

  const getCategoryBadgeColor = (cat: string) => {
    switch (cat.toUpperCase()) {
      case 'DATA_MUTATION':
        return 'bg-amber-500/5 text-amber-700 dark:text-amber-400 border border-amber-500/20';
      case 'SECURITY':
        return 'bg-violet-500/5 text-violet-700 dark:text-violet-400 border border-violet-500/20';
      case 'AUTH':
        return 'bg-blue-500/5 text-blue-700 dark:text-blue-400 border border-blue-500/20';
      case 'SYSTEM':
        return 'bg-zinc-500/5 text-zinc-700 dark:text-zinc-400 border border-zinc-500/20';
      default:
        return 'bg-muted text-muted-foreground border border-border';
    }
  };

  const totalPages = data?.data?.totalPages || 0;
  const items = data?.data?.items || [];
  const totalItems = data?.data?.total || 0;

  return (
    <div className="min-h-screen bg-background text-foreground p-6">
      {/* Toast Notification */}
      {toast && (
        <div className="fixed top-6 right-6 z-50 animate-in fade-in slide-in-from-top-4 duration-300">
          <div
            className={`flex items-center gap-3 px-4 py-3 rounded-lg border ${
              toast.type === 'success'
                ? 'bg-emerald-500/5 text-emerald-700 dark:text-emerald-400 border-emerald-500/20'
                : toast.type === 'error'
                ? 'bg-rose-500/5 text-rose-700 dark:text-rose-400 border-rose-500/20'
                : 'bg-amber-500/5 text-amber-700 dark:text-amber-400 border-amber-500/20'
            }`}
          >
            {toast.type === 'success' ? (
              <CheckCircle className="h-5 w-5 text-emerald-500" />
            ) : (
              <AlertTriangle className="h-5 w-5 text-rose-500" />
            )}
            <span className="text-sm font-medium">{toast.message}</span>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <Shield className="h-7 w-7 text-primary" />
            Platform Safety & Audit Logs
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Record and monitor data mutation (CUD) behaviors and core security events of the system.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="icon"
            onClick={() => refetch()}
            className="text-muted-foreground hover:text-foreground cursor-pointer"
            title="Refresh data"
          >
            <RefreshCw className="h-5 w-5" />
          </Button>
          <Button
            variant="destructive"
            onClick={() => setIsPurgeModalOpen(true)}
            className="font-medium cursor-pointer"
          >
            <Trash2 className="h-4 w-4" />
            Purge logs
          </Button>
        </div>
      </div>

      {/* Filters Board */}
      <div className="bg-card border border-border rounded-xl p-5 mb-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          {/* Keyword Search */}
          <div className="relative flex items-center">
            <Search className="absolute left-3 h-4 w-4 text-muted-foreground" />
            <Input
              type="text"
              placeholder="Email, action, table, IP..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9 cursor-text"
            />
          </div>

          {/* Operation type */}
          <div>
            <select
              value={selectedOperation}
              onChange={(e) => {
                setSelectedOperation(e.target.value);
                setPage(1);
              }}
              className="h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm text-foreground transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
            >
              <option className="bg-background text-foreground" value="">
                -- All Operations (CUD) --
              </option>
              <option className="bg-background text-foreground" value="CREATE">
                CREATE (Create)
              </option>
              <option className="bg-background text-foreground" value="UPDATE">
                UPDATE (Update)
              </option>
              <option className="bg-background text-foreground" value="DELETE">
                DELETE (Delete)
              </option>
            </select>
          </div>

          {/* Category */}
          <div>
            <select
              value={selectedCategory}
              onChange={(e) => {
                setSelectedCategory(e.target.value);
                setPage(1);
              }}
              className="h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm text-foreground transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
            >
              <option className="bg-background text-foreground" value="">
                -- All Categories --
              </option>
              <option className="bg-background text-foreground" value="DATA_MUTATION">
                DATA_MUTATION (Data Mutation)
              </option>
              <option className="bg-background text-foreground" value="SECURITY">
                SECURITY (Security)
              </option>
              <option className="bg-background text-foreground" value="AUTH">
                AUTH (Authentication)
              </option>
              <option className="bg-background text-foreground" value="SYSTEM">
                SYSTEM (System)
              </option>
            </select>
          </div>

          {/* Time Filter - Start Date */}
          <div className="flex flex-col gap-1">
            <Label htmlFor="start-date-input" className="text-xs text-muted-foreground flex items-center gap-1.5 font-medium">
              <Clock className="h-3 w-3" /> Start Date
            </Label>
            <Input
              id="start-date-input"
              type="date"
              value={startDateStr}
              onChange={(e) => {
                setStartDateStr(e.target.value);
                setPage(1);
              }}
              className="cursor-text"
            />
          </div>

          {/* Time Filter - End Date */}
          <div className="flex flex-col gap-1">
            <Label htmlFor="end-date-input" className="text-xs text-muted-foreground flex items-center gap-1.5 font-medium">
              <Clock className="h-3 w-3" /> End Date
            </Label>
            <Input
              id="end-date-input"
              type="date"
              value={endDateStr}
              onChange={(e) => {
                setEndDateStr(e.target.value);
                setPage(1);
              }}
              className="cursor-text"
            />
          </div>
        </div>

        {dateError && (
          <div className="flex items-center gap-2 mt-3 text-amber-600 dark:text-amber-400 text-xs">
            <AlertTriangle className="h-4 w-4 shrink-0" />
            <span>{dateError}</span>
          </div>
        )}
      </div>

      {/* Main Table Content */}
      <div className="bg-card border border-border rounded-xl overflow-hidden">
        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-20 gap-3">
            <Loader2 className="h-10 w-10 text-primary animate-spin" />
            <span className="text-muted-foreground text-sm">
              Loading audit logs...
            </span>
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center justify-center py-16 gap-3 text-rose-500">
            <AlertTriangle className="h-10 w-10 text-rose-500" />
            <span className="text-sm font-medium">An error occurred while loading log data.</span>
            <Button
              variant="outline"
              onClick={() => refetch()}
              className="text-xs font-semibold cursor-pointer"
            >
              Try again
            </Button>
          </div>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
            <Database className="h-12 w-12 text-muted-foreground/60 mb-3" />
            <span className="text-sm">No logs found matching the filters.</span>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm border-collapse">
                <thead className="bg-muted/40 text-muted-foreground uppercase text-xs font-bold border-b border-border">
                  <tr>
                    <th className="p-4">Time (UTC)</th>
                    <th className="p-4">Actor (Email / Role)</th>
                    <th className="p-4 text-center">Operation</th>
                    <th className="p-4">Action</th>
                    <th className="p-4">Target (Table)</th>
                    <th className="p-4">IP Address</th>
                    <th className="p-4 text-center">Details</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/60 text-xs">
                  {items.map((log: AuditLogDto) => (
                    <tr key={log.id} className="hover:bg-muted/10 transition-colors group">
                      <td className="p-4 font-mono text-[11px] text-muted-foreground">
                        {new Date(log.createdAt).toISOString().replace('T', ' ').substring(0, 19)}
                      </td>
                      <td className="p-4">
                        <div className="flex flex-col">
                          <span className="text-foreground font-medium">{log.actorEmail}</span>
                          <span className="text-[10px] text-primary font-semibold uppercase mt-0.5 tracking-wider">
                            {log.actorRole}
                          </span>
                        </div>
                      </td>
                      <td className="p-4 text-center">
                        {log.operationType ? (
                          <span
                            className={`inline-block px-2 py-0.5 rounded text-[10px] font-bold tracking-wider ${getOperationBadgeColor(
                              log.operationType
                            )}`}
                          >
                            {log.operationType}
                          </span>
                        ) : (
                          <span className="text-muted-foreground">-</span>
                        )}
                      </td>
                      <td className="p-4">
                        <div className="flex flex-col gap-1 items-start">
                          <span className="text-foreground font-medium">{log.action}</span>
                          <span
                            className={`px-1.5 py-0.5 rounded-full text-[9px] font-medium ${getCategoryBadgeColor(
                              log.actionCategory
                            )}`}
                          >
                            {log.actionCategory}
                          </span>
                        </div>
                      </td>
                      <td className="p-4">
                        {log.tableName ? (
                          <span className="font-mono text-[10px] bg-muted px-2 py-1 rounded text-muted-foreground border border-border">
                            {log.tableName}
                          </span>
                        ) : (
                          <span className="text-muted-foreground italic text-[11px]">
                            N/A
                          </span>
                        )}
                      </td>
                      <td className="p-4 font-mono text-muted-foreground">{log.ipAddress}</td>
                      <td className="p-4 text-center">
                        <Button
                          variant="outline"
                          size="icon-sm"
                          onClick={() => setSelectedLog(log)}
                          className="text-muted-foreground hover:text-foreground cursor-pointer"
                          title="View details"
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination UI */}
            <div className="flex items-center justify-between px-6 py-4 border-t border-border bg-card">
              <span className="text-xs text-muted-foreground">
                Showing{' '}
                <span className="font-semibold text-foreground">
                  {(page - 1) * pageSize + 1}
                </span>{' '}
                -{' '}
                <span className="font-semibold text-foreground">
                  {Math.min(page * pageSize, totalItems)}
                </span>{' '}
                of{' '}
                <span className="font-semibold text-foreground">{totalItems}</span> audit logs
              </span>

              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="icon-sm"
                  onClick={() => setPage((p) => Math.max(p - 1, 1))}
                  disabled={page === 1}
                  className="cursor-pointer"
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="text-xs text-foreground font-medium px-2">
                  Page {page} / {totalPages || 1}
                </span>
                <Button
                  variant="outline"
                  size="icon-sm"
                  onClick={() => setPage((p) => Math.min(p + 1, totalPages))}
                  disabled={page >= totalPages}
                  className="cursor-pointer"
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </>
        )}
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
