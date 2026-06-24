'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import {
  Search,
  User,
  Shield,
  Clock,
  Ban,
  CheckCircle,
  Eye,
  Edit2,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Users,
  AlertTriangle,
  X,
  Building,
  UserPlus
} from 'lucide-react';
import { useUsers } from '@/hooks/useUserGovernance';
import { UserStatus, SystemRole } from '@/types/user-governance.types';
import { CreateStaffModal } from './components/create-staff-modal';
import { UpdateStatusModal } from './components/update-status-modal';

export default function AdminAccountsPage() {
  // Accounts Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRole, setSelectedRole] = useState<number | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<string | null>(null);
  const [accountsPage, setAccountsPage] = useState(1);
  const accountsPageSize = 10;

  // Modals State (Only what needs to be controlled by parent)
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  const [statusTargetUser, setStatusTargetUser] = useState<{ id: string; email: string; currentStatus: UserStatus } | null>(null);

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

  // Fetch Accounts
  const {
    data: accountsData,
    isLoading: isAccountsLoading,
    isError: isAccountsError,
  } = useUsers({
    page: accountsPage,
    pageSize: accountsPageSize,
    search: debouncedSearch || undefined,
    roleId: selectedRole || undefined,
    status: selectedStatus || undefined,
  });

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

  const accountsTotalPages = accountsData?.data?.totalPages || 0;
  const accountsTotal = accountsData?.data?.total || 0;

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-border pb-5">
        <div>
          <h1 className="text-2xl font-black tracking-tight text-foreground flex items-center gap-2">
            <Users className="text-primary shrink-0" size={28} />
            User Governance
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage user accounts, review access status, and suspend policy-violating users.
          </p>
        </div>
        <div>
          <CreateStaffModal onSuccess={(msg) => showToast(msg, 'success')}>
            <button
              className="inline-flex items-center gap-2 px-4 py-2.5 bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm rounded-xl shadow-xs transition-colors"
            >
              <UserPlus size={16} />
              <span>Create Staff Account</span>
            </button>
          </CreateStaffModal>
        </div>
      </div>

      <div className="space-y-4">
        {/* Filters Bar */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3 bg-card border border-border p-4 rounded-2xl shadow-xs">
          {/* Search */}
          <div className="relative md:col-span-2">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
            <input
              type="text"
              placeholder="Search by email, name, company..."
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
            <option value="">All Roles</option>
            <option value={SystemRole.Admin}>Admin</option>
            <option value={SystemRole.Staff}>Staff</option>
            <option value={SystemRole.Recruiter}>Recruiter</option>
            <option value={SystemRole.Candidate}>Candidate</option>
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
            <option value="">All Statuses</option>
            <option value="ACTIVE">Active</option>
            <option value="INACTIVE">Inactive</option>
            <option value="BANNED">Banned</option>
            <option value="PENDING_VERIFICATION">Pending Verification</option>
          </select>
        </div>

        {/* Table Container */}
        <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-xs">
          {isAccountsLoading ? (
            <div className="py-20 flex flex-col items-center justify-center text-muted-foreground gap-3">
              <Loader2 className="animate-spin text-primary" size={32} />
              <span className="text-sm font-medium">Loading user accounts...</span>
            </div>
          ) : isAccountsError ? (
            <div className="py-20 text-center text-rose-500 font-medium">
              Failed to load user accounts. Please try again.
            </div>
          ) : !accountsData?.data?.items?.length ? (
            <div className="py-20 text-center text-muted-foreground">
              No accounts found matching the filters.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-sm">
                <thead>
                  <tr className="border-b border-border bg-muted/40 font-bold text-muted-foreground">
                    <th className="px-6 py-4">Full Name</th>
                    <th className="px-6 py-4">Email</th>
                    <th className="px-6 py-4">Role</th>
                    <th className="px-6 py-4">Status</th>
                    <th className="px-6 py-4">Created Date</th>
                    <th className="px-6 py-4 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/60">
                  {accountsData.data.items.map((user) => (
                    <tr key={user.id} className="hover:bg-muted/10 transition-colors">
                      <td className="px-6 py-4 font-semibold text-foreground">
                        {user.fullName || (user.roleName?.toLowerCase() === 'staff' ? user.email : <span className="text-muted-foreground italic font-normal">Not updated</span>)}
                      </td>
                      <td className="px-6 py-4 text-muted-foreground font-mono">{user.email}</td>
                      <td className="px-6 py-4">{getRoleBadge(user.roleName)}</td>
                      <td className="px-6 py-4">{getStatusBadge(user.status)}</td>
                      <td className="px-6 py-4 text-muted-foreground">
                        {new Date(user.createdAt).toLocaleDateString('en-US', {
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
                          <span>Details</span>
                        </Link>
                        {user.roleName?.toLowerCase() !== 'admin' && (
                          <button
                            onClick={() => {
                              setStatusTargetUser({ id: user.id, email: user.email, currentStatus: user.status });
                              setIsStatusModalOpen(true);
                            }}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-border hover:bg-muted text-foreground font-semibold text-xs rounded-lg transition-colors"
                          >
                            <Edit2 size={12} />
                            <span>Status</span>
                          </button>
                        )}
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
              Showing page <strong className="text-foreground">{accountsPage}</strong> of{' '}
              <strong className="text-foreground">{accountsTotalPages}</strong> pages ({accountsTotal} results)
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

      {/* UPDATE STATUS DIALOG */}
      <UpdateStatusModal 
        open={isStatusModalOpen} 
        onOpenChange={setIsStatusModalOpen} 
        targetUser={statusTargetUser} 
        onSuccess={(msg) => showToast(msg, 'success')} 
      />

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
