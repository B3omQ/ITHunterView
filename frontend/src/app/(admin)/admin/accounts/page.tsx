'use client';

import React, { useState, useEffect, useMemo } from 'react';
import Link from 'next/link';
import {
  Search,
  User,
  Shield,
  Clock,
  Ban,
  CheckCircle,
  XCircle,
  Eye,
  Edit2,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Users,
  ClipboardList,
  AlertTriangle,
  RotateCcw,
  X,
  FileText,
  Building
} from 'lucide-react';
import {
  useUsers,
  useUpdateUserStatus,
  useUpdateUserRole,
  useUserActivityLogs,
} from '@/hooks/useUserGovernance';
import { UserStatus, SystemRole, ActivityLogCategory } from '@/types/user-governance.types';

export default function AdminAccountsPage() {
  const [activeTab, setActiveTab] = useState<'accounts' | 'audit'>('accounts');

  // Accounts Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRole, setSelectedRole] = useState<number | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<string | null>(null);
  const [accountsPage, setAccountsPage] = useState(1);
  const accountsPageSize = 10;

  // Audit Logs Filters
  const [auditSearch, setAuditSearch] = useState('');
  const [debouncedAuditSearch, setDebouncedAuditSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [selectedAuditStatus, setSelectedAuditStatus] = useState<string | null>(null);
  const [auditPage, setAuditPage] = useState(1);
  const auditPageSize = 10;

  // Modals / Status Action State
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  const [statusTargetUser, setStatusTargetUser] = useState<{ id: string; email: string; currentStatus: UserStatus } | null>(null);
  const [newStatus, setNewStatus] = useState<UserStatus>('ACTIVE');
  const [statusReason, setStatusReason] = useState('');
  const [statusError, setStatusError] = useState('');

  // Modals / Role Action State
  const [isRoleModalOpen, setIsRoleModalOpen] = useState(false);
  const [roleTargetUser, setRoleTargetUser] = useState<{ id: string; email: string; currentRole: number | null } | null>(null);
  const [newRole, setNewRole] = useState<number>(SystemRole.Candidate);
  const [roleError, setRoleError] = useState('');

  // Toast notifications state
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

  // Debounces
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setAccountsPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedAuditSearch(auditSearch);
      setAuditPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [auditSearch]);

  // Fetch Accounts
  const {
    data: accountsData,
    isLoading: isAccountsLoading,
    isError: isAccountsError,
    refetch: refetchAccounts,
  } = useUsers({
    page: accountsPage,
    pageSize: accountsPageSize,
    search: debouncedSearch || undefined,
    roleId: selectedRole || undefined,
    status: selectedStatus || undefined,
  });

  // Fetch Audit Logs
  const {
    data: auditData,
    isLoading: isAuditLoading,
    isError: isAuditError,
    refetch: refetchAudit,
  } = useUserActivityLogs({
    page: auditPage,
    pageSize: auditPageSize,
    search: debouncedAuditSearch || undefined,
    category: selectedCategory || undefined,
    status: selectedAuditStatus || undefined,
  });

  // Mutations
  const updateStatusMutation = useUpdateUserStatus();
  const updateRoleMutation = useUpdateUserRole();

  // Status Change Submit
  const handleStatusSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!statusTargetUser) return;
    if (!statusReason.trim()) {
      setStatusError('Vui lòng nhập lý do cập nhật trạng thái.');
      return;
    }
    if (statusReason.trim().length < 5) {
      setStatusError('Lý do phải có ít nhất 5 ký tự.');
      return;
    }

    setStatusError('');
    updateStatusMutation.mutate(
      {
        id: statusTargetUser.id,
        dto: {
          status: newStatus,
          reason: statusReason.trim(),
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast('Cập nhật trạng thái người dùng thành công!', 'success');
            setIsStatusModalOpen(false);
            setStatusTargetUser(null);
            setStatusReason('');
          } else {
            setStatusError(res.message || 'Cập nhật trạng thái thất bại.');
          }
        },
        onError: (err: any) => {
          setStatusError(err.response?.data?.message || 'Có lỗi xảy ra khi cập nhật.');
        },
      }
    );
  };

  // Role Change Submit
  const handleRoleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!roleTargetUser) return;

    setRoleError('');
    updateRoleMutation.mutate(
      {
        id: roleTargetUser.id,
        dto: {
          roleId: newRole,
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast('Cập nhật vai trò người dùng thành công!', 'success');
            setIsRoleModalOpen(false);
            setRoleTargetUser(null);
          } else {
            setRoleError(res.message || 'Cập nhật vai trò thất bại.');
          }
        },
        onError: (err: any) => {
          setRoleError(err.response?.data?.message || 'Có lỗi xảy ra khi cập nhật.');
        },
      }
    );
  };

  const getStatusBadge = (status: UserStatus) => {
    switch (status) {
      case 'ACTIVE':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
            <CheckCircle size={12} />
            <span>Đang hoạt động</span>
          </span>
        );
      case 'INACTIVE':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-500/10 text-zinc-500 border border-zinc-500/20">
            <Clock size={12} />
            <span>Tạm ngưng</span>
          </span>
        );
      case 'BANNED':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-500/10 text-rose-500 border border-rose-500/20">
            <Ban size={12} />
            <span>Bị khóa</span>
          </span>
        );
      case 'PENDING_VERIFICATION':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-500 border border-amber-500/20">
            <AlertTriangle size={12} />
            <span>Chờ xác minh</span>
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-muted text-muted-foreground border border-border">
            <span>{status}</span>
          </span>
        );
    }
  };

  const getRoleBadge = (roleName: string) => {
    const name = roleName.toLowerCase();
    if (name.includes('admin')) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-bold bg-purple-500/10 text-purple-600 dark:text-purple-400 border border-purple-500/10">
          <Shield size={11} />
          <span>Admin</span>
        </span>
      );
    } else if (name.includes('staff')) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-bold bg-blue-500/10 text-blue-600 dark:text-blue-400 border border-blue-500/10">
          <User size={11} />
          <span>Staff</span>
        </span>
      );
    } else if (name.includes('recruiter')) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-bold bg-orange-500/10 text-orange-600 dark:text-orange-400 border border-orange-500/10">
          <Building size={11} />
          <span>Nhà tuyển dụng</span>
        </span>
      );
    } else {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-bold bg-zinc-500/10 text-zinc-600 dark:text-zinc-400 border border-zinc-500/10">
          <User size={11} />
          <span>Ứng viên</span>
        </span>
      );
    }
  };

  const accountsTotalPages = accountsData?.data?.totalPages || 0;
  const accountsTotal = accountsData?.data?.total || 0;

  const auditTotalPages = auditData?.data?.totalPages || 0;
  const auditTotal = auditData?.data?.total || 0;

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-border pb-5">
        <div>
          <h1 className="text-2xl font-black tracking-tight text-foreground flex items-center gap-2">
            <Shield className="text-primary shrink-0" size={28} />
            Quản trị người dùng & An ninh
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Quản lý tài khoản, thay đổi quyền truy cập, khóa tài khoản vi phạm và giám sát nhật ký hoạt động hệ thống.
          </p>
        </div>
      </div>

      {/* Tabs Menu */}
      <div className="flex border-b border-border gap-2">
        <button
          onClick={() => setActiveTab('accounts')}
          className={`px-4 py-2.5 font-bold text-sm border-b-2 flex items-center gap-2 transition-all ${
            activeTab === 'accounts'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Users size={16} />
          Danh sách tài khoản
        </button>
        <button
          onClick={() => setActiveTab('audit')}
          className={`px-4 py-2.5 font-bold text-sm border-b-2 flex items-center gap-2 transition-all ${
            activeTab === 'audit'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <ClipboardList size={16} />
          Nhật ký hoạt động (Audit Trail)
        </button>
      </div>

      {/* Content for Accounts Tab */}
      {activeTab === 'accounts' && (
        <div className="space-y-4">
          {/* Filters Bar */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3 bg-card border border-border p-4 rounded-2xl shadow-xs">
            {/* Search */}
            <div className="relative md:col-span-2">
              <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
              <input
                type="text"
                placeholder="Tìm theo email, tên, công ty..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-border rounded-xl bg-background text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground p-0.5 rounded-full hover:bg-muted"
                >
                  <X size={14} />
                </button>
              )}
            </div>

            {/* Role Filter */}
            <select
              value={selectedRole || ''}
              onChange={(e) => {
                const val = e.target.value;
                setSelectedRole(val ? parseInt(val, 10) : null);
                setAccountsPage(1);
              }}
              className="py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
            >
              <option value="">Tất cả vai trò</option>
              <option value={SystemRole.Admin}>Admin</option>
              <option value={SystemRole.Staff}>Staff</option>
              <option value={SystemRole.Recruiter}>Nhà tuyển dụng</option>
              <option value={SystemRole.Candidate}>Ứng viên</option>
            </select>

            {/* Status Filter */}
            <select
              value={selectedStatus || ''}
              onChange={(e) => {
                setSelectedStatus(e.target.value || null);
                setAccountsPage(1);
              }}
              className="py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="ACTIVE">Đang hoạt động</option>
              <option value="INACTIVE">Tạm ngưng</option>
              <option value="BANNED">Bị khóa</option>
              <option value="PENDING_VERIFICATION">Chờ xác minh</option>
            </select>
          </div>

          {/* Table Container */}
          <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-xs">
            {isAccountsLoading ? (
              <div className="py-20 flex flex-col items-center justify-center text-muted-foreground gap-3">
                <Loader2 className="animate-spin text-primary" size={32} />
                <span className="text-sm font-medium">Đang tải danh sách tài khoản...</span>
              </div>
            ) : isAccountsError ? (
              <div className="py-20 text-center text-rose-500 font-medium">
                Không thể tải dữ liệu tài khoản. Vui lòng thử lại.
              </div>
            ) : !accountsData?.data?.items?.length ? (
              <div className="py-20 text-center text-muted-foreground">
                Không tìm thấy tài khoản nào khớp với bộ lọc.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse text-sm">
                  <thead>
                    <tr className="border-b border-border bg-muted/40 font-bold text-muted-foreground">
                      <th className="px-6 py-4">Họ và tên</th>
                      <th className="px-6 py-4">Email</th>
                      <th className="px-6 py-4">Vai trò</th>
                      <th className="px-6 py-4">Trạng thái</th>
                      <th className="px-6 py-4">Ngày tạo</th>
                      <th className="px-6 py-4 text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/60">
                    {accountsData.data.items.map((user) => (
                      <tr key={user.id} className="hover:bg-muted/10 transition-colors">
                        <td className="px-6 py-4 font-semibold text-foreground">
                          {user.fullName || <span className="text-muted-foreground italic font-normal">Chưa cập nhật</span>}
                        </td>
                        <td className="px-6 py-4 text-muted-foreground font-mono">{user.email}</td>
                        <td className="px-6 py-4">{getRoleBadge(user.roleName)}</td>
                        <td className="px-6 py-4">{getStatusBadge(user.status)}</td>
                        <td className="px-6 py-4 text-muted-foreground">
                          {new Date(user.createdAt).toLocaleDateString('vi-VN', {
                            year: 'numeric',
                            month: 'short',
                            day: 'numeric',
                          })}
                        </td>
                        <td className="px-6 py-4 text-right space-x-2">
                          <Link
                            href={`/admin/accounts/${user.id}`}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-border hover:bg-muted text-foreground font-semibold text-xs rounded-lg transition-colors"
                          >
                            <Eye size={12} />
                            <span>Chi tiết</span>
                          </Link>
                          <button
                            onClick={() => {
                              setStatusTargetUser({ id: user.id, email: user.email, currentStatus: user.status });
                              setNewStatus(user.status);
                              setStatusReason('');
                              setStatusError('');
                              setIsStatusModalOpen(true);
                            }}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-border hover:bg-muted text-foreground font-semibold text-xs rounded-lg transition-colors"
                          >
                            <Edit2 size={12} />
                            <span>Trạng thái</span>
                          </button>
                          <button
                            onClick={() => {
                              setRoleTargetUser({ id: user.id, email: user.email, currentRole: user.roleId });
                              setNewRole(user.roleId || SystemRole.Candidate);
                              setRoleError('');
                              setIsRoleModalOpen(true);
                            }}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-purple-500/25 hover:bg-purple-500/5 text-purple-600 dark:text-purple-400 font-semibold text-xs rounded-lg transition-colors"
                          >
                            <Shield size={12} />
                            <span>Quyền</span>
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Accounts Pagination */}
          {accountsData?.data && accountsTotalPages > 1 && (
            <div className="flex items-center justify-between border border-border bg-card p-4 rounded-2xl shadow-xs">
              <span className="text-sm text-muted-foreground">
                Hiển thị trang <strong className="text-foreground">{accountsPage}</strong> trên tổng số{' '}
                <strong className="text-foreground">{accountsTotalPages}</strong> trang ({accountsTotal} kết quả)
              </span>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setAccountsPage((p) => Math.max(p - 1, 1))}
                  disabled={accountsPage === 1}
                  className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors"
                >
                  <ChevronLeft size={16} />
                </button>
                {Array.from({ length: accountsTotalPages }).map((_, i) => {
                  const pg = i + 1;
                  return (
                    <button
                      key={pg}
                      onClick={() => setAccountsPage(pg)}
                      className={`w-9 h-9 rounded-xl text-sm font-bold transition-all ${
                        accountsPage === pg
                          ? 'bg-primary text-primary-foreground'
                          : 'border border-border hover:bg-muted text-foreground'
                      }`}
                    >
                      {pg}
                    </button>
                  );
                })}
                <button
                  onClick={() => setAccountsPage((p) => Math.min(p + 1, accountsTotalPages))}
                  disabled={accountsPage === accountsTotalPages}
                  className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors"
                >
                  <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Content for Audit Trail Tab */}
      {activeTab === 'audit' && (
        <div className="space-y-4">
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
                Không tìm thấy nhật ký hoạt động nào.
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
      )}

      {/* UPDATE STATUS DIALOG */}
      {isStatusModalOpen && statusTargetUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-base font-bold text-foreground">Cập nhật trạng thái hoạt động</h3>
              <button
                onClick={() => {
                  setIsStatusModalOpen(false);
                  setStatusTargetUser(null);
                }}
                className="text-muted-foreground hover:text-foreground p-1 rounded-lg hover:bg-muted"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleStatusSubmit} className="space-y-4">
              <div className="space-y-1">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Tài khoản</label>
                <p className="text-sm font-mono font-semibold text-foreground bg-muted p-2 rounded-lg border border-border">
                  {statusTargetUser.email}
                </p>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Trạng thái mới</label>
                <select
                  value={newStatus}
                  onChange={(e) => setNewStatus(e.target.value as UserStatus)}
                  className="w-full py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
                >
                  <option value="ACTIVE">Đang hoạt động (ACTIVE)</option>
                  <option value="INACTIVE">Tạm ngưng hoạt động (INACTIVE)</option>
                  <option value="BANNED">Khóa tài khoản (BANNED)</option>
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">
                  Lý do thay đổi <span className="text-rose-500">*</span>
                </label>
                <textarea
                  placeholder="Nhập lý do chi tiết để lưu trữ vào Audit Log (tối thiểu 5 ký tự)..."
                  value={statusReason}
                  onChange={(e) => setStatusReason(e.target.value)}
                  rows={3}
                  className="w-full p-3 border border-border rounded-xl bg-background text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground resize-none"
                />
                <span className="text-xs text-muted-foreground">
                  Lý do này sẽ ghi vào lịch sử và không thể chỉnh sửa.
                </span>
              </div>

              {statusError && (
                <div className="p-3 bg-rose-500/10 border border-rose-500/25 rounded-xl text-rose-500 text-xs font-semibold flex items-center gap-1.5">
                  <XCircle size={14} className="shrink-0" />
                  <span>{statusError}</span>
                </div>
              )}

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => {
                    setIsStatusModalOpen(false);
                    setStatusTargetUser(null);
                  }}
                  className="px-4 py-2 border border-border hover:bg-muted text-foreground font-semibold text-sm rounded-xl transition-colors"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={updateStatusMutation.isPending}
                  className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
                >
                  {updateStatusMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                  <span>Xác nhận</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* UPDATE ROLE DIALOG */}
      {isRoleModalOpen && roleTargetUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-base font-bold text-foreground">Phân quyền tài khoản (Chỉ Admin)</h3>
              <button
                onClick={() => {
                  setIsRoleModalOpen(false);
                  roleTargetUser && setRoleTargetUser(null);
                }}
                className="text-muted-foreground hover:text-foreground p-1 rounded-lg hover:bg-muted"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleRoleSubmit} className="space-y-4">
              <div className="space-y-1">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Tài khoản</label>
                <p className="text-sm font-mono font-semibold text-foreground bg-muted p-2 rounded-lg border border-border">
                  {roleTargetUser.email}
                </p>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Vai trò hệ thống mới</label>
                <select
                  value={newRole}
                  onChange={(e) => setNewRole(parseInt(e.target.value, 10))}
                  className="w-full py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
                >
                  <option value={SystemRole.Admin}>Quản trị viên (Admin)</option>
                  <option value={SystemRole.Staff}>Nhân viên vận hành (Staff)</option>
                  <option value={SystemRole.Recruiter}>Nhà tuyển dụng (Recruiter)</option>
                  <option value={SystemRole.Candidate}>Ứng viên (Candidate)</option>
                </select>
                <span className="text-xs text-rose-500 block font-semibold mt-1">
                  ⚠️ Cảnh báo: Việc thay đổi vai trò sẽ lập tức thay đổi quyền truy cập của người dùng trên toàn bộ hệ thống.
                </span>
              </div>

              {roleError && (
                <div className="p-3 bg-rose-500/10 border border-rose-500/25 rounded-xl text-rose-500 text-xs font-semibold flex items-center gap-1.5">
                  <XCircle size={14} className="shrink-0" />
                  <span>{roleError}</span>
                </div>
              )}

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => {
                    setIsRoleModalOpen(false);
                    setRoleTargetUser(null);
                  }}
                  className="px-4 py-2 border border-border hover:bg-muted text-foreground font-semibold text-sm rounded-xl transition-colors"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={updateRoleMutation.isPending}
                  className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
                >
                  {updateRoleMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                  <span>Xác nhận</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* TOAST SYSTEM */}
      {toast && (
        <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
          <div className={`flex items-center gap-3 px-4 py-3 rounded-2xl shadow-lg border text-sm font-semibold ${
            toast.type === 'success' ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/25' :
            toast.type === 'warning' ? 'bg-amber-500/10 text-amber-500 border-amber-500/25' :
            'bg-destructive/10 text-destructive border-destructive/25'
          }`}>
            <CheckCircle size={18} className="shrink-0" />
            <span>{toast.message}</span>
            <button
              onClick={() => setToast(null)}
              className="text-muted-foreground hover:text-foreground shrink-0 p-0.5 rounded-lg hover:bg-black/5"
            >
              <X size={14} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
