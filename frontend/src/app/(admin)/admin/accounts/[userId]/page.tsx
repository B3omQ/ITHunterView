'use client';

import React, { useState, useEffect, use } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  ArrowLeft,
  User,
  Shield,
  Clock,
  Ban,
  CheckCircle,
  XCircle,
  Edit2,
  Loader2,
  Mail,
  Phone,
  MapPin,
  Globe,
  Building,
  AlertTriangle,
  X
} from 'lucide-react';
import { useUserDetail, useUpdateUserStatus, useUpdateUserRole } from '@/hooks/useUserGovernance';
import { UserStatus, SystemRole } from '@/types/user-governance.types';

// Custom inline SVG icons for social links since they are not exported by this version of lucide-react
const Github = ({ size = 24, className, ...props }: React.SVGProps<SVGSVGElement> & { size?: number }) => (
  <svg
    viewBox="0 0 24 24"
    width={size}
    height={size}
    stroke="currentColor"
    strokeWidth="2"
    fill="none"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
    {...props}
  >
    <path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4" />
    <path d="M9 18c-4.51 2-5-2-7-2" />
  </svg>
);

const Linkedin = ({ size = 24, className, ...props }: React.SVGProps<SVGSVGElement> & { size?: number }) => (
  <svg
    viewBox="0 0 24 24"
    width={size}
    height={size}
    stroke="currentColor"
    strokeWidth="2"
    fill="none"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
    {...props}
  >
    <path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z" />
    <rect width="4" height="12" x="2" y="9" />
    <circle cx="4" cy="4" r="2" />
  </svg>
);

interface PageProps {
  params: Promise<{ userId: string }>;
}

export default function AdminAccountDetailPage({ params }: PageProps) {
  const router = useRouter();
  const resolvedParams = use(params);
  const userId = resolvedParams.userId;

  const { data: detailData, isLoading, isError } = useUserDetail(userId);

  // Modals / Status Action State
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  const [newStatus, setNewStatus] = useState<UserStatus>('ACTIVE');
  const [statusReason, setStatusReason] = useState('');
  const [statusError, setStatusError] = useState('');

  // Modals / Role Action State
  const [isRoleModalOpen, setIsRoleModalOpen] = useState(false);
  const [newRole, setNewRole] = useState<number>(SystemRole.Candidate);
  const [roleError, setRoleError] = useState('');

  // Toast notifications state
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

  const showToast = (message: string, type: 'success' | 'error' = 'success') => {
    setToast({ message, type });
  };

  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  // Set default values when detailData is loaded
  useEffect(() => {
    if (detailData?.data) {
      setNewStatus(detailData.data.status);
      setNewRole(detailData.data.roleId || SystemRole.Candidate);
    }
  }, [detailData]);

  // Mutations
  const updateStatusMutation = useUpdateUserStatus();
  const updateRoleMutation = useUpdateUserRole();

  // Status Change Submit
  const handleStatusSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!statusReason.trim()) {
      setStatusError('Vui lòng nhập lý do cập nhật trạng thái.');
      return;
    }
    if (statusReason.trim().length < 5) {
      setStatusError('Lý do phải có nhất 5 ký tự.');
      return;
    }

    setStatusError('');
    updateStatusMutation.mutate(
      {
        id: userId,
        dto: {
          status: newStatus,
          reason: statusReason.trim(),
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast('Cập nhật trạng thái thành công!', 'success');
            setIsStatusModalOpen(false);
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

    setRoleError('');
    updateRoleMutation.mutate(
      {
        id: userId,
        dto: {
          roleId: newRole,
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast('Cập nhật vai trò thành công!', 'success');
            setIsRoleModalOpen(false);
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

  if (isLoading) {
    return (
      <div className="min-h-[400px] flex flex-col items-center justify-center text-muted-foreground gap-3">
        <Loader2 className="animate-spin text-primary" size={40} />
        <span className="text-sm font-semibold">Đang tải hồ sơ chi tiết người dùng...</span>
      </div>
    );
  }

  if (isError || !detailData?.success || !detailData.data) {
    return (
      <div className="p-6 max-w-4xl mx-auto space-y-6">
        <Link
          href="/admin/accounts"
          className="inline-flex items-center gap-2 text-sm font-bold text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft size={16} />
          <span>Quay lại danh sách</span>
        </Link>
        <div className="bg-rose-500/10 border border-rose-500/25 p-6 rounded-2xl text-rose-500 text-center font-bold">
          Không tìm thấy thông tin chi tiết của tài khoản người dùng này.
        </div>
      </div>
    );
  }

  const user = detailData.data;

  return (
    <div className="p-6 max-w-4xl mx-auto space-y-6">
      {/* Back Link */}
      <Link
        href="/admin/accounts"
        className="inline-flex items-center gap-2 text-sm font-bold text-muted-foreground hover:text-foreground transition-colors border border-border bg-card px-3.5 py-2 rounded-xl"
      >
        <ArrowLeft size={16} />
        <span>Quay lại danh sách</span>
      </Link>

      {/* Main Grid Layout */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Sidebar Status & Quick actions */}
        <div className="space-y-6">
          <div className="bg-card border border-border p-6 rounded-2xl shadow-xs space-y-5">
            <h2 className="text-sm font-black text-foreground border-b border-border pb-3 uppercase tracking-wider">
              Tài khoản hệ thống
            </h2>
            <div className="space-y-3">
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Email</span>
                <span className="text-sm font-mono font-semibold text-foreground break-all">{user.email}</span>
              </div>
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Vai trò</span>
                <div>{getRoleBadge(user.roleName)}</div>
              </div>
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Trạng thái</span>
                <div>{getStatusBadge(user.status)}</div>
              </div>
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Ngày tham gia</span>
                <span className="text-sm font-semibold text-foreground">
                  {new Date(user.createdAt).toLocaleDateString('vi-VN', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </span>
              </div>
              {user.deactiveAt && (
                <div className="space-y-1">
                  <span className="text-xs font-bold text-rose-500 uppercase tracking-wider block">Ngày ngừng hoạt động</span>
                  <span className="text-sm font-semibold text-rose-500">
                    {new Date(user.deactiveAt).toLocaleDateString('vi-VN', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                    })}
                  </span>
                </div>
              )}
            </div>

            {/* Quick Actions */}
            <div className="pt-4 border-t border-border space-y-2">
              <button
                onClick={() => {
                  setNewStatus(user.status);
                  setStatusReason('');
                  setStatusError('');
                  setIsStatusModalOpen(true);
                }}
                className="w-full inline-flex items-center justify-center gap-2 px-4 py-2.5 border border-border hover:bg-muted text-foreground font-semibold text-sm rounded-xl transition-colors"
              >
                <Edit2 size={16} />
                <span>Thay đổi trạng thái</span>
              </button>
              <button
                onClick={() => {
                  setNewRole(user.roleId || SystemRole.Candidate);
                  setRoleError('');
                  setIsRoleModalOpen(true);
                }}
                className="w-full inline-flex items-center justify-center gap-2 px-4 py-2.5 border border-purple-500/20 hover:bg-purple-500/5 text-purple-600 dark:text-purple-400 font-semibold text-sm rounded-xl transition-colors"
              >
                <Shield size={16} />
                <span>Phân quyền tài khoản</span>
              </button>
            </div>
          </div>
        </div>

        {/* Profile Details (Candidate or Recruiter) */}
        <div className="md:col-span-2 space-y-6">
          {/* Candidate Profile Details */}
          {user.candidateProfile && (
            <div className="bg-card border border-border p-6 rounded-2xl shadow-xs space-y-6">
              <div className="flex items-start gap-4 border-b border-border pb-5">
                <div className="w-16 h-16 rounded-2xl bg-muted border border-border flex items-center justify-center overflow-hidden shrink-0">
                  {user.candidateProfile.avatarUrl ? (
                    <img
                      src={user.candidateProfile.avatarUrl}
                      alt={`${user.candidateProfile.firstName} ${user.candidateProfile.lastName}`}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <User size={32} className="text-muted-foreground" />
                  )}
                </div>
                <div>
                  <h3 className="text-xl font-black text-foreground">
                    {user.candidateProfile.firstName} {user.candidateProfile.lastName}
                  </h3>
                  <span className="inline-flex px-2 py-0.5 rounded bg-blue-500/10 text-blue-500 font-semibold text-xs mt-1">
                    Hồ sơ ứng viên
                  </span>
                </div>
              </div>

              {/* Bio & Details */}
              <div className="space-y-4">
                <div className="space-y-1">
                  <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Giới thiệu</span>
                  <p className="text-sm text-foreground bg-muted/40 border border-border/50 p-3 rounded-xl leading-relaxed whitespace-pre-wrap">
                    {user.candidateProfile.aboutMe || <span className="text-muted-foreground italic">Chưa điền thông tin giới thiệu.</span>}
                  </p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Điện thoại</span>
                    <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                      <Phone size={14} className="text-muted-foreground" />
                      {user.candidateProfile.phone || <span className="text-muted-foreground italic font-normal">Chưa cập nhật</span>}
                    </span>
                  </div>
                  <div className="space-y-1">
                    <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Địa điểm</span>
                    <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                      <MapPin size={14} className="text-muted-foreground" />
                      {user.candidateProfile.location || <span className="text-muted-foreground italic font-normal">Chưa cập nhật</span>}
                    </span>
                  </div>
                </div>

                {/* Social Links */}
                <div className="space-y-2 pt-2">
                  <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Liên kết mạng xã hội</span>
                  <div className="flex flex-wrap gap-2">
                    {user.candidateProfile.githubUrl && (
                      <a
                        href={user.candidateProfile.githubUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-border hover:bg-muted text-foreground font-semibold text-xs rounded-lg transition-colors"
                      >
                        <Github size={14} />
                        <span>GitHub</span>
                      </a>
                    )}
                    {user.candidateProfile.linkedInUrl && (
                      <a
                        href={user.candidateProfile.linkedInUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-border hover:bg-muted text-foreground font-semibold text-xs rounded-lg transition-colors"
                      >
                        <Linkedin size={14} className="text-blue-500" />
                        <span>LinkedIn</span>
                      </a>
                    )}
                    {user.candidateProfile.portfolioUrl && (
                      <a
                        href={user.candidateProfile.portfolioUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-border hover:bg-muted text-foreground font-semibold text-xs rounded-lg transition-colors"
                      >
                        <Globe size={14} className="text-primary" />
                        <span>Portfolio</span>
                      </a>
                    )}
                    {!user.candidateProfile.githubUrl && !user.candidateProfile.linkedInUrl && !user.candidateProfile.portfolioUrl && (
                      <span className="text-sm text-muted-foreground italic">Không có liên kết xã hội.</span>
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Recruiter Profile Details */}
          {user.recruiterProfile && (
            <div className="bg-card border border-border p-6 rounded-2xl shadow-xs space-y-6">
              <div className="flex items-start gap-4 border-b border-border pb-5">
                <div className="w-16 h-16 rounded-2xl bg-muted border border-border flex items-center justify-center overflow-hidden shrink-0">
                  {user.recruiterProfile.avatarUrl ? (
                    <img
                      src={user.recruiterProfile.avatarUrl}
                      alt={user.recruiterProfile.fullName}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <User size={32} className="text-muted-foreground" />
                  )}
                </div>
                <div>
                  <h3 className="text-xl font-black text-foreground">{user.recruiterProfile.fullName}</h3>
                  <p className="text-sm text-muted-foreground font-medium mt-0.5">{user.recruiterProfile.positionTitle}</p>
                  <span className="inline-flex px-2 py-0.5 rounded bg-orange-500/10 text-orange-500 font-semibold text-xs mt-2">
                    Nhà tuyển dụng chuyên nghiệp
                  </span>
                </div>
              </div>

              {/* Personal Info */}
              <div className="space-y-4">
                <div className="space-y-1">
                  <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Điện thoại liên hệ</span>
                  <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                    <Phone size={14} className="text-muted-foreground" />
                    {user.recruiterProfile.phone || <span className="text-muted-foreground italic font-normal">Chưa cập nhật</span>}
                  </span>
                </div>
              </div>

              {/* Company Info */}
              {user.recruiterProfile.company ? (
                <div className="border-t border-border pt-5 space-y-4">
                  <h4 className="text-sm font-bold text-foreground flex items-center gap-1.5 uppercase tracking-wider">
                    <Building size={16} className="text-primary" />
                    Công ty trực thuộc
                  </h4>
                  <div className="flex items-start gap-3 bg-muted/20 border border-border/60 p-4 rounded-xl">
                    <div className="w-12 h-12 bg-card border border-border rounded-xl flex items-center justify-center overflow-hidden shrink-0">
                      {user.recruiterProfile.company.logoUrl ? (
                        <img
                          src={user.recruiterProfile.company.logoUrl}
                          alt={user.recruiterProfile.company.name}
                          className="w-full h-full object-contain p-1"
                        />
                      ) : (
                        <Building size={20} className="text-muted-foreground" />
                      )}
                    </div>
                    <div className="space-y-1">
                      <h5 className="text-sm font-bold text-foreground">{user.recruiterProfile.company.name}</h5>
                      <span className="inline-flex px-1.5 py-0.2 rounded text-[10px] font-bold bg-zinc-200 dark:bg-zinc-800 text-foreground">
                        {user.recruiterProfile.company.industry}
                      </span>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="space-y-1">
                      <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Trụ sở chính</span>
                      <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                        <MapPin size={14} className="text-muted-foreground shrink-0" />
                        <span className="leading-relaxed">{user.recruiterProfile.company.headquartersAddress}</span>
                      </span>
                    </div>
                    <div className="space-y-1">
                      <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Trang web</span>
                      <a
                        href={user.recruiterProfile.company.website}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-sm font-semibold text-primary hover:underline flex items-center gap-1.5 break-all"
                      >
                        <Globe size={14} className="shrink-0" />
                        <span>{user.recruiterProfile.company.website}</span>
                      </a>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="border-t border-border pt-5 text-center text-muted-foreground text-sm italic">
                  Nhà tuyển dụng này chưa liên kết với công ty nào.
                </div>
              )}
            </div>
          )}

          {/* Admin / Staff (no profile) */}
          {!user.candidateProfile && !user.recruiterProfile && (
            <div className="bg-card border border-border p-8 rounded-2xl text-center text-muted-foreground shadow-xs">
              <User size={48} className="mx-auto text-muted-foreground/50 mb-3" />
              <p className="font-semibold text-sm text-foreground">Không có hồ sơ cá nhân</p>
              <p className="text-xs text-muted-foreground mt-1 max-w-md mx-auto">
                Tài khoản này chưa thiết lập hồ sơ Ứng viên hoặc Nhà tuyển dụng trên hệ thống. Đây có thể là tài khoản của Admin hoặc Staff.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* UPDATE STATUS DIALOG */}
      {isStatusModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-base font-bold text-foreground">Cập nhật trạng thái hoạt động</h3>
              <button
                onClick={() => setIsStatusModalOpen(false)}
                className="text-muted-foreground hover:text-foreground p-1 rounded-lg hover:bg-muted"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleStatusSubmit} className="space-y-4">
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
                  onClick={() => setIsStatusModalOpen(false)}
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
      {isRoleModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-base font-bold text-foreground">Phân quyền tài khoản (Chỉ Admin)</h3>
              <button
                onClick={() => setIsRoleModalOpen(false)}
                className="text-muted-foreground hover:text-foreground p-1 rounded-lg hover:bg-muted"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleRoleSubmit} className="space-y-4">
              <div className="space-y-1.5">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Vai trò mới</label>
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
                  ⚠️ Cảnh báo: Thay đổi vai trò sẽ lập tức ảnh hưởng đến quyền truy cập hệ thống của tài khoản này.
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
                  onClick={() => setIsRoleModalOpen(false)}
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
