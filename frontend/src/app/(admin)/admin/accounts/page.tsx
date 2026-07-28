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
  Users,
  AlertTriangle,
  X,
  Building,
  UserPlus,
  XCircle,
  RotateCcw,
  SearchX,
} from 'lucide-react';
import { useUsers } from '@/hooks/useUserGovernance';
import { UserStatus, SystemRole } from '@/types/user-governance.types';
import { CreateStaffModal } from './components/create-staff-modal';
import { UpdateStatusModal } from './components/update-status-modal';
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
import { format } from 'date-fns';

export default function AdminAccountsPage() {
  // Accounts Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedRole, setSelectedRole] = useState<number | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<string | null>(null);
  const [accountsPage, setAccountsPage] = useState(1);
  const [accountsPageSize, setAccountsPageSize] = useState(10);

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
    }, 350);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Fetch Accounts complying with kinh-mantra.md (page -> hook -> service -> api-client -> backend)
  const {
    data: accountsData,
    isLoading: isAccountsLoading,
    isError: isAccountsError,
    refetch,
    isFetching,
  } = useUsers({
    page: accountsPage,
    pageSize: accountsPageSize,
    search: debouncedSearch || undefined,
    roleId: selectedRole || undefined,
    status: selectedStatus || undefined,
  });

  const handleResetFilters = () => {
    setSearchQuery('');
    setSelectedRole(null);
    setSelectedStatus(null);
    setAccountsPage(1);
  };

  const isFilterActive =
    searchQuery !== '' || selectedRole !== null || selectedStatus !== null;

  const getStatusBadge = (status: UserStatus) => {
    switch (status) {
      case 'ACTIVE':
        return (
          <Badge className="bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
            <CheckCircle size={11} />
            <span>Active</span>
          </Badge>
        );
      case 'INACTIVE':
        return (
          <Badge className="bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700 rounded-full px-2.5 py-0.5 text-xs font-medium inline-flex items-center gap-1">
            <Clock size={11} />
            <span>Inactive</span>
          </Badge>
        );
      case 'BANNED':
        return (
          <Badge className="bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
            <Ban size={11} />
            <span>Banned</span>
          </Badge>
        );
      case 'PENDING_VERIFICATION':
        return (
          <Badge className="bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
            <AlertTriangle size={11} />
            <span>Pending</span>
          </Badge>
        );
      default:
        return (
          <Badge variant="outline" className="rounded-full px-2.5 py-0.5 text-xs font-medium">
            {status}
          </Badge>
        );
    }
  };

  const getRoleBadge = (roleName: string) => {
    const name = roleName?.toLowerCase() || '';
    if (name.includes('admin')) {
      return (
        <Badge className="bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
          <Shield size={11} />
          <span>Admin</span>
        </Badge>
      );
    } else if (name.includes('staff')) {
      return (
        <Badge className="bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
          <User size={11} />
          <span>Staff</span>
        </Badge>
      );
    } else if (name.includes('recruiter')) {
      return (
        <Badge className="bg-orange-50 dark:bg-orange-950/40 text-orange-700 dark:text-orange-300 border border-orange-200 dark:border-orange-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none inline-flex items-center gap-1">
          <Building size={11} />
          <span>Recruiter</span>
        </Badge>
      );
    } else {
      return (
        <Badge className="bg-zinc-100 dark:bg-zinc-800 text-zinc-700 dark:text-zinc-300 border border-zinc-200 dark:border-zinc-700 rounded-full px-2.5 py-0.5 text-xs font-medium inline-flex items-center gap-1">
          <User size={11} />
          <span>Candidate</span>
        </Badge>
      );
    }
  };

  const accountsTotalPages = accountsData?.data?.totalPages || 1;
  const accountsTotal = accountsData?.data?.total || 0;
  const accountsList = accountsData?.data?.items || [];

  const startResult = accountsTotal > 0 ? (accountsPage - 1) * accountsPageSize + 1 : 0;
  const endResult = Math.min(accountsPage * accountsPageSize, accountsTotal);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Users className="text-[#1877F2] shrink-0 h-8 w-8" />
              User Governance
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Manage user accounts, review access status, and suspend policy-violating users across the platform.
            </p>
          </div>

          <CreateStaffModal onSuccess={(msg) => showToast(msg, 'success')}>
            <Button className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto">
              <UserPlus className="h-4 w-4" />
              Create Staff Account
            </Button>
          </CreateStaffModal>
        </div>

        {/* TẦNG 1: TOOLBAR (Search, Role, Status Filters, Reset, Refresh) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-72 md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search by email, name, company..."
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1 cursor-pointer"
                  title="Clear search"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>

            {/* Role Filter */}
            <Select
              value={selectedRole !== null ? String(selectedRole) : 'ALL'}
              onValueChange={(val) => {
                if (val) setSelectedRole(val === 'ALL' ? null : parseInt(val, 10));
                setAccountsPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[150px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder="Role Filter" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">All Roles</SelectItem>
                <SelectItem value={String(SystemRole.Admin)}>Admin</SelectItem>
                <SelectItem value={String(SystemRole.Staff)}>Staff</SelectItem>
                <SelectItem value={String(SystemRole.Recruiter)}>Recruiter</SelectItem>
                <SelectItem value={String(SystemRole.Candidate)}>Candidate</SelectItem>
              </SelectContent>
            </Select>

            {/* Status Filter */}
            <Select
              value={selectedStatus || 'ALL'}
              onValueChange={(val) => {
                if (val) setSelectedStatus(val === 'ALL' ? null : val);
                setAccountsPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[170px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder="Status Filter" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">All Statuses</SelectItem>
                <SelectItem value="ACTIVE">Active</SelectItem>
                <SelectItem value="INACTIVE">Inactive</SelectItem>
                <SelectItem value="BANNED">Banned</SelectItem>
                <SelectItem value="PENDING_VERIFICATION">Pending Verification</SelectItem>
              </SelectContent>
            </Select>

            {/* Clear Filters Button */}
            {isFilterActive && (
              <Button
                onClick={handleResetFilters}
                variant="ghost"
                className="h-10 px-3 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 font-medium transition-colors cursor-pointer"
              >
                <RotateCcw className="h-3.5 w-3.5 mr-1.5" /> Clear Filters
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
                <TableHead className="w-[22%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  FULL NAME
                </TableHead>

                <TableHead className="w-[26%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  EMAIL
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  ROLE
                </TableHead>

                <TableHead className="w-[15%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  STATUS
                </TableHead>

                <TableHead className="w-[17%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  CREATED DATE
                </TableHead>

                <TableHead className="w-[8%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  ACTIONS
                </TableHead>
              </TableRow>
            </TableHeader>

            {/* Table Body */}
            <TableBody>
              {isAccountsLoading ? (
                // Loading Skeleton State (6 rows)
                Array.from({ length: accountsPageSize || 6 }).map((_, index) => (
                  <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-4/5 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-6 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-6 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-2 text-right">
                      <Skeleton className="h-8 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md ml-auto" />
                    </TableCell>
                  </TableRow>
                ))
              ) : isAccountsError ? (
                // Error State
                <TableRow>
                  <TableCell colSpan={6} className="h-64 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">
                        Failed to load user accounts
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        An error occurred while fetching user accounts data. Please try again.
                      </p>
                    </div>
                  </TableCell>
                </TableRow>
              ) : accountsList.length === 0 ? (
                // Empty State
                <TableRow>
                  <TableCell colSpan={6} className="h-72 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                        <SearchX className="h-6 w-6" />
                      </div>
                      <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                        No user accounts found
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        {isFilterActive
                          ? 'No user accounts match the current filters. Try clearing or adjusting your search criteria.'
                          : 'No user accounts recorded yet.'}
                      </p>
                      {isFilterActive && (
                        <Button
                          onClick={handleResetFilters}
                          variant="outline"
                          className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                        >
                          <RotateCcw className="h-4 w-4 mr-2" /> Clear All Filters
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ) : (
                // Actual Data Rows
                accountsList.map((user) => (
                  <TableRow
                    key={user.id}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Full Name */}
                    <TableCell className="py-3.5 px-3 align-middle font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors">
                      {user.fullName || (user.roleName?.toLowerCase() === 'staff' ? user.email : <span className="text-[#65676B] dark:text-zinc-400 italic font-normal">Not updated</span>)}
                    </TableCell>

                    {/* Email */}
                    <TableCell className="py-3.5 px-3 align-middle font-mono text-xs text-[#65676B] dark:text-zinc-300">
                      {user.email}
                    </TableCell>

                    {/* Role */}
                    <TableCell className="py-3.5 px-3 align-middle text-center">
                      <div className="flex justify-center">
                        {getRoleBadge(user.roleName)}
                      </div>
                    </TableCell>

                    {/* Status */}
                    <TableCell className="py-3.5 px-3 align-middle text-center">
                      <div className="flex justify-center">
                        {getStatusBadge(user.status)}
                      </div>
                    </TableCell>

                    {/* Created Date */}
                    <TableCell className="py-3.5 px-3 align-middle text-sm text-[#65676B] dark:text-zinc-300 font-medium">
                      {format(new Date(user.createdAt), 'MMM dd, yyyy')}
                    </TableCell>

                    {/* Actions */}
                    <TableCell className="py-3.5 px-2 align-middle text-center">
                      <div className="flex items-center justify-center gap-1">
                        <Link href={`/admin/accounts/${user.id}`}>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                            title="View Account Details"
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                        </Link>
                        {user.roleName?.toLowerCase() !== 'admin' && (
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => {
                              setStatusTargetUser({ id: user.id, email: user.email, currentStatus: user.status });
                              setIsStatusModalOpen(true);
                            }}
                            className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                            title="Change User Status"
                          >
                            <Edit2 className="h-4 w-4" />
                          </Button>
                        )}
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
            <div>
              Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{accountsTotal}</span> user accounts
            </div>
            <Select
              value={String(accountsPageSize)}
              onValueChange={(val) => {
                if (val) setAccountsPageSize(Number(val));
                setAccountsPage(1);
              }}
            >
              <SelectTrigger className="h-8 w-[110px] border-[#CED0D4] dark:border-zinc-800 text-xs font-medium focus:ring-[#1877F2]">
                <SelectValue placeholder="Page size" />
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
              disabled={accountsPage === 1 || isAccountsLoading}
              onClick={() => setAccountsPage((prev) => Math.max(1, prev - 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>

            {Array.from({ length: accountsTotalPages }).map((_, index) => {
              const pageNum = index + 1;
              if (
                accountsTotalPages <= 5 ||
                pageNum === 1 ||
                pageNum === accountsTotalPages ||
                Math.abs(pageNum - accountsPage) <= 1
              ) {
                const isCurrent = pageNum === accountsPage;
                return (
                  <Button
                    key={pageNum}
                    variant={isCurrent ? 'default' : 'outline'}
                    disabled={isAccountsLoading}
                    onClick={() => setAccountsPage(pageNum)}
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
                (pageNum === 2 && accountsPage > 3) ||
                (pageNum === accountsTotalPages - 1 && accountsPage < accountsTotalPages - 2)
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
              disabled={accountsPage >= accountsTotalPages || isAccountsLoading}
              onClick={() => setAccountsPage((prev) => Math.min(accountsTotalPages, prev + 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
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
            {toast.type === 'success' && <CheckCircle size={18} className="shrink-0" />}
            {toast.type === 'warning' && <AlertTriangle size={18} className="shrink-0" />}
            {toast.type === 'error' && <XCircle size={18} className="shrink-0" />}
            <span>{toast.message}</span>
            <button
              onClick={() => setToast(null)}
              className="text-muted-foreground hover:text-foreground shrink-0 p-0.5 rounded-lg hover:bg-black/5 cursor-pointer"
            >
              <X size={14} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
