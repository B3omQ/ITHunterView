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
  User,
  SearchX,
  Building2,
} from 'lucide-react';
import { toast } from 'sonner';
import { useCompanies, useUpdateCompanyStatus } from '@/hooks/useCompany';
import { Company } from '@/types/company.types';
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

export default function StaffCompaniesPage() {
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('PENDING'); // Default to PENDING for staff action items
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

  // Fetch Companies adhering to kinh-mantra.md (page -> hook -> service -> api-client -> backend)
  const {
    data: companyData,
    isLoading,
    isError,
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
              headquartersAddress:
                selectedCompany.pendingHeadquartersAddress || selectedCompany.headquartersAddress,
              verificationMethod:
                selectedCompany.pendingVerificationMethod || selectedCompany.verificationMethod,
              verificationDocumentUrl:
                selectedCompany.pendingVerificationDocumentUrl ||
                selectedCompany.verificationDocumentUrl,
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
  const companiesList = companyData?.items || [];

  const startResult = totalItems > 0 ? (page - 1) * pageSize + 1 : 0;
  const endResult = Math.min(page * pageSize, totalItems);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Card */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Building2 className="text-[#1877F2] shrink-0 h-8 w-8" />
              Company Verification Portal
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Review legal registration documents and verify recruiter company accounts.
            </p>
          </div>
        </div>

        {/* TẦNG 1: TOOLBAR (Search, Filters, Reset) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-72 md:w-80">
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

            {/* Status Filter Select */}
            <Select
              value={statusFilter}
              onValueChange={(val) => {
                if (val) setStatusFilter(val);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[190px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder="Status Filter" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                {STATUS_FILTERS.map((tab) => (
                  <SelectItem key={tab.value} value={tab.value}>
                    {tab.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            {/* Reset Filters Button */}
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
                <TableHead className="w-[7%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  LOGO
                </TableHead>

                <TableHead className="w-[28%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  COMPANY NAME
                </TableHead>

                <TableHead className="w-[14%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  TAX CODE
                </TableHead>

                <TableHead className="w-[14%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  INDUSTRY
                </TableHead>

                <TableHead className="w-[17%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  HEADQUARTERS
                </TableHead>

                <TableHead className="w-[12%] text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400 px-3 py-3">
                  STATUS
                </TableHead>

                <TableHead className="w-[8%] text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400 px-2 py-3">
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
                    <TableCell className="py-3.5 px-2 text-center">
                      <Skeleton className="h-10 w-10 rounded-lg mx-auto bg-slate-100 dark:bg-zinc-800" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <div className="space-y-1">
                        <Skeleton className="h-4 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        <Skeleton className="h-3 w-1/2 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                      </div>
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-28 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-6 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
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
                        Failed to load company records
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        An error occurred while fetching company verification data.
                      </p>
                    </div>
                  </TableCell>
                </TableRow>
              ) : companiesList.length === 0 ? (
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
                          ? 'No records match the current filters. Try clearing or adjusting your search criteria.'
                          : 'No company verification requests found.'}
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
                companiesList.map((company: Company) => (
                  <TableRow
                    key={company.id}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Logo Column */}
                    <TableCell className="py-3 px-2 text-center align-top">
                      <div className="flex items-center justify-center mt-0.5">
                        <div className="w-10 h-10 rounded-lg bg-zinc-100 dark:bg-zinc-800 flex items-center justify-center border border-zinc-200 dark:border-zinc-700 overflow-hidden shrink-0">
                          <CompanyLogo
                            src={company.logoUrl}
                            alt={company.name}
                            fallbackType="building"
                            fallbackIconClassName="text-zinc-400 w-5 h-5"
                            imageClassName="w-full h-full object-cover bg-background"
                          />
                        </div>
                      </div>
                    </TableCell>

                    {/* Company Name Column */}
                    <TableCell className="py-3 px-3 align-top">
                      <div className="flex flex-col gap-0.5 mt-0.5">
                        <span className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors block line-clamp-1">
                          {company.name}
                        </span>
                        {company.createdByName && (
                          <span className="text-[11px] text-[#65676B] dark:text-zinc-400 flex items-center gap-1">
                            <User size={10} className="text-[#1877F2] shrink-0" />
                            <span>By: {company.createdByName}</span>
                          </span>
                        )}
                        {company.website && (
                          <a
                            href={
                              company.website.startsWith('http')
                                ? company.website
                                : `https://${company.website}`
                            }
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs text-[#1877F2] dark:text-blue-400 hover:underline flex items-center gap-0.5 mt-0.5"
                          >
                            <span className="truncate max-w-[180px]">{company.website}</span>
                            <ExternalLink size={10} className="shrink-0" />
                          </a>
                        )}
                      </div>
                    </TableCell>

                    {/* Tax Code Column */}
                    <TableCell className="py-3 px-3 align-top font-mono text-xs font-semibold text-[#050505] dark:text-zinc-200">
                      <div className="mt-1">{company.taxCode || '-'}</div>
                    </TableCell>

                    {/* Industry Column */}
                    <TableCell className="py-3 px-3 align-top text-xs text-[#65676B] dark:text-zinc-400">
                      <div className="mt-1 truncate max-w-[140px]">{company.industry || '-'}</div>
                    </TableCell>

                    {/* Headquarters Column */}
                    <TableCell className="py-3 px-3 align-top text-xs text-[#65676B] dark:text-zinc-400">
                      <div className="mt-1 max-w-[180px] truncate" title={company.headquartersAddress || undefined}>
                        {company.headquartersAddress || '-'}
                      </div>
                    </TableCell>

                    {/* Status Column */}
                    <TableCell className="py-3 px-3 align-top text-center">
                      <div className="mt-0.5 flex justify-center">
                        <CompanyStatusBadge
                          status={company.status}
                          hasPendingChange={company.hasPendingChange}
                          rejectReason={company.rejectReason}
                        />
                      </div>
                    </TableCell>

                    {/* Actions Column */}
                    <TableCell className="py-3 px-2 align-top text-center">
                      <div className="flex items-center justify-center gap-1.5 mt-0.5">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => {
                            setSelectedCompany(company);
                            setIsDetailOpen(true);
                          }}
                          className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                          title="View Details"
                        >
                          <Eye size={15} />
                        </Button>

                        {(company.status === 'PENDING' || company.hasPendingChange) && (
                          <>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => {
                                setConfirmAction({ company, targetStatus: 'VERIFIED' });
                              }}
                              className="h-8 w-8 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 cursor-pointer"
                              title="Approve Verification"
                            >
                              <Check size={15} />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => {
                                setConfirmAction({ company, targetStatus: 'REJECTED' });
                              }}
                              className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                              title="Reject Verification"
                            >
                              <Ban size={15} />
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

      {/* Detail Modal */}
      {selectedCompany && (
        <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
          <DialogContent className="max-w-4xl sm:max-w-4xl">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2 text-xl font-bold">
                <Building size={20} className="text-[#1877F2]" />
                Company Verification Details
              </DialogTitle>
              <DialogDescription>
                Review detailed profile information and legal verification documents.
              </DialogDescription>
            </DialogHeader>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 py-4">
              {/* Left Column: Logo & status */}
              <div className="flex flex-col items-center justify-start text-center border-b md:border-b-0 md:border-r border-border pb-6 md:pb-0 md:pr-6 gap-4">
                <div className="w-24 h-24 rounded-2xl bg-muted flex items-center justify-center border border-border shadow-sm overflow-hidden">
                  <CompanyLogo
                    src={selectedCompany.logoUrl}
                    alt={selectedCompany.name}
                    fallbackType="building"
                    fallbackIconClassName="text-muted-foreground w-12 h-12"
                    imageClassName="w-full h-full object-cover bg-background"
                  />
                </div>
                <div>
                  <h3 className="font-bold text-lg text-foreground line-clamp-2">{selectedCompany.name}</h3>
                  <p className="text-xs text-muted-foreground mt-1">{selectedCompany.industry}</p>
                </div>
                <CompanyStatusBadge 
                  status={selectedCompany.status} 
                  hasPendingChange={selectedCompany.hasPendingChange} 
                />
              </div>

              {/* Right Column: Detailed info */}
              {selectedCompany.hasPendingChange ? (
                <div className="col-span-2 space-y-4 text-sm max-h-[450px] overflow-y-auto pr-2">
                  <div className="grid grid-cols-2 gap-4 border-b border-border pb-2 bg-blue-50/50 p-3 rounded-xl border border-blue-100">
                    <div>
                      <h4 className="font-bold text-xs text-muted-foreground uppercase tracking-wider">Current Verified Info</h4>
                    </div>
                    <div>
                      <h4 className="font-bold text-xs text-primary uppercase tracking-wider">Requested Changes</h4>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 p-1">
                    <div className={selectedCompany.name !== selectedCompany.pendingName ? "bg-rose-50/70 border border-rose-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-muted-foreground font-semibold">Company Name</label>
                      <p className="font-semibold text-foreground mt-0.5">{selectedCompany.name}</p>
                    </div>
                    <div className={selectedCompany.name !== selectedCompany.pendingName ? "bg-emerald-50/70 border border-emerald-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-emerald-800 font-semibold">Company Name (Pending)</label>
                      <p className="font-bold text-foreground mt-0.5">{selectedCompany.pendingName || selectedCompany.name}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 p-1">
                    <div className={selectedCompany.taxCode !== selectedCompany.pendingTaxCode ? "bg-rose-50/70 border border-rose-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-muted-foreground font-semibold">Tax Code</label>
                      <p className="font-mono font-medium text-foreground mt-0.5">{selectedCompany.taxCode || 'Not provided'}</p>
                    </div>
                    <div className={selectedCompany.taxCode !== selectedCompany.pendingTaxCode ? "bg-emerald-50/70 border border-emerald-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-emerald-800 font-semibold">Tax Code (Pending)</label>
                      <p className="font-mono font-bold text-foreground mt-0.5">{selectedCompany.pendingTaxCode || selectedCompany.taxCode || 'Not provided'}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 p-1">
                    <div className={selectedCompany.headquartersAddress !== selectedCompany.pendingHeadquartersAddress ? "bg-rose-50/70 border border-rose-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-muted-foreground font-semibold">Headquarters Address</label>
                      <p className="font-medium text-foreground mt-0.5">{selectedCompany.headquartersAddress || 'Not provided'}</p>
                    </div>
                    <div className={selectedCompany.headquartersAddress !== selectedCompany.pendingHeadquartersAddress ? "bg-emerald-50/70 border border-emerald-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-emerald-800 font-semibold">Headquarters Address (Pending)</label>
                      <p className="font-bold text-foreground mt-0.5">{selectedCompany.pendingHeadquartersAddress || selectedCompany.headquartersAddress || 'Not provided'}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 p-1">
                    <div className={selectedCompany.verificationMethod !== selectedCompany.pendingVerificationMethod ? "bg-rose-50/70 border border-rose-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-muted-foreground font-semibold">Verification Method</label>
                      <p className="font-medium text-foreground mt-0.5">
                        {selectedCompany.verificationMethod === 'BUSINESS_REGISTRATION'
                          ? 'Business Registration License'
                          : selectedCompany.verificationMethod === 'POA_AND_ID'
                          ? 'Power of Attorney & ID Card'
                          : 'Not selected'}
                      </p>
                    </div>
                    <div className={selectedCompany.verificationMethod !== selectedCompany.pendingVerificationMethod ? "bg-emerald-50/70 border border-emerald-100 p-2.5 rounded-xl" : "p-2.5"}>
                      <label className="text-xs text-emerald-800 font-semibold">Verification Method (Pending)</label>
                      <p className="font-bold text-foreground mt-0.5">
                        {selectedCompany.pendingVerificationMethod === 'BUSINESS_REGISTRATION'
                          ? 'Business Registration License'
                          : selectedCompany.pendingVerificationMethod === 'POA_AND_ID'
                          ? 'Power of Attorney & ID Card'
                          : 'Not selected'}
                      </p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 p-2 bg-muted/40 rounded-xl border border-border">
                    <div className="p-2">
                      <label className="text-xs text-muted-foreground font-semibold">Verification Document</label>
                      <p className="mt-0.5">
                        {selectedCompany.verificationDocumentUrl ? (
                          <a
                            href={selectedCompany.verificationDocumentUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1.5 text-[#1877F2] hover:underline font-bold"
                          >
                            <FileText size={14} />
                            <span>Original Doc</span>
                            <ExternalLink size={10} />
                          </a>
                        ) : (
                          <span className="text-muted-foreground italic">No document</span>
                        )}
                      </p>
                    </div>
                    <div className="p-2">
                      <label className="text-xs text-emerald-800 font-semibold">Verification Document (Pending)</label>
                      <p className="mt-0.5">
                        {selectedCompany.pendingVerificationDocumentUrl ? (
                          <a
                            href={selectedCompany.pendingVerificationDocumentUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1.5 text-emerald-600 hover:underline font-bold"
                          >
                            <FileText size={14} />
                            <span>Pending Doc</span>
                            <ExternalLink size={10} />
                          </a>
                        ) : (
                          <span className="text-muted-foreground italic">No pending document</span>
                        )}
                      </p>
                    </div>
                  </div>

                  <div className="border-t border-border pt-4 grid grid-cols-2 gap-4">
                    <div>
                      <label className="text-xs text-muted-foreground font-semibold">Company Size</label>
                      <p className="font-medium text-foreground mt-0.5">{selectedCompany.companySize || 'Not provided'}</p>
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground font-semibold">Submitted By</label>
                      <p className="font-medium text-foreground mt-0.5">
                        {selectedCompany.createdByName ? (
                          <span className="flex flex-col">
                            <span className="font-semibold">{selectedCompany.createdByName}</span>
                            <span className="text-xs text-muted-foreground">{selectedCompany.createdByEmail}</span>
                          </span>
                        ) : (
                          'N/A'
                        )}
                      </p>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="col-span-2 space-y-4 text-sm">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="text-xs text-muted-foreground font-semibold">Tax Code</label>
                      <p className="font-mono font-medium text-foreground mt-0.5">{selectedCompany.taxCode || 'Not provided'}</p>
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground font-semibold">Company Size</label>
                      <p className="font-medium text-foreground mt-0.5">{selectedCompany.companySize || 'Not provided'}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div>
                      <label className="text-xs text-muted-foreground font-semibold">Headquarters Address</label>
                      <p className="font-medium text-foreground mt-0.5">{selectedCompany.headquartersAddress || 'Not provided'}</p>
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground font-semibold">Submitted By (Applicant)</label>
                      <p className="font-medium text-foreground mt-0.5">
                        {selectedCompany.createdByName ? (
                          <span className="flex flex-col">
                            <span className="font-semibold">{selectedCompany.createdByName}</span>
                            <span className="text-xs text-muted-foreground">{selectedCompany.createdByEmail}</span>
                          </span>
                        ) : (
                          'N/A'
                        )}
                      </p>
                    </div>
                  </div>

                  {selectedCompany.website && (
                    <div>
                      <label className="text-xs text-muted-foreground font-semibold">Website</label>
                      <p className="mt-0.5">
                        <a
                          href={selectedCompany.website.startsWith('http') ? selectedCompany.website : `https://${selectedCompany.website}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-[#1877F2] hover:underline inline-flex items-center gap-1 font-medium"
                        >
                          {selectedCompany.website}
                          <ExternalLink size={12} />
                        </a>
                      </p>
                    </div>
                  )}

                  <div>
                    <label className="text-xs text-muted-foreground font-semibold">Description</label>
                    <p className="text-muted-foreground text-xs mt-1 leading-relaxed max-h-[120px] overflow-y-auto pr-1">
                      {selectedCompany.description || 'No description available.'}
                    </p>
                  </div>

                  <div className="border-t border-border pt-4 space-y-3 bg-muted/30 p-3 rounded-xl border">
                    <h4 className="font-bold text-xs text-foreground uppercase tracking-wider">Legal Documents & Verification</h4>
                    
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
                      <div>
                        <span className="text-muted-foreground">Verification Method:</span>
                        <p className="font-semibold text-foreground mt-0.5">
                          {selectedCompany.verificationMethod === 'BUSINESS_REGISTRATION'
                            ? 'Business Registration License'
                            : selectedCompany.verificationMethod === 'POA_AND_ID'
                            ? 'Power of Attorney & ID Card'
                            : 'Not selected'}
                        </p>
                      </div>

                      <div>
                        <span className="text-muted-foreground">Attached Document:</span>
                        <p className="mt-0.5">
                          {selectedCompany.verificationDocumentUrl ? (
                            <a
                              href={selectedCompany.verificationDocumentUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center gap-1.5 text-[#1877F2] hover:underline font-bold"
                            >
                              <FileText size={14} />
                              <span>Download / View Document</span>
                              <ExternalLink size={10} />
                            </a>
                          ) : (
                            <span className="text-muted-foreground italic">No document uploaded</span>
                          )}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>

            <DialogFooter className="flex sm:justify-between items-center border-t border-border pt-4">
              <Button variant="ghost" onClick={() => setIsDetailOpen(false)}>
                Close
              </Button>
              
              {(selectedCompany.status === 'PENDING' || selectedCompany.hasPendingChange) && (
                <div className="flex gap-2">
                  <Button
                    variant="destructive"
                    onClick={() => {
                      setConfirmAction({ company: selectedCompany, targetStatus: 'REJECTED' });
                    }}
                    className="cursor-pointer"
                  >
                    <Ban size={14} className="mr-1.5" />
                    Reject
                  </Button>
                  <Button
                    variant="default"
                    onClick={() => {
                      setConfirmAction({ company: selectedCompany, targetStatus: 'VERIFIED' });
                    }}
                    className="bg-green-600 hover:bg-green-700 text-white cursor-pointer"
                  >
                    <Check size={14} className="mr-1.5" />
                    Approve
                  </Button>
                </div>
              )}
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}

      {/* Confirmation Dialog */}
      {confirmAction && (
        <Dialog open={!!confirmAction} onOpenChange={(open) => !open && setConfirmAction(null)}>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                {confirmAction.targetStatus === 'VERIFIED' ? (
                  <CheckCircle className="text-green-600" size={20} />
                ) : (
                  <XCircle className="text-red-600" size={20} />
                )}
                Confirm Action
              </DialogTitle>
              <DialogDescription>
                Are you sure you want to {confirmAction.targetStatus === 'VERIFIED' ? 'APPROVE' : 'REJECT'}{' '}
                {confirmAction.company.hasPendingChange ? 'the pending update request for' : 'the company'}{' '}
                <strong>{confirmAction.company.name}</strong>?
              </DialogDescription>
            </DialogHeader>

            {confirmAction.targetStatus === 'REJECTED' && (
              <div className="mt-4 space-y-2">
                <label className="text-xs font-semibold text-muted-foreground block">
                  Rejection Reason (Required)
                </label>
                <textarea
                  className="w-full min-h-[80px] p-2.5 border border-border rounded-xl text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all bg-background text-foreground"
                  placeholder="Provide details about why the registration or change request is being rejected..."
                  value={rejectReasonInput}
                  onChange={(e) => setRejectReasonInput(e.target.value)}
                />
              </div>
            )}

            <DialogFooter className="mt-6">
              <Button
                variant="ghost"
                onClick={() => setConfirmAction(null)}
                disabled={isUpdating}
              >
                Cancel
              </Button>
              <Button
                variant={confirmAction.targetStatus === 'VERIFIED' ? 'default' : 'destructive'}
                onClick={() => handleStatusUpdate(confirmAction.company.id, confirmAction.targetStatus)}
                disabled={isUpdating}
                className={confirmAction.targetStatus === 'VERIFIED' ? 'bg-green-600 hover:bg-green-700 text-white cursor-pointer' : 'cursor-pointer'}
              >
                {isUpdating && <Loader2 className="animate-spin mr-1.5" size={14} />}
                Confirm
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
