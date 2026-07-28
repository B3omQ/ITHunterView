'use client';

import React, { useState, useEffect } from 'react';
import {
  Search,
  CheckCircle,
  XCircle,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Building,
  RotateCcw,
  X,
  Eye,
  FileText,
  ExternalLink,
  Check,
  Ban,
  Clock,
  User,
  SearchX,
} from 'lucide-react';
import { toast } from 'sonner';
import { useCompanies, useUpdateCompanyStatus } from '@/hooks/useCompany';
import { Company, CompanyStatus } from '@/types/company.types';
import { CompanyStatusBadge } from '@/components/shared/CompanyStatusBadge';
import { CompanyLogo } from '@/components/shared/CompanyLogo';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

const STATUS_FILTERS = [
  { value: 'ALL', label: 'All Statuses' },
  { value: 'PENDING', label: 'Pending Review' },
  { value: 'PENDING_UPDATE', label: 'Pending Update' },
  { value: 'VERIFIED', label: 'Verified' },
  { value: 'REJECTED', label: 'Rejected' },
];

export default function AdminCompaniesPage() {
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('PENDING'); // Default to PENDING for action items
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Detail & Action Modals state
  const [selectedCompany, setSelectedCompany] = useState<Company | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);

  // Confirm action states
  const [confirmAction, setConfirmAction] = useState<{
    company: Company;
    targetStatus: 'VERIFIED' | 'REJECTED';
  } | null>(null);

  const [rejectReasonInput, setRejectReasonInput] = useState('');

  // Clear reject reason when modal closes
  useEffect(() => {
    if (!confirmAction) {
      setRejectReasonInput('');
    }
  }, [confirmAction]);

  // Debounce search query
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [search]);

  // Fetch Companies complying with kinh-mantra.md (page -> hook -> service -> api-client -> backend)
  const {
    data: companyData,
    isLoading,
    isError,
    refetch,
  } = useCompanies({
    page,
    pageSize,
    search: debouncedSearch || undefined,
    status: statusFilter === 'ALL' ? undefined : statusFilter,
  });

  // Status update mutation
  const { mutateAsync: updateStatus, isPending: isUpdating } = useUpdateCompanyStatus();

  const handleStatusUpdate = async (companyId: string, status: 'VERIFIED' | 'REJECTED') => {
    if (status === 'REJECTED' && !rejectReasonInput.trim()) {
      toast.error('Please provide a reason for the rejection.');
      return;
    }

    try {
      await updateStatus({
        id: companyId,
        dto: {
          status,
          rejectReason: status === 'REJECTED' ? rejectReasonInput.trim() : undefined,
        },
      });
      toast.success(
        status === 'VERIFIED'
          ? 'Company verified successfully!'
          : 'Company rejected successfully!'
      );

      // Update selected company status in detail modal if open
      if (selectedCompany && selectedCompany.id === companyId) {
        if (selectedCompany.hasPendingChange) {
          if (status === 'VERIFIED') {
            setSelectedCompany({
              ...selectedCompany,
              name: selectedCompany.pendingName || selectedCompany.name,
              taxCode: selectedCompany.pendingTaxCode || selectedCompany.taxCode,
              headquartersAddress: selectedCompany.pendingHeadquartersAddress || selectedCompany.headquartersAddress,
              verificationMethod: selectedCompany.pendingVerificationMethod || selectedCompany.verificationMethod,
              verificationDocumentUrl: selectedCompany.pendingVerificationDocumentUrl || selectedCompany.verificationDocumentUrl,
              hasPendingChange: false,
              pendingName: undefined,
              pendingTaxCode: undefined,
              pendingHeadquartersAddress: undefined,
              pendingVerificationMethod: undefined,
              pendingVerificationDocumentUrl: undefined,
              rejectReason: undefined,
            });
          } else {
            setSelectedCompany({
              ...selectedCompany,
              hasPendingChange: false,
              pendingName: undefined,
              pendingTaxCode: undefined,
              pendingHeadquartersAddress: undefined,
              pendingVerificationMethod: undefined,
              pendingVerificationDocumentUrl: undefined,
              rejectReason: rejectReasonInput.trim(),
            });
          }
        } else {
          setSelectedCompany({
            ...selectedCompany,
            status,
            rejectReason: status === 'REJECTED' ? rejectReasonInput.trim() : undefined,
          });
        }
      }

      setConfirmAction(null);
      setRejectReasonInput('');
    } catch (err) {
      toast.error('An error occurred while updating the company verification status.');
    }
  };

  const handleResetFilters = () => {
    setSearch('');
    setStatusFilter('ALL');
    setPage(1);
  };

  const isFilterActive = search !== '' || statusFilter !== 'ALL';
  const totalPages = companyData?.totalPages || 1;
  const totalItems = companyData?.total || 0;
  const companyList = companyData?.items || [];

  const startResult = totalItems > 0 ? (page - 1) * pageSize + 1 : 0;
  const endResult = Math.min(page * pageSize, totalItems);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Building className="text-[#1877F2] shrink-0 h-8 w-8" />
              Company Verification Portal
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Review legal registration documents and verify recruiter company accounts across the platform.
            </p>
          </div>
        </div>

        {/* TẦNG 1: TOOLBAR (Search, Status Filter, Clear Filters) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-80 md:w-96">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search by company name, tax code, address..."
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {search && (
                <button
                  onClick={() => setSearch('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1 cursor-pointer"
                  title="Clear search"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>

            {/* Status Filter Dropdown */}
            <Select
              value={statusFilter}
              onValueChange={(val) => {
                if (val) setStatusFilter(val);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[180px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder="Status Filter" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                {STATUS_FILTERS.map((item) => (
                  <SelectItem key={item.value} value={item.value}>
                    {item.label}
                  </SelectItem>
                ))}
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
                <TableHead className="w-[6%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  LOGO
                </TableHead>

                <TableHead className="w-[23%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  COMPANY NAME
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  TAX CODE
                </TableHead>

                <TableHead className="w-[18%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  INDUSTRY
                </TableHead>

                <TableHead className="w-[21%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  HEADQUARTERS
                </TableHead>

                <TableHead className="w-[12%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  STATUS
                </TableHead>

                <TableHead className="w-[8%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  ACTIONS
                </TableHead>
              </TableRow>
            </TableHeader>

            {/* Table Body */}
            <TableBody>
              {isLoading ? (
                // Loading Skeleton State (6 rows)
                Array.from({ length: pageSize || 6 }).map((_, index) => (
                  <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-10 w-10 rounded-lg bg-slate-100 dark:bg-zinc-800 mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-4/5 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-full bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-6 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full" />
                    </TableCell>
                    <TableCell className="py-3.5 px-2 text-center">
                      <Skeleton className="h-8 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
                    </TableCell>
                  </TableRow>
                ))
              ) : isError ? (
                // Error State
                <TableRow>
                  <TableCell colSpan={7} className="h-64 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">
                        Failed to load company data
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        An error occurred while fetching company data. Please try again.
                      </p>
                    </div>
                  </TableCell>
                </TableRow>
              ) : companyList.length === 0 ? (
                // Empty State
                <TableRow>
                  <TableCell colSpan={7} className="h-72 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                        <SearchX className="h-6 w-6" />
                      </div>
                      <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                        No companies found
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        {isFilterActive
                          ? 'No companies match the current filters. Try clearing or adjusting your search criteria.'
                          : 'No companies registered yet.'}
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
                companyList.map((company: Company) => (
                  <TableRow
                    key={company.id}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Logo */}
                    <TableCell className="py-3.5 px-3 align-middle text-center">
                      <div className="flex items-center justify-center">
                        <div className="w-10 h-10 rounded-lg bg-slate-100 dark:bg-zinc-800 flex items-center justify-center border border-[#CED0D4]/60 dark:border-zinc-700 overflow-hidden shrink-0">
                          <CompanyLogo
                            src={company.logoUrl}
                            alt={company.name}
                            fallbackType="building"
                            fallbackIconClassName="text-[#65676B] dark:text-zinc-400 w-5 h-5"
                            imageClassName="w-full h-full object-cover bg-background"
                          />
                        </div>
                      </div>
                    </TableCell>

                    {/* Company Name */}
                    <TableCell className="py-3.5 px-3 align-middle">
                      <div className="flex flex-col">
                        <span className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors">
                          {company.name}
                        </span>
                        {company.createdByName && (
                          <span className="text-[11px] text-[#65676B] dark:text-zinc-400 mt-0.5 flex items-center gap-1">
                            <User size={10} className="text-[#1877F2]" />
                            <span>By: {company.createdByName}</span>
                          </span>
                        )}
                        {company.website && (
                          <a
                            href={company.website.startsWith('http') ? company.website : `https://${company.website}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs text-[#1877F2] dark:text-blue-400 hover:underline inline-flex items-center gap-1 mt-0.5 font-medium"
                          >
                            <span>Website</span>
                            <ExternalLink size={10} />
                          </a>
                        )}
                      </div>
                    </TableCell>

                    {/* Tax Code */}
                    <TableCell className="py-3.5 px-3 align-middle font-mono text-xs text-[#65676B] dark:text-zinc-300">
                      {company.taxCode || 'N/A'}
                    </TableCell>

                    {/* Industry */}
                    <TableCell
                      className="py-3.5 px-3 align-middle text-sm text-[#65676B] dark:text-zinc-300 max-w-[180px] truncate"
                      title={company.industry || 'N/A'}
                    >
                      {company.industry || 'N/A'}
                    </TableCell>

                    {/* Headquarters */}
                    <TableCell className="py-3.5 px-3 align-middle text-sm text-[#65676B] dark:text-zinc-300 max-w-[200px] truncate" title={company.headquartersAddress}>
                      {company.headquartersAddress || 'N/A'}
                    </TableCell>

                    {/* Status */}
                    <TableCell className="py-3.5 px-3 align-middle">
                      <CompanyStatusBadge status={company.status} hasPendingChange={company.hasPendingChange} />
                    </TableCell>

                    {/* Actions (Icon-only Buttons) */}
                    <TableCell className="py-3.5 px-2 align-middle text-center">
                      <div className="flex items-center justify-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => {
                            setSelectedCompany(company);
                            setIsDetailOpen(true);
                          }}
                          className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                          title="View Verification Details"
                        >
                          <Eye className="h-4 w-4" />
                        </Button>

                        {(company.status === 'PENDING' || company.hasPendingChange) && (
                          <>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => setConfirmAction({ company, targetStatus: 'VERIFIED' })}
                              className="h-8 w-8 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 cursor-pointer"
                              title="Approve / Verify"
                            >
                              <Check className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => setConfirmAction({ company, targetStatus: 'REJECTED' })}
                              className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                              title="Reject Verification"
                            >
                              <Ban className="h-4 w-4" />
                            </Button>
                          </>
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
              Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{totalItems}</span> companies
            </div>
            <Select
              value={String(pageSize)}
              onValueChange={(val) => {
                if (val) setPageSize(Number(val));
                setPage(1);
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
              disabled={page === 1 || isLoading}
              onClick={() => setPage((prev) => Math.max(1, prev - 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>

            {Array.from({ length: totalPages }).map((_, index) => {
              const pageNum = index + 1;
              if (
                totalPages <= 5 ||
                pageNum === 1 ||
                pageNum === totalPages ||
                Math.abs(pageNum - page) <= 1
              ) {
                const isCurrent = pageNum === page;
                return (
                  <Button
                    key={pageNum}
                    variant={isCurrent ? 'default' : 'outline'}
                    disabled={isLoading}
                    onClick={() => setPage(pageNum)}
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
                (pageNum === 2 && page > 3) ||
                (pageNum === totalPages - 1 && page < totalPages - 2)
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
              disabled={page >= totalPages || isLoading}
              onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Modal 1: Detail Modal */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="sm:max-w-[650px] max-h-[90vh] overflow-y-auto">
          {selectedCompany && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-2 text-xl font-extrabold">
                  <Building className="text-[#1877F2]" size={24} />
                  {selectedCompany.name}
                </DialogTitle>
                <DialogDescription>
                  Detailed company verification information and legal documents.
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-6 py-2">
                {/* Header overview card */}
                <div className="flex items-start gap-4 p-4 rounded-xl bg-slate-50 dark:bg-zinc-900 border border-[#CED0D4] dark:border-zinc-800">
                  <div className="w-14 h-14 rounded-xl bg-background border border-[#CED0D4] dark:border-zinc-700 overflow-hidden flex items-center justify-center shrink-0">
                    <CompanyLogo
                      src={selectedCompany.logoUrl}
                      alt={selectedCompany.name}
                      fallbackType="building"
                      fallbackIconClassName="text-muted-foreground w-7 h-7"
                    />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h3 className="font-bold text-base text-foreground truncate">{selectedCompany.name}</h3>
                    <div className="flex items-center gap-2 mt-1">
                      <CompanyStatusBadge status={selectedCompany.status} hasPendingChange={selectedCompany.hasPendingChange} />
                      <span className="text-xs text-muted-foreground font-mono">Tax: {selectedCompany.taxCode || 'N/A'}</span>
                    </div>
                  </div>
                </div>

                {/* Reject Reason Alert if Rejected */}
                {selectedCompany.status === 'REJECTED' && selectedCompany.rejectReason && (
                  <div className="p-4 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800/60 text-rose-700 dark:text-rose-300 text-sm">
                    <div className="font-bold flex items-center gap-1.5 mb-1">
                      <XCircle size={16} />
                      <span>Rejection Reason</span>
                    </div>
                    <p className="text-xs leading-relaxed">{selectedCompany.rejectReason}</p>
                  </div>
                )}

                {/* Pending Update Alert */}
                {selectedCompany.hasPendingChange && (
                  <div className="p-4 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800/60 text-amber-800 dark:text-amber-300 text-sm">
                    <div className="font-bold flex items-center gap-1.5 mb-1">
                      <Clock size={16} />
                      <span>Pending Information Update Request</span>
                    </div>
                    <p className="text-xs leading-relaxed">
                      The recruiter has submitted new company details for re-verification. Compare current vs pending info below.
                    </p>
                  </div>
                )}

                {/* Information Comparison or Display */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="p-3.5 rounded-xl border border-[#CED0D4] dark:border-zinc-800 space-y-2 bg-card">
                    <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Company Name</h4>
                    <p className="text-sm font-semibold text-foreground">{selectedCompany.name}</p>
                    {selectedCompany.hasPendingChange && selectedCompany.pendingName && selectedCompany.pendingName !== selectedCompany.name && (
                      <div className="text-xs text-amber-600 dark:text-amber-400 font-medium pt-1 border-t border-dashed border-amber-500/30">
                        New: <span className="font-bold">{selectedCompany.pendingName}</span>
                      </div>
                    )}
                  </div>

                  <div className="p-3.5 rounded-xl border border-[#CED0D4] dark:border-zinc-800 space-y-2 bg-card">
                    <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Tax Identification Code</h4>
                    <p className="text-sm font-semibold font-mono text-foreground">{selectedCompany.taxCode || 'N/A'}</p>
                    {selectedCompany.hasPendingChange && selectedCompany.pendingTaxCode && selectedCompany.pendingTaxCode !== selectedCompany.taxCode && (
                      <div className="text-xs text-amber-600 dark:text-amber-400 font-mono font-medium pt-1 border-t border-dashed border-amber-500/30">
                        New: <span className="font-bold">{selectedCompany.pendingTaxCode}</span>
                      </div>
                    )}
                  </div>

                  <div className="p-3.5 rounded-xl border border-[#CED0D4] dark:border-zinc-800 space-y-2 bg-card md:col-span-2">
                    <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Headquarters Address</h4>
                    <p className="text-sm font-semibold text-foreground">{selectedCompany.headquartersAddress || 'N/A'}</p>
                    {selectedCompany.hasPendingChange && selectedCompany.pendingHeadquartersAddress && selectedCompany.pendingHeadquartersAddress !== selectedCompany.headquartersAddress && (
                      <div className="text-xs text-amber-600 dark:text-amber-400 font-medium pt-1 border-t border-dashed border-amber-500/30">
                        New: <span className="font-bold">{selectedCompany.pendingHeadquartersAddress}</span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Legal Verification Document Section */}
                <div className="p-4 rounded-xl border border-[#CED0D4] dark:border-zinc-800 space-y-3 bg-slate-50/50 dark:bg-zinc-900/50">
                  <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider flex items-center gap-1.5">
                    <FileText size={14} className="text-[#1877F2]" />
                    <span>Legal Verification Document</span>
                  </h4>

                  {(() => {
                    const docUrl = selectedCompany.hasPendingChange && selectedCompany.pendingVerificationDocumentUrl
                      ? selectedCompany.pendingVerificationDocumentUrl
                      : selectedCompany.verificationDocumentUrl;
                    const method = selectedCompany.hasPendingChange && selectedCompany.pendingVerificationMethod
                      ? selectedCompany.pendingVerificationMethod
                      : selectedCompany.verificationMethod;

                    if (!docUrl) {
                      return <p className="text-xs text-muted-foreground italic">No verification document submitted.</p>;
                    }

                    return (
                      <div className="space-y-3">
                        <div className="flex items-center justify-between text-xs">
                          <span className="text-muted-foreground">Verification Method: <strong className="text-foreground capitalize">{method || 'Business License'}</strong></span>
                          <a
                            href={docUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-[#1877F2] font-bold hover:underline inline-flex items-center gap-1"
                          >
                            <span>Open Full Document</span>
                            <ExternalLink size={12} />
                          </a>
                        </div>

                        {/* Document Preview (Image/PDF wrapper) */}
                        <div className="w-full max-h-[300px] overflow-hidden rounded-xl border border-[#CED0D4] dark:border-zinc-700 bg-background flex items-center justify-center">
                          {docUrl.match(/\.(jpeg|jpg|gif|png|webp)/i) ? (
                            <img src={docUrl} alt="Verification Document" className="w-full h-full object-contain max-h-[300px]" />
                          ) : (
                            <div className="p-8 text-center space-y-2">
                              <FileText size={40} className="mx-auto text-[#1877F2] opacity-60" />
                              <p className="text-xs text-muted-foreground font-medium">Document attached (PDF or external file format)</p>
                            </div>
                          )}
                        </div>
                      </div>
                    );
                  })()}
                </div>
              </div>

              <DialogFooter className="flex-col sm:flex-row gap-2">
                <Button variant="outline" onClick={() => setIsDetailOpen(false)} className="w-full sm:w-auto">
                  Close
                </Button>

                {(selectedCompany.status === 'PENDING' || selectedCompany.hasPendingChange) && (
                  <div className="flex items-center gap-2 w-full sm:w-auto">
                    <Button
                      variant="destructive"
                      onClick={() => {
                        setIsDetailOpen(false);
                        setConfirmAction({ company: selectedCompany, targetStatus: 'REJECTED' });
                      }}
                      className="w-full sm:w-auto gap-1.5"
                    >
                      <Ban size={16} />
                      <span>Reject</span>
                    </Button>
                    <Button
                      onClick={() => {
                        setIsDetailOpen(false);
                        setConfirmAction({ company: selectedCompany, targetStatus: 'VERIFIED' });
                      }}
                      className="w-full sm:w-auto bg-emerald-600 hover:bg-emerald-700 text-white gap-1.5"
                    >
                      <Check size={16} />
                      <span>Approve</span>
                    </Button>
                  </div>
                )}
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Modal 2: Confirm Action (Approve/Reject) Modal */}
      <Dialog open={!!confirmAction} onOpenChange={(open) => !open && setConfirmAction(null)}>
        <DialogContent className="sm:max-w-[450px]">
          {confirmAction && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-2">
                  {confirmAction.targetStatus === 'VERIFIED' ? (
                    <CheckCircle className="text-emerald-500" size={24} />
                  ) : (
                    <XCircle className="text-rose-500" size={24} />
                  )}
                  <span>
                    {confirmAction.targetStatus === 'VERIFIED' ? 'Approve Verification' : 'Reject Verification'}
                  </span>
                </DialogTitle>
                <DialogDescription>
                  Company: <strong className="text-foreground">{confirmAction.company.name}</strong>
                </DialogDescription>
              </DialogHeader>

              <div className="py-3 space-y-4">
                {confirmAction.targetStatus === 'VERIFIED' ? (
                  <p className="text-sm text-muted-foreground">
                    Are you sure you want to verify this company? Once verified, the recruiter will be granted posting privileges and verified company badge.
                  </p>
                ) : (
                  <div className="space-y-2">
                    <label className="text-xs font-bold text-foreground uppercase tracking-wider">
                      Rejection Reason <span className="text-rose-500">*</span>
                    </label>
                    <textarea
                      rows={3}
                      placeholder="Enter the specific reason for rejecting verification (e.g. Invalid tax code, blurred business license)..."
                      value={rejectReasonInput}
                      onChange={(e) => setRejectReasonInput(e.target.value)}
                      className="w-full p-3 border border-[#CED0D4] dark:border-zinc-800 rounded-xl bg-background text-sm outline-none focus:border-[#1877F2] focus:ring-2 focus:ring-[#1877F2]/20 transition-all placeholder:text-muted-foreground"
                    />
                  </div>
                )}
              </div>

              <DialogFooter className="gap-2">
                <Button variant="outline" onClick={() => setConfirmAction(null)} disabled={isUpdating}>
                  Cancel
                </Button>
                <Button
                  onClick={() => handleStatusUpdate(confirmAction.company.id, confirmAction.targetStatus)}
                  disabled={isUpdating}
                  className={confirmAction.targetStatus === 'VERIFIED' ? 'bg-emerald-600 hover:bg-emerald-700 text-white' : 'bg-rose-600 hover:bg-rose-700 text-white'}
                >
                  {isUpdating ? <Loader2 className="animate-spin mr-1.5" size={16} /> : null}
                  <span>Confirm {confirmAction.targetStatus === 'VERIFIED' ? 'Approve' : 'Reject'}</span>
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
