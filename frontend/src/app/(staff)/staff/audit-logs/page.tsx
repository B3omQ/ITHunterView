'use client';

import React, { useState, useEffect } from 'react';
import {
  Search,
  CheckCircle,
  XCircle,
  ChevronLeft,
  ChevronRight,
  Loader2,
  ClipboardList,
  RotateCcw,
  X,
  Eye,
  Clock
} from 'lucide-react';
import { useAuditLogs } from '@/hooks/useAuditLogs';
import { AuditLogDto } from '@/types/audit-log.types';
import { LogDetailsModal } from '@/app/(admin)/admin/audit-logs/components/log-details-modal';
import { Button } from '@/components/ui/button';

export default function StaffAuditLogsPage() {
  // Audit Logs Filters
  const [auditSearch, setAuditSearch] = useState('');
  const [debouncedAuditSearch, setDebouncedAuditSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [selectedAuditStatus, setSelectedAuditStatus] = useState<string | null>(null);
  const [selectedOperation, setSelectedOperation] = useState<string | null>(null);
  const [auditPage, setAuditPage] = useState(1);
  const auditPageSize = 10;

  // Selected Log for detail Modal
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);

  // Debounce search query
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedAuditSearch(auditSearch);
      setAuditPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [auditSearch]);

  // Fetch Audit Logs
  const {
    data: auditData,
    isLoading: isAuditLoading,
    isError: isAuditError,
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
      case 'ACCESS':
        return 'bg-cyan-500/5 text-cyan-700 dark:text-cyan-400 border border-cyan-500/20';
      default:
        return 'bg-muted text-muted-foreground border border-border';
    }
  };

  const auditTotalPages = auditData?.data?.totalPages || 0;
  const auditTotal = auditData?.data?.total || 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-border pb-5">
        <div>
          <h1 className="text-2xl font-black tracking-tight text-foreground flex items-center gap-2">
            <ClipboardList className="text-primary shrink-0" size={28} />
            Nhật ký giám sát hệ thống (Surveillance Logs)
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Ghi nhận toàn bộ hoạt động đăng nhập, cập nhật hồ sơ, thay đổi dữ liệu hoặc thay đổi an ninh trong hệ thống.
          </p>
        </div>
      </div>

      {/* Filters Bar */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-3 bg-card border border-border p-4 rounded-2xl shadow-xs">
        {/* Search */}
        <div className="relative md:col-span-2">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
          <input
            type="text"
            placeholder="Tìm theo email người thực hiện, hành động, IP..."
            value={auditSearch}
            onChange={(e) => setAuditSearch(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-border rounded-xl bg-background text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground"
          />
          {auditSearch && (
            <button
              onClick={() => setAuditSearch('')}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground p-0.5 rounded-full hover:bg-muted"
            >
              <X size={14} />
            </button>
          )}
        </div>

        {/* Operation Filter */}
        <select
          value={selectedOperation || ''}
          onChange={(e) => {
            setSelectedOperation(e.target.value || null);
            setAuditPage(1);
          }}
          className="py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
        >
          <option value="">Tất cả thao tác (CUD)</option>
          <option value="CREATE">CREATE (Tạo mới)</option>
          <option value="UPDATE">UPDATE (Sửa đổi)</option>
          <option value="DELETE">DELETE (Xoá bỏ)</option>
        </select>

        {/* Category Filter */}
        <select
          value={selectedCategory || ''}
          onChange={(e) => {
            setSelectedCategory(e.target.value || null);
            setAuditPage(1);
          }}
          className="py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
        >
          <option value="">Tất cả danh mục</option>
          <option value="AUTH">Xác thực (AUTH)</option>
          <option value="DATA_MUTATION">Thay đổi dữ liệu (DATA)</option>
          <option value="SECURITY">An ninh (SECURITY)</option>
          <option value="SYSTEM">Hệ thống (SYSTEM)</option>
        </select>

        {/* Status Filter */}
        <select
          value={selectedAuditStatus || ''}
          onChange={(e) => {
            setSelectedAuditStatus(e.target.value || null);
            setAuditPage(1);
          }}
          className="py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
        >
          <option value="">Tất cả trạng thái</option>
          <option value="SUCCESS">Thành công (SUCCESS)</option>
          <option value="FAIL">Thất bại (FAIL)</option>
        </select>
      </div>

      {/* Reset Filters button if any filter is active */}
      {(auditSearch || selectedCategory || selectedAuditStatus || selectedOperation) && (
        <div className="flex justify-end">
          <button
            onClick={handleResetFilters}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-border hover:bg-muted text-muted-foreground hover:text-foreground font-semibold text-xs rounded-lg transition-colors"
          >
            <RotateCcw size={12} />
            <span>Đặt lại bộ lọc</span>
          </button>
        </div>
      )}

      {/* Table Container */}
      <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-xs">
        {isAuditLoading ? (
          <div className="py-20 flex flex-col items-center justify-center text-muted-foreground gap-3">
            <Loader2 className="animate-spin text-primary" size={32} />
            <span className="text-sm font-medium">Đang tải nhật ký hoạt động...</span>
          </div>
        ) : isAuditError ? (
          <div className="py-20 text-center text-rose-500 font-medium">
            Không thể tải dữ liệu nhật ký. Vui lòng thử lại.
          </div>
        ) : !auditData?.data?.items?.length ? (
          <div className="py-20 text-center text-muted-foreground">
            Không tìm thấy nhật ký hoạt động nào khớp với bộ lọc.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40 font-bold text-muted-foreground">
                  <th className="px-4 py-4 w-[160px]">Thời gian</th>
                  <th className="px-4 py-4">Tác nhân</th>
                  <th className="px-4 py-4 text-center">Thao tác</th>
                  <th className="px-4 py-4">Hành động</th>
                  <th className="px-4 py-4">Đối tượng (Bảng)</th>
                  <th className="px-4 py-4 w-[110px]">Trạng thái</th>
                  <th className="px-4 py-4">IP Address</th>
                  <th className="px-4 py-4 text-center">Chi tiết</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60 text-xs">
                {auditData.data.items.map((log: AuditLogDto) => (
                  <tr key={log.id} className="hover:bg-muted/10 transition-colors">
                    <td className="px-4 py-4 text-muted-foreground font-mono">
                      {new Date(log.createdAt).toLocaleString('vi-VN', {
                        year: 'numeric',
                        month: '2-digit',
                        day: '2-digit',
                        hour: '2-digit',
                        minute: '2-digit',
                        second: '2-digit'
                      })}
                    </td>
                    <td className="px-4 py-4">
                      <div className="flex flex-col">
                        <span className="font-semibold text-foreground">{log.actorEmail}</span>
                        <span className="text-[10px] text-primary font-bold uppercase mt-0.5 tracking-wider">{log.actorRole}</span>
                      </div>
                    </td>
                    <td className="px-4 py-4 text-center">
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
                    <td className="px-4 py-4">
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
                    <td className="px-4 py-4">
                      {log.tableName ? (
                        <span className="font-mono text-[10px] bg-muted px-2 py-1 rounded text-muted-foreground border border-border">
                          {log.tableName}
                        </span>
                      ) : (
                        <span className="text-muted-foreground italic text-[11px]">
                          Không áp dụng
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-4">
                      {log.status === 'SUCCESS' ? (
                        <span className="inline-flex items-center gap-0.5 px-2 py-0.5 rounded text-xs font-bold bg-emerald-500/10 text-emerald-500">
                          <CheckCircle size={10} />
                          <span>SUCCESS</span>
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-0.5 px-2 py-0.5 rounded text-xs font-bold bg-rose-500/10 text-rose-500">
                          <XCircle size={10} />
                          <span>FAIL</span>
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-4 text-muted-foreground font-mono text-xs">{log.ipAddress}</td>
                    <td className="px-4 py-4 text-center">
                      <Button
                        variant="outline"
                        size="icon-sm"
                        onClick={() => setSelectedLog(log)}
                        className="text-muted-foreground hover:text-foreground cursor-pointer"
                        title="Xem chi tiết"
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Audit Pagination */}
      {auditData?.data && auditTotalPages > 1 && (
        <div className="flex items-center justify-between border border-border bg-card p-4 rounded-2xl shadow-xs">
          <span className="text-sm text-muted-foreground">
            Hiển thị trang <strong className="text-foreground">{auditPage}</strong> trên tổng số{' '}
            <strong className="text-foreground">{auditTotalPages}</strong> trang ({auditTotal} kết quả)
          </span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setAuditPage((p) => Math.max(p - 1, 1))}
              disabled={auditPage === 1}
              className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors cursor-pointer"
            >
              <ChevronLeft size={16} />
            </button>
            {Array.from({ length: auditTotalPages }).map((_, i) => {
              const pg = i + 1;
              return (
                <button
                  key={pg}
                  onClick={() => setAuditPage(pg)}
                  className={`w-9 h-9 rounded-xl text-sm font-bold transition-all cursor-pointer ${
                    auditPage === pg
                      ? 'bg-primary text-primary-foreground'
                      : 'border border-border hover:bg-muted text-foreground'
                  }`}
                >
                  {pg}
                </button>
              );
            })}
            <button
              onClick={() => setAuditPage((p) => Math.min(p + 1, auditTotalPages))}
              disabled={auditPage === auditTotalPages}
              className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors cursor-pointer"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      )}

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
