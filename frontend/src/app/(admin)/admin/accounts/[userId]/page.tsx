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
import { useUserDetail, useUpdateUserStatus } from '@/hooks/useUserGovernance';
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

  useEffect(() => {
    if (detailData?.data) {
      setNewStatus(detailData.data.status);
    }
  }, [detailData]);

  // Mutations
  const updateStatusMutation = useUpdateUserStatus();

  // Status Change Submit
  const handleStatusSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!statusReason.trim()) {
      setStatusError('Please enter the reason for updating the status.');
      return;
    }
    if (statusReason.trim().length < 5) {
      setStatusError('Reason must be at least 5 characters.');
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
            showToast('Status updated successfully!', 'success');
            setIsStatusModalOpen(false);
            setStatusReason('');
          } else {
            setStatusError(res.message || 'Failed to update status.');
          }
        },
        onError: (err: any) => {
          setStatusError(err.response?.data?.message || 'An error occurred while updating.');
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
            <span>Active</span>
          </span>
        );
      case 'INACTIVE':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-500/10 text-zinc-500 border border-zinc-500/20">
            <Clock size={12} />
            <span>Inactive</span>
          </span>
        );
      case 'BANNED':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-500/10 text-rose-500 border border-rose-500/20">
            <Ban size={12} />
            <span>Banned</span>
          </span>
        );
      case 'PENDING_VERIFICATION':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-500 border border-amber-500/20">
            <AlertTriangle size={12} />
            <span>Pending Verification</span>
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
          <span>Recruiter</span>
        </span>
      );
    } else {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-bold bg-zinc-500/10 text-zinc-600 dark:text-zinc-400 border border-zinc-500/10">
          <User size={11} />
          <span>Candidate</span>
        </span>
      );
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-[400px] flex flex-col items-center justify-center text-muted-foreground gap-3">
        <Loader2 className="animate-spin text-primary" size={40} />
        <span className="text-sm font-semibold">Loading user profile details...</span>
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
          <span>Back to list</span>
        </Link>
        <div className="bg-rose-500/10 border border-rose-500/25 p-6 rounded-2xl text-rose-500 text-center font-bold">
          Detailed profile info for this user account could not be found.
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
        <span>Back to list</span>
      </Link>

      {/* Main Grid Layout */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Sidebar Status & Quick actions */}
        <div className="space-y-6">
          <div className="bg-card border border-border p-6 rounded-2xl shadow-xs space-y-5">
            <h2 className="text-sm font-black text-foreground border-b border-border pb-3 uppercase tracking-wider">
              System Account
            </h2>
            <div className="space-y-3">
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Email</span>
                <span className="text-sm font-mono font-semibold text-foreground break-all">{user.email}</span>
              </div>
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Role</span>
                <div>{getRoleBadge(user.roleName)}</div>
              </div>
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Status</span>
                <div>{getStatusBadge(user.status)}</div>
              </div>
              <div className="space-y-1">
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Joined Date</span>
                <span className="text-sm font-semibold text-foreground">
                  {new Date(user.createdAt).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </span>
              </div>
              {user.deactiveAt && (
                <div className="space-y-1">
                  <span className="text-xs font-bold text-rose-500 uppercase tracking-wider block">Deactivated Date</span>
                  <span className="text-sm font-semibold text-rose-500">
                    {new Date(user.deactiveAt).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                    })}
                  </span>
                </div>
              )}
            </div>

            {/* Quick Actions */}
            {!(user.roleName?.toLowerCase().includes('admin') || user.roleId === SystemRole.Admin) && (
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
                  <span>Change Status</span>
                </button>
              </div>
            )}
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
                    Candidate Profile
                  </span>
                </div>
              </div>

              {/* Bio & Details */}
              <div className="space-y-4">
                <div className="space-y-1">
                  <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">About Me</span>
                  <p className="text-sm text-foreground bg-muted/40 border border-border/50 p-3 rounded-xl leading-relaxed whitespace-pre-wrap">
                    {user.candidateProfile.aboutMe || <span className="text-muted-foreground italic">No introduction provided.</span>}
                  </p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Phone</span>
                    <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                      <Phone size={14} className="text-muted-foreground" />
                      {user.candidateProfile.phone || <span className="text-muted-foreground italic font-normal">Not updated</span>}
                    </span>
                  </div>
                  <div className="space-y-1">
                    <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Location</span>
                    <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                      <MapPin size={14} className="text-muted-foreground" />
                      {user.candidateProfile.location || <span className="text-muted-foreground italic font-normal">Not updated</span>}
                    </span>
                  </div>
                </div>

                {/* Social Links */}
                <div className="space-y-2 pt-2">
                  <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Social Links</span>
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
                      <span className="text-sm text-muted-foreground italic">No social links.</span>
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
                    Professional Recruiter
                  </span>
                </div>
              </div>

              {/* Personal Info */}
              <div className="space-y-4">
                <div className="space-y-1">
                  <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Contact Phone</span>
                  <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                    <Phone size={14} className="text-muted-foreground" />
                    {user.recruiterProfile.phone || <span className="text-muted-foreground italic font-normal">Not updated</span>}
                  </span>
                </div>
              </div>

              {/* Company Info */}
              {user.recruiterProfile.company ? (
                <div className="border-t border-border pt-5 space-y-4">
                  <h4 className="text-sm font-bold text-foreground flex items-center gap-1.5 uppercase tracking-wider">
                    <Building size={16} className="text-primary" />
                    Affiliated Company
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
                      <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Headquarters</span>
                      <span className="text-sm font-semibold text-foreground flex items-center gap-1.5">
                        <MapPin size={14} className="text-muted-foreground shrink-0" />
                        <span className="leading-relaxed">{user.recruiterProfile.company.headquartersAddress}</span>
                      </span>
                    </div>
                    <div className="space-y-1">
                      <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Website</span>
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
                  This recruiter is not yet affiliated with any company.
                </div>
              )}
            </div>
          )}

          {/* Admin / Staff Profile Details */}
          {!user.candidateProfile && !user.recruiterProfile && (
            <div className="bg-card border border-border p-6 rounded-2xl shadow-xs space-y-6">
              <div className="flex items-start gap-4 border-b border-border pb-5">
                <div className="w-16 h-16 rounded-2xl bg-muted border border-border flex items-center justify-center overflow-hidden shrink-0">
                  <User size={32} className="text-muted-foreground" />
                </div>
                <div>
                  <h3 className="text-xl font-black text-foreground">
                    {user.roleName?.toLowerCase() === 'staff' ? user.email : 'System Admin'}
                  </h3>
                  <span className={`inline-flex px-2 py-0.5 rounded font-semibold text-xs mt-1 capitalize ${
                    user.roleName?.toLowerCase() === 'admin' 
                      ? 'bg-purple-500/10 text-purple-500 border border-purple-500/20' 
                      : 'bg-blue-500/10 text-blue-500 border border-blue-500/20'
                  }`}>
                    {user.roleName} Profile
                  </span>
                </div>
              </div>

              {/* Description */}
              <div className="space-y-4">
                <div className="space-y-1">
                  <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider block">Description</span>
                  <p className="text-sm text-foreground bg-muted/40 border border-border/50 p-4 rounded-xl leading-relaxed">
                    {user.roleName?.toLowerCase() === 'staff' 
                      ? 'This is an internal system Staff account. Staff members can monitor candidate and recruiter activities, inspect applications, review job postings, and handle standard support operations.'
                      : 'This is the master Administrator account. The administrator has full permissions over system configurations, user governance, audit log tracking, and security parameters.'
                    }
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* UPDATE STATUS DIALOG */}
      {isStatusModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-base font-bold text-foreground">Update Account Status</h3>
              <button
                onClick={() => setIsStatusModalOpen(false)}
                className="text-muted-foreground hover:text-foreground p-1 rounded-lg hover:bg-muted"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleStatusSubmit} className="space-y-4">
              <div className="space-y-1.5">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">New Status</label>
                <select
                  value={newStatus}
                  onChange={(e) => setNewStatus(e.target.value as UserStatus)}
                  className="w-full py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
                >
                  <option value="ACTIVE">Active (ACTIVE)</option>
                  <option value="INACTIVE">Inactive (INACTIVE)</option>
                  <option value="BANNED">Banned (BANNED)</option>
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">
                  Reason for Change <span className="text-rose-500">*</span>
                </label>
                <textarea
                  placeholder="Enter detailed reason for the audit log (minimum 5 characters)..."
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
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={updateStatusMutation.isPending}
                  className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
                >
                  {updateStatusMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                  <span>Confirm</span>
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
