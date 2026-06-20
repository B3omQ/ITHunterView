'use client';

import React, { useState, useEffect } from 'react';
import {
  Search,
  Clock,
  Database,
  Filter,
  Trash2,
  Eye,
  ChevronLeft,
  ChevronRight,
  Loader2,
  X,
  Shield,
  FileText,
  AlertTriangle,
  CheckCircle,
  HelpCircle,
  RefreshCw
} from 'lucide-react';
import { useAuditLogs, usePurgeAuditLogs } from '@/hooks/useAuditLogs';
import { AuditLogDto } from '@/types/audit-log.types';

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
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'warning' } | null>(null);

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
        setDateError('Ngày bắt đầu không được sau ngày kết thúc.');
      } else if (diffDays > 30) {
        setDateError('Khoảng thời gian tối đa để truy xuất logs là 30 ngày.');
      } else {
        setDateError('');
      }
    } else {
      setDateError('');
    }
  }, [startDateStr, endDateStr]);

  // Formats Dates to ISO for API
  const startIso = startDateStr ? new Date(`${startDateStr}T00:00:00Z`).toISOString() : undefined;
  const endIso = endDateStr ? new Date(`${endDateStr}T23:59:59Z`).toISOString() : undefined;

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

  const handlePurgeSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (purgeDays < 1) {
      showToast('Số ngày lưu trữ tối thiểu phải là 1 ngày.', 'error');
      return;
    }

    purgeMutation.mutate(purgeDays, {
      onSuccess: (res) => {
        if (res.success) {
          showToast(res.message || `Đã dọn dẹp logs cũ hơn ${purgeDays} ngày thành công.`, 'success');
          setIsPurgeModalOpen(false);
        } else {
          showToast(res.message || 'Có lỗi xảy ra khi dọn dẹp logs.', 'error');
        }
      },
      onError: (err: any) => {
        showToast(err.response?.data?.message || 'Có lỗi xảy ra khi gọi API dọn dẹp logs.', 'error');
      }
    });
  };

  const getOperationBadgeColor = (op: string | null) => {
    switch (op?.toUpperCase()) {
      case 'CREATE':
        return 'bg-green-950/50 text-green-400 border border-green-800/50';
      case 'UPDATE':
        return 'bg-blue-950/50 text-blue-400 border border-blue-800/50';
      case 'DELETE':
        return 'bg-red-950/50 text-red-400 border border-red-800/50';
      default:
        return 'bg-gray-800/50 text-gray-400 border border-gray-700/50';
    }
  };

  const getCategoryBadgeColor = (cat: string) => {
    switch (cat.toUpperCase()) {
      case 'DATA_MUTATION':
        return 'bg-amber-950/40 text-amber-400 border border-amber-800/30';
      case 'SECURITY':
        return 'bg-purple-950/40 text-purple-400 border border-purple-800/30';
      case 'ACCESS':
        return 'bg-cyan-950/40 text-cyan-400 border border-cyan-800/30';
      default:
        return 'bg-slate-950/40 text-slate-400 border border-slate-800/30';
    }
  };

  // Render Diff Detail
  const renderSnapshotDiff = (diffStr: string | null, operation: string | null) => {
    if (!diffStr) return <p className="text-gray-400 italic text-sm">Không ghi nhận thay đổi cấu trúc/dữ liệu hoặc không có payload diff.</p>;
    
    try {
      const parsed = JSON.parse(diffStr);
      if (parsed.error) {
        return <p className="text-red-400 italic text-sm">{parsed.error}</p>;
      }

      if (parsed.changes) {
        const changeEntries = Object.entries(parsed.changes);
        if (changeEntries.length === 0) {
          return <p className="text-gray-400 italic text-sm">Không có trường nào bị sửa đổi giá trị.</p>;
        }
        return (
          <div className="overflow-hidden border border-gray-800 rounded-lg">
            <table className="w-full text-left text-sm text-gray-300">
              <thead className="bg-gray-900/80 text-gray-400 uppercase text-[10px] tracking-wider font-semibold">
                <tr>
                  <th className="p-3 border-b border-gray-800">Trường dữ liệu</th>
                  <th className="p-3 border-b border-gray-800 bg-red-950/10 text-red-400">Giá trị cũ</th>
                  <th className="p-3 border-b border-gray-800 bg-green-950/10 text-green-400">Giá trị mới</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-800/60 font-mono text-xs">
                {changeEntries.map(([field, diff]: [string, any]) => (
                  <tr key={field} className="hover:bg-gray-800/30 transition-colors">
                    <td className="p-3 font-medium text-gray-300 border-r border-gray-800/50">{field}</td>
                    <td className="p-3 bg-red-950/5 text-red-300 line-through max-w-xs break-all border-r border-gray-800/50">
                      {diff.old === null || diff.old === undefined ? <span className="text-gray-600 italic">null</span> : String(diff.old)}
                    </td>
                    <td className="p-3 bg-green-950/5 text-green-300 max-w-xs break-all">
                      {diff.new === null || diff.new === undefined ? <span className="text-gray-600 italic">null</span> : String(diff.new)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        );
      }

      if (parsed.values) {
        const valEntries = Object.entries(parsed.values);
        return (
          <div className="overflow-hidden border border-gray-800 rounded-lg">
            <table className="w-full text-left text-sm text-gray-300">
              <thead className="bg-gray-900/80 text-gray-400 uppercase text-[10px] tracking-wider font-semibold">
                <tr>
                  <th className="p-3 border-b border-gray-800">Trường dữ liệu</th>
                  <th className="p-3 border-b border-gray-800">Giá trị ghi nhận</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-800/60 font-mono text-xs">
                {valEntries.map(([field, val]) => (
                  <tr key={field} className="hover:bg-gray-800/30 transition-colors">
                    <td className="p-3 font-medium text-gray-300 border-r border-gray-800/50">{field}</td>
                    <td className={`p-3 max-w-lg break-all ${operation === 'CREATE' ? 'text-green-300 bg-green-950/5' : 'text-red-300 bg-red-950/5 line-through'}`}>
                      {val === null || val === undefined ? <span className="text-gray-600 italic">null</span> : String(val)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        );
      }
    } catch {
      // Fallback
    }

    return (
      <pre className="p-3 bg-gray-950 border border-gray-800 rounded-lg font-mono text-xs text-gray-300 overflow-x-auto whitespace-pre-wrap max-h-60">
        {diffStr}
      </pre>
    );
  };

  const totalPages = data?.data?.totalPages || 0;
  const items = data?.data?.items || [];
  const totalItems = data?.data?.total || 0;

  return (
    <div className="min-h-screen bg-[#0d0f12] text-gray-100 p-6">
      {/* Toast Notification */}
      {toast && (
        <div className="fixed top-6 right-6 z-50 animate-in fade-in slide-in-from-top-4 duration-300">
          <div className={`flex items-center gap-3 px-4 py-3 rounded-lg shadow-xl border ${
            toast.type === 'success' ? 'bg-green-950/90 text-green-300 border-green-800' :
            toast.type === 'error' ? 'bg-red-950/90 text-red-300 border-red-800' :
            'bg-amber-950/90 text-amber-300 border-amber-800'
          }`}>
            {toast.type === 'success' ? <CheckCircle className="h-5 w-5 text-green-400" /> : <AlertTriangle className="h-5 w-5 text-red-400" />}
            <span className="text-sm font-medium">{toast.message}</span>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <Shield className="h-7 w-7 text-indigo-500" />
            Platform Safety & Audit Logs
          </h1>
          <p className="text-sm text-gray-400 mt-1">
            Ghi nhận và giám sát hành vi biến đổi dữ liệu (CUD) và các sự kiện an ninh cốt lõi của hệ thống.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => refetch()}
            className="flex items-center justify-center p-2 rounded-lg bg-gray-900 border border-gray-800 hover:bg-gray-800 text-gray-300 hover:text-white transition-all cursor-pointer"
            title="Làm mới dữ liệu"
          >
            <RefreshCw className="h-5 w-5" />
          </button>
          <button
            onClick={() => setIsPurgeModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2 bg-red-950/50 hover:bg-red-900/60 text-red-400 hover:text-red-200 border border-red-800/40 hover:border-red-700/60 rounded-lg text-sm font-medium transition-all cursor-pointer"
          >
            <Trash2 className="h-4 w-4" />
            Dọn dẹp logs
          </button>
        </div>
      </div>

      {/* Filters Board */}
      <div className="bg-gray-900/40 border border-gray-800/80 rounded-xl p-5 mb-6 backdrop-blur-md">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          
          {/* Keyword Search */}
          <div className="relative">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-gray-500" />
            <input
              type="text"
              placeholder="Email, hành động, bảng, IP..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full bg-gray-950 border border-gray-800 rounded-lg pl-9 pr-4 py-2 text-sm focus:outline-none focus:border-indigo-500 text-gray-200 transition-colors"
            />
          </div>

          {/* Operation type */}
          <div>
            <select
              value={selectedOperation}
              onChange={(e) => { setSelectedOperation(e.target.value); setPage(1); }}
              className="w-full bg-gray-950 border border-gray-800 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-indigo-500 text-gray-300"
            >
              <option value="">-- Mọi thao tác (CUD) --</option>
              <option value="CREATE">CREATE (Tạo mới)</option>
              <option value="UPDATE">UPDATE (Sửa đổi)</option>
              <option value="DELETE">DELETE (Xoá bỏ)</option>
            </select>
          </div>

          {/* Category */}
          <div>
            <select
              value={selectedCategory}
              onChange={(e) => { setSelectedCategory(e.target.value); setPage(1); }}
              className="w-full bg-gray-950 border border-gray-800 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-indigo-500 text-gray-300"
            >
              <option value="">-- Mọi danh mục --</option>
              <option value="DATA_MUTATION">DATA_MUTATION (Thay đổi dữ liệu)</option>
              <option value="SECURITY">SECURITY (Bảo mật)</option>
              <option value="ACCESS">ACCESS (Truy cập)</option>
              <option value="SYSTEM">SYSTEM (Hệ thống)</option>
            </select>
          </div>

          {/* Time Filter - Start Date */}
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-1.5 text-xs text-gray-400">
              <Clock className="h-3 w-3" /> Ngày bắt đầu
            </div>
            <input
              type="date"
              value={startDateStr}
              onChange={(e) => { setStartDateStr(e.target.value); setPage(1); }}
              className="w-full bg-gray-950 border border-gray-800 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:border-indigo-500 text-gray-300"
            />
          </div>

          {/* Time Filter - End Date */}
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-1.5 text-xs text-gray-400">
              <Clock className="h-3 w-3" /> Ngày kết thúc
            </div>
            <input
              type="date"
              value={endDateStr}
              onChange={(e) => { setEndDateStr(e.target.value); setPage(1); }}
              className="w-full bg-gray-950 border border-gray-800 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:border-indigo-500 text-gray-300"
            />
          </div>
        </div>

        {dateError && (
          <div className="flex items-center gap-2 mt-3 text-amber-400 text-xs">
            <AlertTriangle className="h-4 w-4 shrink-0" />
            <span>{dateError}</span>
          </div>
        )}
      </div>

      {/* Main Table Content */}
      <div className="bg-gray-900/20 border border-gray-800/80 rounded-xl overflow-hidden backdrop-blur-sm">
        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-20 gap-3">
            <Loader2 className="h-10 w-10 text-indigo-500 animate-spin" />
            <span className="text-gray-400 text-sm">Đang tải danh sách logs kiểm toán...</span>
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center justify-center py-16 gap-3 text-red-400">
            <AlertTriangle className="h-10 w-10 text-red-500" />
            <span className="text-sm font-medium">Có lỗi xảy ra khi tải dữ liệu log.</span>
            <button
              onClick={() => refetch()}
              className="px-4 py-2 bg-gray-800 hover:bg-gray-700 text-gray-200 border border-gray-700 rounded-lg text-xs font-semibold cursor-pointer"
            >
              Thử lại
            </button>
          </div>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 text-gray-500">
            <Database className="h-12 w-12 text-gray-700 mb-3" />
            <span className="text-sm">Không tìm thấy bất kỳ logs nào thoả mãn bộ lọc.</span>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm text-gray-300">
                <thead className="bg-gray-900/60 text-gray-400 uppercase text-xs font-semibold">
                  <tr>
                    <th className="p-4">Thời gian (UTC)</th>
                    <th className="p-4">Actor (Email / Role)</th>
                    <th className="p-4 text-center">Thao tác</th>
                    <th className="p-4">Hành động</th>
                    <th className="p-4">Đối tượng (Bảng)</th>
                    <th className="p-4">Địa chỉ IP</th>
                    <th className="p-4 text-center">Chi tiết</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-800/60">
                  {items.map((log: AuditLogDto) => (
                    <tr key={log.id} className="hover:bg-gray-800/20 transition-colors group">
                      <td className="p-4 font-mono text-xs text-gray-400">
                        {new Date(log.createdAt).toISOString().replace('T', ' ').substring(0, 19)}
                      </td>
                      <td className="p-4">
                        <div className="flex flex-col">
                          <span className="text-gray-200 font-medium">{log.actorEmail}</span>
                          <span className="text-[10px] text-indigo-400 font-semibold uppercase mt-0.5 tracking-wider">
                            {log.actorRole}
                          </span>
                        </div>
                      </td>
                      <td className="p-4 text-center">
                        {log.operationType ? (
                          <span className={`inline-block px-2 py-0.5 rounded text-[10px] font-bold tracking-wider ${getOperationBadgeColor(log.operationType)}`}>
                            {log.operationType}
                          </span>
                        ) : (
                          <span className="text-gray-600">-</span>
                        )}
                      </td>
                      <td className="p-4">
                        <div className="flex flex-col gap-1">
                          <span className="text-gray-300 text-xs font-medium">{log.action}</span>
                          <span className={`inline-self-start px-1.5 py-0.5 rounded-full text-[9px] font-medium ${getCategoryBadgeColor(log.actionCategory)}`}>
                            {log.actionCategory}
                          </span>
                        </div>
                      </td>
                      <td className="p-4">
                        {log.tableName ? (
                          <span className="font-mono text-xs bg-gray-950 px-2 py-1 rounded text-gray-400 border border-gray-800">
                            {log.tableName}
                          </span>
                        ) : (
                          <span className="text-gray-600 italic text-xs">Không áp dụng</span>
                        )}
                      </td>
                      <td className="p-4 text-xs font-mono text-gray-400">
                        {log.ipAddress}
                      </td>
                      <td className="p-4 text-center">
                        <button
                          onClick={() => setSelectedLog(log)}
                          className="p-1.5 rounded-lg bg-gray-900 border border-gray-800 hover:bg-gray-800 hover:text-white text-gray-400 transition-colors cursor-pointer"
                          title="Xem chi tiết"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination UI */}
            <div className="flex items-center justify-between px-6 py-4 border-t border-gray-800">
              <span className="text-xs text-gray-400">
                Hiển thị <span className="font-medium text-gray-200">{(page - 1) * pageSize + 1}</span> - <span className="font-medium text-gray-200">{Math.min(page * pageSize, totalItems)}</span> trong tổng số <span className="font-medium text-gray-200">{totalItems}</span> bản ghi logs
              </span>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => setPage((p) => Math.max(p - 1, 1))}
                  disabled={page === 1}
                  className="p-2 rounded-lg bg-gray-950 border border-gray-800 text-gray-400 hover:text-white disabled:opacity-40 disabled:hover:text-gray-400 transition-colors cursor-pointer"
                >
                  <ChevronLeft className="h-4 w-4" />
                </button>
                <span className="text-xs text-gray-300 font-medium px-2">
                  Trang {page} / {totalPages || 1}
                </span>
                <button
                  onClick={() => setPage((p) => Math.min(p + 1, totalPages))}
                  disabled={page >= totalPages}
                  className="p-2 rounded-lg bg-gray-950 border border-gray-800 text-gray-400 hover:text-white disabled:opacity-40 disabled:hover:text-gray-400 transition-colors cursor-pointer"
                >
                  <ChevronRight className="h-4 w-4" />
                </button>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Log Detail Modal */}
      {selectedLog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="w-full max-w-4xl bg-gray-900 border border-gray-800 rounded-xl overflow-hidden shadow-2xl animate-in zoom-in-95 duration-200 flex flex-col max-h-[90vh]">
            
            {/* Modal Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-800">
              <div className="flex items-center gap-2.5">
                <FileText className="h-5 w-5 text-indigo-500" />
                <h3 className="text-base font-bold text-white">Chi tiết bản ghi nhật ký kiểm toán</h3>
              </div>
              <button
                onClick={() => setSelectedLog(null)}
                className="p-1 rounded bg-transparent hover:bg-gray-800 text-gray-400 hover:text-white transition-colors cursor-pointer"
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-6 overflow-y-auto space-y-6 flex-1">
              
              {/* Metadata Cards Grid */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                
                {/* Col 1 */}
                <div className="bg-gray-950 p-4 rounded-lg border border-gray-800/80 space-y-2.5">
                  <div className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">Tác nhân (Actor)</div>
                  <div>
                    <div className="text-xs font-semibold text-gray-200">{selectedLog.actorEmail}</div>
                    <div className="text-[10px] text-indigo-400 font-bold uppercase mt-1 tracking-wider">{selectedLog.actorRole}</div>
                  </div>
                </div>

                {/* Col 2 */}
                <div className="bg-gray-950 p-4 rounded-lg border border-gray-800/80 space-y-2.5">
                  <div className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">Môi trường mạng</div>
                  <div className="text-xs space-y-1">
                    <div><span className="text-gray-500">IP:</span> <span className="font-mono text-gray-300">{selectedLog.ipAddress}</span></div>
                    <div className="truncate" title={selectedLog.userAgent}>
                      <span className="text-gray-500">UA:</span> <span className="text-gray-400 font-mono text-[10px]">{selectedLog.userAgent}</span>
                    </div>
                  </div>
                </div>

                {/* Col 3 */}
                <div className="bg-gray-950 p-4 rounded-lg border border-gray-800/80 space-y-2.5">
                  <div className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">Thời gian ghi nhận</div>
                  <div className="text-xs text-gray-300">
                    <div>{new Date(selectedLog.createdAt).toLocaleString()}</div>
                    <div className="text-[10px] text-gray-500 font-mono mt-1">UTC: {selectedLog.createdAt}</div>
                  </div>
                </div>
              </div>

              {/* Action Description */}
              <div className="bg-gray-950 p-4 rounded-lg border border-gray-800/80 space-y-2">
                <div className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">Hành vi (Action)</div>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-sm font-semibold text-gray-200">{selectedLog.action}</p>
                    <div className="flex items-center gap-2 mt-2">
                      <span className={`px-2 py-0.5 rounded text-[9px] font-bold tracking-wider ${getCategoryBadgeColor(selectedLog.actionCategory)}`}>
                        {selectedLog.actionCategory}
                      </span>
                      {selectedLog.tableName && (
                        <span className="font-mono text-[10px] bg-gray-900 border border-gray-800 px-2 py-0.5 rounded text-gray-400">
                          Bảng: {selectedLog.tableName}
                        </span>
                      )}
                    </div>
                  </div>
                  {selectedLog.operationType && (
                    <span className={`px-3 py-1 rounded text-xs font-bold tracking-widest ${getOperationBadgeColor(selectedLog.operationType)}`}>
                      {selectedLog.operationType}
                    </span>
                  )}
                </div>
              </div>

              {/* Payload Diff (SnapshotDiff) */}
              <div className="space-y-2">
                <div className="text-[10px] font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5">
                  <Database className="h-3.5 w-3.5 text-indigo-400" />
                  Ảnh chụp dữ liệu thay đổi (Payload Diff)
                </div>
                {renderSnapshotDiff(selectedLog.snapshotDiff, selectedLog.operationType)}
              </div>
            </div>

            {/* Modal Footer */}
            <div className="flex items-center justify-end px-6 py-4 border-t border-gray-800 bg-gray-900/60">
              <button
                onClick={() => setSelectedLog(null)}
                className="px-4 py-2 bg-gray-800 hover:bg-gray-700 text-gray-200 border border-gray-700 rounded-lg text-sm font-medium transition-colors cursor-pointer"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Purge Confirmation Modal */}
      {isPurgeModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="w-full max-w-md bg-gray-900 border border-gray-800 rounded-xl overflow-hidden shadow-2xl animate-in zoom-in-95 duration-200">
            <form onSubmit={handlePurgeSubmit}>
              {/* Header */}
              <div className="flex items-center justify-between px-6 py-4 border-b border-gray-800">
                <div className="flex items-center gap-2.5 text-red-500">
                  <AlertTriangle className="h-5 w-5" />
                  <h3 className="text-base font-bold text-white">Xác nhận dọn dẹp logs</h3>
                </div>
                <button
                  type="button"
                  onClick={() => setIsPurgeModalOpen(false)}
                  className="p-1 rounded bg-transparent hover:bg-gray-800 text-gray-400 hover:text-white transition-colors cursor-pointer"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              {/* Body */}
              <div className="p-6 space-y-4">
                <div className="p-3 bg-red-950/20 border border-red-800/30 rounded-lg text-xs text-red-300 leading-relaxed">
                  <strong className="text-red-200 block mb-1">CẢNH BÁO QUAN TRỌNG:</strong>
                  Hành động này sẽ xoá hoàn toàn các bản ghi logs kiểm toán cũ hơn số ngày quy định. 
                  Dữ liệu logs sau khi bị xoá sẽ không thể khôi phục lại.
                </div>

                <div className="flex flex-col gap-1.5">
                  <label className="text-xs text-gray-400 font-medium">
                    Giữ lại logs trong phạm vi (ngày):
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="365"
                    value={purgeDays}
                    onChange={(e) => setPurgeDays(parseInt(e.target.value) || 0)}
                    className="w-full bg-gray-950 border border-gray-800 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-red-500 text-gray-200 transition-colors"
                  />
                  <p className="text-[10px] text-gray-500 mt-1">
                    Các logs có thời gian tạo trước ngày {getPastDateStr(purgeDays)} sẽ bị xoá vĩnh viễn.
                  </p>
                </div>
              </div>

              {/* Footer */}
              <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-800 bg-gray-900/60">
                <button
                  type="button"
                  onClick={() => setIsPurgeModalOpen(false)}
                  className="px-4 py-2 bg-gray-800 hover:bg-gray-700 text-gray-200 border border-gray-700 rounded-lg text-sm font-medium transition-colors cursor-pointer"
                >
                  Huỷ bỏ
                </button>
                <button
                  type="submit"
                  disabled={purgeMutation.isPending}
                  className="flex items-center gap-1.5 px-4 py-2 bg-red-600 hover:bg-red-500 text-white rounded-lg text-sm font-medium transition-colors disabled:opacity-50 cursor-pointer"
                >
                  {purgeMutation.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
                  Thực hiện xoá
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
