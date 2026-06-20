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
  X
} from 'lucide-react';
import { useUserActivityLogs } from '@/hooks/useUserGovernance';

export default function StaffAuditLogsPage() {
  // Audit Logs Filters
  const [auditSearch, setAuditSearch] = useState('');
  const [debouncedAuditSearch, setDebouncedAuditSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [selectedAuditStatus, setSelectedAuditStatus] = useState<string | null>(null);
  const [auditPage, setAuditPage] = useState(1);
  const auditPageSize = 10;

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
  } = useUserActivityLogs({
    page: auditPage,
    pageSize: auditPageSize,
    search: debouncedAuditSearch || undefined,
    category: selectedCategory || undefined,
    status: selectedAuditStatus || undefined,
  });

  const handleResetFilters = () => {
    setAuditSearch('');
    setSelectedCategory(null);
    setSelectedAuditStatus(null);
    setAuditPage(1);
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
      <div className="grid grid-cols-1 md:grid-cols-4 gap-3 bg-card border border-border p-4 rounded-2xl shadow-xs">
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
      {(auditSearch || selectedCategory || selectedAuditStatus) && (
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
                  <th className="px-4 py-4 w-[130px]">Danh mục</th>
                  <th className="px-4 py-4">Hành động</th>
                  <th className="px-4 py-4 w-[110px]">Trạng thái</th>
                  <th className="px-4 py-4">IP Address</th>
                  <th className="px-4 py-4 max-w-[200px] truncate">Thiết bị (User Agent)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {auditData.data.items.map((log) => (
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
                        <span className="text-xs text-muted-foreground font-medium uppercase tracking-wider">{log.actorRole}</span>
                      </div>
                    </td>
                    <td className="px-4 py-4">
                      <span className="inline-flex px-2 py-0.5 rounded text-xs font-bold bg-muted border border-border text-foreground">
                        {log.actionCategory}
                      </span>
                    </td>
                    <td className="px-4 py-4 text-foreground font-medium">{log.action}</td>
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
                    <td className="px-4 py-4 text-xs text-muted-foreground max-w-[200px] truncate" title={log.userAgent}>
                      {log.userAgent}
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
              className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors"
            >
              <ChevronLeft size={16} />
            </button>
            {Array.from({ length: auditTotalPages }).map((_, i) => {
              const pg = i + 1;
              return (
                <button
                  key={pg}
                  onClick={() => setAuditPage(pg)}
                  className={`w-9 h-9 rounded-xl text-sm font-bold transition-all ${
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
              className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
