'use client';

import React, { useState, useMemo } from 'react';
import {
  useCreateCertification,
  useUpdateCertification,
} from '@/hooks/useCandidateProfile';
import type { CandidateCertification, CertificationUpsertRequest } from '@/types/candidate.types';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { AlertTriangle, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Card, CardContent } from '@/components/ui/card';
import { useTranslations } from 'next-intl';

const CURRENT_YEAR = new Date().getFullYear();
const YEARS = Array.from({ length: 50 }, (_, i) => CURRENT_YEAR - i + 5);

const parseDateString = (dateStr: string | null | undefined) => {
  if (!dateStr) return { month: '', year: '' };
  const parts = dateStr.split('T')[0].split('-');
  return { year: parts[0] || '', month: parts[1] || '' };
};

const buildDateString = (year: string, month: string) => {
  if (!year && !month) return '';
  return `${year || CURRENT_YEAR}-${month || '01'}-01`;
};

interface CertificationFormProps {
  initialData?: CandidateCertification | null;
  onCancel: () => void;
  onSuccess: () => void;
}

export function CertificationForm({ initialData, onCancel, onSuccess }: CertificationFormProps) {
  const { mutate: createCertification, isPending: isCreating } = useCreateCertification();
  const { mutate: updateCertification, isPending: isUpdating } = useUpdateCertification();
  const t = useTranslations("CandidateProfile");

  const MONTHS = [
    { value: '01', label: '01' },
    { value: '02', label: '02' },
    { value: '03', label: '03' },
    { value: '04', label: '04' },
    { value: '05', label: '05' },
    { value: '06', label: '06' },
    { value: '07', label: '07' },
    { value: '08', label: '08' },
    { value: '09', label: '09' },
    { value: '10', label: '10' },
    { value: '11', label: '11' },
    { value: '12', label: '12' },
  ];

  // Form states
  const [name, setName] = useState(initialData?.name || '');
  const [issuingOrganization, setIssuingOrganization] = useState(initialData?.issuingOrganization || '');
  const [issueDate, setIssueDate] = useState(initialData?.issueDate ? initialData.issueDate.split('T')[0] : '');
  const [expirationDate, setExpirationDate] = useState(initialData?.expirationDate ? initialData.expirationDate.split('T')[0] : '');
  const [credentialUrl, setCredentialUrl] = useState(initialData?.credentialUrl || '');

  // Validation errors
  const [errors, setErrors] = useState<{ name?: string; issuingOrganization?: string }>({});

  // Unsaved changes tracking
  const [showConfirmCancel, setShowConfirmCancel] = useState(false);

  const isDirty = useMemo(() => {
    if (!initialData) {
      return (
        name !== '' ||
        issuingOrganization !== '' ||
        issueDate !== '' ||
        expirationDate !== '' ||
        credentialUrl !== ''
      );
    }
    return (
      name !== (initialData.name || '') ||
      issuingOrganization !== (initialData.issuingOrganization || '') ||
      issueDate !== (initialData.issueDate ? initialData.issueDate.split('T')[0] : '') ||
      expirationDate !== (initialData.expirationDate ? initialData.expirationDate.split('T')[0] : '') ||
      credentialUrl !== (initialData.credentialUrl || '')
    );
  }, [name, issuingOrganization, issueDate, expirationDate, credentialUrl, initialData]);

  const handleCancelClick = () => {
    if (isDirty) {
      setShowConfirmCancel(true);
    } else {
      onCancel();
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: { name?: string; issuingOrganization?: string } = {};
    if (!name.trim()) newErrors.name = t('certNameRequired');
    if (!issuingOrganization.trim()) newErrors.issuingOrganization = t('issuingOrgRequired');

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setErrors({});

    const payload: CertificationUpsertRequest = {
      name,
      issuingOrganization,
      issueDate: issueDate || null,
      expirationDate: expirationDate || null,
      credentialUrl: credentialUrl || null,
    };

    if (initialData) {
      updateCertification(
        { id: initialData.id, payload },
        {
          onSuccess: () => onSuccess(),
          onError: (error: any) => {
            toast.error(error?.response?.data?.message || t('certUpdateError'));
          }
        }
      );
    } else {
      createCertification(payload, {
        onSuccess: () => onSuccess(),
        onError: (error: any) => {
          toast.error(error?.response?.data?.message || t('certSaveError'));
        }
      });
    }
  };

  return (
    <div className="w-full mt-2">
        <form onSubmit={handleSubmit} className="space-y-4" noValidate>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className={cn("sm:col-span-2 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm", errors.name && "border-destructive focus-within:border-destructive focus-within:ring-destructive/30")}>
              <Label htmlFor="name" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">{t('certificationName')} <span className="text-destructive">*</span></Label>
              <input
                id="name"
                placeholder="e.g. AWS Certified Solutions Architect"
                value={name}
                autoFocus
                onChange={(e) => {
                  setName(e.target.value);
                  if (errors.name) setErrors((prev) => ({ ...prev, name: undefined }));
                }}
                className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
              />
              {errors.name && <p className="text-[10px] text-destructive mt-1 font-medium">{errors.name}</p>}
            </div>

            <div className={cn("sm:col-span-2 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm", errors.issuingOrganization && "border-destructive focus-within:border-destructive focus-within:ring-destructive/30")}>
              <Label htmlFor="issuingOrganization" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">{t('issuingOrg')} <span className="text-destructive">*</span></Label>
              <input
                id="issuingOrganization"
                placeholder="e.g. Amazon Web Services (AWS)"
                value={issuingOrganization}
                onChange={(e) => {
                  setIssuingOrganization(e.target.value);
                  if (errors.issuingOrganization) setErrors((prev) => ({ ...prev, issuingOrganization: undefined }));
                }}
                className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
              />
              {errors.issuingOrganization && <p className="text-[10px] text-destructive mt-1 font-medium">{errors.issuingOrganization}</p>}
            </div>

            <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
              <Label className="text-[11px] text-muted-foreground font-semibold block mb-0.5">{t('issueDate')}</Label>
              <div className="flex gap-2">
                <div className="w-1/2 border-r border-border/50 pr-2">
                  <Select
                    value={parseDateString(issueDate).month}
                    onValueChange={(val) => setIssueDate(buildDateString(parseDateString(issueDate).year || '', val || ''))}
                  >
                    <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
                      <SelectValue placeholder={t('month')} />
                    </SelectTrigger>
                    <SelectContent>
                      {MONTHS.map(m => <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="w-1/2">
                  <Select
                    value={parseDateString(issueDate).year}
                    onValueChange={(val) => setIssueDate(buildDateString(val || '', parseDateString(issueDate).month || ''))}
                  >
                    <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
                      <SelectValue placeholder={t('year')} />
                    </SelectTrigger>
                    <SelectContent>
                      {YEARS.map(y => <SelectItem key={y} value={y.toString()}>{y}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
              <Label className="text-[11px] text-muted-foreground font-semibold block mb-0.5">{t('expirationDateOpt')}</Label>
              <div className="flex gap-2">
                <div className="w-1/2 border-r border-border/50 pr-2">
                  <Select
                    value={parseDateString(expirationDate).month}
                    onValueChange={(val) => setExpirationDate(buildDateString(parseDateString(expirationDate).year || '', val || ''))}
                  >
                    <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
                      <SelectValue placeholder={t('month')} />
                    </SelectTrigger>
                    <SelectContent>
                      {MONTHS.map(m => <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="w-1/2">
                  <Select
                    value={parseDateString(expirationDate).year}
                    onValueChange={(val) => setExpirationDate(buildDateString(val || '', parseDateString(expirationDate).month || ''))}
                  >
                    <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
                      <SelectValue placeholder={t('year')} />
                    </SelectTrigger>
                    <SelectContent>
                      {YEARS.map(y => <SelectItem key={y} value={y.toString()}>{y}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            <div className="sm:col-span-2 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
              <Label htmlFor="credentialUrl" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">{t('credentialUrl')}</Label>
              <input
                id="credentialUrl"
                type="url"
                placeholder="e.g. https://www.credly.com/badges/..."
                value={credentialUrl}
                onChange={(e) => setCredentialUrl(e.target.value)}
                className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
              />
            </div>
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-border/10 mt-6">
            <Button
              type="button"
              variant="ghost"
              onClick={handleCancelClick}
              disabled={isCreating || isUpdating}
              className="text-muted-foreground hover:text-foreground hover:bg-muted font-semibold rounded-lg"
            >
              {t('cancel')}
            </Button>
            <Button
              type="submit"
              disabled={!isDirty || isCreating || isUpdating}
              className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-6 shadow-md shadow-primary/10 rounded-lg flex items-center gap-2"
            >
              {(isCreating || isUpdating) && <Loader2 className="w-4 h-4 animate-spin" />}
              {t('save')}
            </Button>
          </div>
        </form>


      {/* Cancel Confirmation Dialog */}
      <Dialog open={showConfirmCancel} onOpenChange={setShowConfirmCancel}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg z-[60]">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">{t('discardChanges')}</DialogTitle>
            <DialogDescription className="text-xs">
              {t('discardChangesDesc')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setShowConfirmCancel(false)}
              className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
            >
              {t('continueEditing')}
            </Button>
            <Button
              onClick={() => {
                setShowConfirmCancel(false);
                onCancel();
              }}
              className="bg-destructive hover:bg-destructive/95 transition-all text-destructive-foreground font-semibold px-6 shadow-md shadow-destructive/10 rounded-lg"
            >
              {t('discardChangesBtn')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
