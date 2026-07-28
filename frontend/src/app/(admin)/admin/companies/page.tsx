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

export default function AdminCompaniesPage() {
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
    <div className="w-full pb-8 space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-border pb-5">
        <div>
          <h1 className="text-2xl font-black tracking-tight text-foreground flex items-center gap-2">
            <Building className="text-primary shrink-0" size={28} />
            Company Verification Portal (Admin)
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Review legal registration documents and verify recruiter company accounts across the platform.
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
                            className="text-xs text-primary hover:underline inline-flex items-center gap-1 mt-0.5"
                          >
                            <span>Website</span>
                            <ExternalLink size={10} />
                          </a>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-4 font-mono text-xs">{company.taxCode || 'N/A'}</td>
                    <td className="px-4 py-4 text-muted-foreground">{company.industry || 'N/A'}</td>
                    <td className="px-4 py-4 max-w-[200px] truncate text-muted-foreground" title={company.headquartersAddress}>
                      {company.headquartersAddress || 'N/A'}
                    </td>
                    <td className="px-4 py-4">
                      <CompanyStatusBadge status={company.status} hasPendingChange={company.hasPendingChange} />
                    </td>
                    <td className="px-4 py-4">
                      <div className="flex items-center justify-center gap-1.5">
                        <button
                          onClick={() => {
                            setSelectedCompany(company);
                            setIsDetailOpen(true);
                          }}
                          className="px-2.5 py-1.5 bg-muted hover:bg-muted/80 text-foreground font-semibold text-xs rounded-lg transition-colors inline-flex items-center gap-1 cursor-pointer"
                          title="View Verification Details"
                        >
                          <Eye size={14} />
                          <span>View</span>
                        </button>

                        {(company.status === 'PENDING' || company.hasPendingChange) && (
                          <>
                            <button
                              onClick={() => setConfirmAction({ company, targetStatus: 'VERIFIED' })}
                              className="p-1.5 bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 font-semibold rounded-lg transition-colors cursor-pointer"
                              title="Approve / Verify"
                            >
                              <Check size={16} />
                            </button>
                            <button
                              onClick={() => setConfirmAction({ company, targetStatus: 'REJECTED' })}
                              className="p-1.5 bg-rose-500/10 hover:bg-rose-500/20 text-rose-600 dark:text-rose-400 font-semibold rounded-lg transition-colors cursor-pointer"
                              title="Reject Verification"
                            >
                              <Ban size={16} />
                            </button>
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

        {/* Pagination Bar */}
        {totalPages > 1 && (
          <div className="p-4 border-t border-border bg-muted/20 flex flex-col sm:flex-row items-center justify-between gap-4">
            <span className="text-xs text-muted-foreground font-medium">
              Showing {((page - 1) * pageSize) + 1} - {Math.min(page * pageSize, totalItems)} of {totalItems} companies
            </span>
            <div className="flex items-center gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
                className="p-1.5 border border-border rounded-lg text-muted-foreground hover:text-foreground disabled:opacity-40 disabled:cursor-not-allowed hover:bg-muted transition-colors"
              >
                <ChevronLeft size={16} />
              </button>
              <span className="text-xs font-semibold px-2 text-foreground">
                Page {page} of {totalPages}
              </span>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage(page + 1)}
                className="p-1.5 border border-border rounded-lg text-muted-foreground hover:text-foreground disabled:opacity-40 disabled:cursor-not-allowed hover:bg-muted transition-colors"
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Modal 1: Detail Modal */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="sm:max-w-[650px] max-h-[90vh] overflow-y-auto">
          {selectedCompany && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-2 text-xl font-extrabold">
                  <Building className="text-primary" size={24} />
                  {selectedCompany.name}
                </DialogTitle>
                <DialogDescription>
                  Detailed company verification information and legal documents.
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-6 py-2">
                {/* Header overview card */}
                <div className="flex items-start gap-4 p-4 rounded-xl bg-muted/40 border border-border">
                  <div className="w-14 h-14 rounded-xl bg-background border border-border overflow-hidden flex items-center justify-center shrink-0">
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
                  <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-600 dark:text-rose-400 text-sm">
                    <div className="font-bold flex items-center gap-1.5 mb-1">
                      <XCircle size={16} />
                      <span>Rejection Reason</span>
                    </div>
                    <p className="text-xs leading-relaxed">{selectedCompany.rejectReason}</p>
                  </div>
                )}

                {/* Pending Update Alert */}
                {selectedCompany.hasPendingChange && (
                  <div className="p-4 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-700 dark:text-amber-300 text-sm">
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
                  <div className="p-3.5 rounded-xl border border-border space-y-2 bg-card">
                    <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Company Name</h4>
                    <p className="text-sm font-semibold text-foreground">{selectedCompany.name}</p>
                    {selectedCompany.hasPendingChange && selectedCompany.pendingName && selectedCompany.pendingName !== selectedCompany.name && (
                      <div className="text-xs text-amber-600 dark:text-amber-400 font-medium pt-1 border-t border-dashed border-amber-500/30">
                        New: <span className="font-bold">{selectedCompany.pendingName}</span>
                      </div>
                    )}
                  </div>

                  <div className="p-3.5 rounded-xl border border-border space-y-2 bg-card">
                    <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Tax Identification Code</h4>
                    <p className="text-sm font-semibold font-mono text-foreground">{selectedCompany.taxCode || 'N/A'}</p>
                    {selectedCompany.hasPendingChange && selectedCompany.pendingTaxCode && selectedCompany.pendingTaxCode !== selectedCompany.taxCode && (
                      <div className="text-xs text-amber-600 dark:text-amber-400 font-mono font-medium pt-1 border-t border-dashed border-amber-500/30">
                        New: <span className="font-bold">{selectedCompany.pendingTaxCode}</span>
                      </div>
                    )}
                  </div>

                  <div className="p-3.5 rounded-xl border border-border space-y-2 bg-card md:col-span-2">
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
                <div className="p-4 rounded-xl border border-border space-y-3 bg-muted/20">
                  <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider flex items-center gap-1.5">
                    <FileText size={14} className="text-primary" />
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
                            className="text-primary font-bold hover:underline inline-flex items-center gap-1"
                          >
                            <span>Open Full Document</span>
                            <ExternalLink size={12} />
                          </a>
                        </div>

                        {/* Document Preview (Image/PDF wrapper) */}
                        <div className="w-full max-h-[300px] overflow-hidden rounded-xl border border-border bg-background flex items-center justify-center">
                          {docUrl.match(/\.(jpeg|jpg|gif|png|webp)/i) ? (
                            <img src={docUrl} alt="Verification Document" className="w-full h-full object-contain max-h-[300px]" />
                          ) : (
                            <div className="p-8 text-center space-y-2">
                              <FileText size={40} className="mx-auto text-primary opacity-60" />
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
                      className="w-full p-3 border border-border rounded-xl bg-background text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground"
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
