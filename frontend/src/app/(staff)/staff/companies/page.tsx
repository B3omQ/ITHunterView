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
  User
} from 'lucide-react';
import { toast } from 'sonner';
import { useCompanies, useUpdateCompanyStatus } from '@/hooks/useCompany';
import { Company, CompanyStatus } from '@/types/company.types';
import { CompanyStatusBadge } from '@/components/shared/CompanyStatusBadge';
import { CompanyLogo } from '@/components/shared/CompanyLogo';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

const STATUS_FILTERS = [
  { value: 'ALL', label: 'All' },
  { value: 'PENDING', label: 'Pending Review' },
  { value: 'PENDING_UPDATE', label: 'Pending Update' },
  { value: 'VERIFIED', label: 'Verified' },
  { value: 'REJECTED', label: 'Rejected' }
];

export default function StaffCompaniesPage() {
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('PENDING'); // Default to PENDING for action items
  const [page, setPage] = useState(1);
  const pageSize = 10;

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
    }, 300);
    return () => clearTimeout(timer);
  }, [search]);

  // Fetch Companies
  const {
    data: companyData,
    isLoading,
    isError,
    refetch
  } = useCompanies({
    page,
    pageSize,
    search: debouncedSearch || undefined,
    status: statusFilter === 'ALL' ? undefined : statusFilter
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
          rejectReason: status === 'REJECTED' ? rejectReasonInput.trim() : undefined
        }
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
              rejectReason: undefined
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
              rejectReason: rejectReasonInput.trim()
            });
          }
        } else {
          setSelectedCompany({
            ...selectedCompany,
            status,
            rejectReason: status === 'REJECTED' ? rejectReasonInput.trim() : undefined
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

  const totalPages = companyData?.totalPages || 0;
  const totalItems = companyData?.total || 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-border pb-5">
        <div>
          <h1 className="text-2xl font-black tracking-tight text-foreground flex items-center gap-2">
            <Building className="text-primary shrink-0" size={28} />
            Company Verification Portal
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Review legal registration documents and verify recruiter company accounts.
          </p>
        </div>
      </div>

      {/* Tabs & Search Bar */}
      <div className="flex flex-col gap-4">
        {/* Status Filters Tabs */}
        <div className="flex flex-wrap items-center gap-2 border-b border-border pb-1">
          {STATUS_FILTERS.map((tab) => (
            <button
              key={tab.value}
              onClick={() => {
                setStatusFilter(tab.value);
                setPage(1);
              }}
              className={`px-4 py-2 text-sm font-semibold border-b-2 transition-all cursor-pointer ${
                statusFilter === tab.value
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Search Bar */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div className="relative md:col-span-2">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
            <input
              type="text"
              placeholder="Search by company name, tax code, address..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-border rounded-xl bg-background text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground"
            />
            {search && (
              <button
                onClick={() => setSearch('')}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground p-0.5 rounded-full hover:bg-muted"
              >
                <X size={14} />
              </button>
            )}
          </div>

          {(search || statusFilter !== 'PENDING') && (
            <button
              onClick={handleResetFilters}
              className="inline-flex items-center justify-center gap-1.5 px-4 py-2 border border-border hover:bg-muted text-muted-foreground hover:text-foreground font-semibold text-sm rounded-xl transition-colors cursor-pointer"
            >
              <RotateCcw size={14} />
              <span>Reset Filters</span>
            </button>
          )}
        </div>
      </div>

      {/* Grid/Table List */}
      <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-xs">
        {isLoading ? (
          <div className="py-20 flex flex-col items-center justify-center text-muted-foreground gap-3">
            <Loader2 className="animate-spin text-primary" size={32} />
            <span className="text-sm font-medium">Loading companies...</span>
          </div>
        ) : isError ? (
          <div className="py-20 text-center text-rose-500 font-medium">
            Failed to load company data. Please try again.
          </div>
        ) : !companyData?.items?.length ? (
          <div className="py-20 text-center text-muted-foreground">
            No companies found matching the filters.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40 font-bold text-muted-foreground">
                  <th className="px-4 py-4 w-[80px] text-center">Logo</th>
                  <th className="px-4 py-4">Company Name</th>
                  <th className="px-4 py-4">Tax Code</th>
                  <th className="px-4 py-4">Industry</th>
                  <th className="px-4 py-4">Headquarters</th>
                  <th className="px-4 py-4">Status</th>
                  <th className="px-4 py-4 text-center w-[180px]">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {companyData.items.map((company: Company) => (
                  <tr key={company.id} className="hover:bg-muted/10 transition-colors">
                    <td className="px-4 py-4 text-center">
                      <div className="flex items-center justify-center">
                        <div className="w-10 h-10 rounded-lg bg-muted flex items-center justify-center border border-border overflow-hidden">
                          <CompanyLogo
                            src={company.logoUrl}
                            alt={company.name}
                            fallbackType="building"
                            fallbackIconClassName="text-muted-foreground w-5 h-5"
                            imageClassName="w-full h-full object-cover bg-background"
                          />
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-4">
                      <div className="flex flex-col">
                        <span className="font-bold text-foreground">{company.name}</span>
                        {company.createdByName && (
                          <span className="text-[11px] text-muted-foreground mt-0.5 flex items-center gap-1">
                            <User size={10} className="text-primary" />
                            <span>By: {company.createdByName}</span>
                          </span>
                        )}
                        {company.website && (
                          <a
                            href={company.website.startsWith('http') ? company.website : `https://${company.website}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs text-primary hover:underline flex items-center gap-0.5 mt-0.5"
                          >
                            <span>{company.website}</span>
                            <ExternalLink size={10} />
                          </a>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-4 font-mono font-medium text-foreground">{company.taxCode || '-'}</td>
                    <td className="px-4 py-4 text-muted-foreground text-xs">{company.industry || '-'}</td>
                    <td className="px-4 py-4 text-muted-foreground text-xs max-w-[200px] truncate">
                      {company.headquartersAddress || '-'}
                    </td>
                    <td className="px-4 py-4">
                      <CompanyStatusBadge 
                        status={company.status} 
                        hasPendingChange={company.hasPendingChange} 
                        rejectReason={company.rejectReason} 
                      />
                    </td>
                    <td className="px-4 py-4 text-center">
                      <div className="flex items-center justify-center gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setSelectedCompany(company);
                            setIsDetailOpen(true);
                          }}
                          className="cursor-pointer"
                        >
                          <Eye size={14} className="mr-1" />
                          Details
                        </Button>

                        {(company.status === 'PENDING' || company.hasPendingChange) && (
                          <>
                            <Button
                              variant="default"
                              size="sm"
                              onClick={() => {
                                setConfirmAction({ company, targetStatus: 'VERIFIED' });
                              }}
                              className="bg-green-600 hover:bg-green-700 text-white cursor-pointer"
                            >
                              <Check size={14} />
                            </Button>
                            <Button
                              variant="destructive"
                              size="sm"
                              onClick={() => {
                                setConfirmAction({ company, targetStatus: 'REJECTED' });
                              }}
                              className="cursor-pointer"
                            >
                              <Ban size={14} />
                            </Button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Pagination */}
      {companyData && totalPages > 1 && (
        <div className="flex items-center justify-between border border-border bg-card p-4 rounded-2xl shadow-xs">
          <span className="text-sm text-muted-foreground">
            Showing page <strong className="text-foreground">{page}</strong> of{' '}
            <strong className="text-foreground">{totalPages}</strong> pages ({totalItems} results)
          </span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((p) => Math.max(p - 1, 1))}
              disabled={page === 1}
              className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors cursor-pointer"
            >
              <ChevronLeft size={16} />
            </button>
            {Array.from({ length: totalPages }).map((_, i) => {
              const pg = i + 1;
              return (
                <button
                  key={pg}
                  onClick={() => setPage(pg)}
                  className={`w-9 h-9 rounded-xl text-sm font-bold transition-all cursor-pointer ${
                    page === pg
                      ? 'bg-primary text-primary-foreground'
                      : 'border border-border hover:bg-muted text-foreground'
                  }`}
                >
                  {pg}
                </button>
              );
            })}
            <button
              onClick={() => setPage((p) => Math.min(p + 1, totalPages))}
              disabled={page === totalPages}
              className="p-2 border border-border rounded-xl hover:bg-muted disabled:opacity-40 transition-colors cursor-pointer"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* Detail Modal */}
      {selectedCompany && (
        <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
          <DialogContent className="max-w-4xl sm:max-w-4xl">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2 text-xl font-bold">
                <Building size={20} className="text-primary" />
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
                            className="inline-flex items-center gap-1.5 text-primary hover:underline font-bold"
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
                          className="text-primary hover:underline inline-flex items-center gap-1 font-medium"
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
                              className="inline-flex items-center gap-1.5 text-primary hover:underline font-bold"
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
