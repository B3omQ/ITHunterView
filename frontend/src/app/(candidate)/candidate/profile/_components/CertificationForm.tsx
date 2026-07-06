'use client';

import React, { useState, useMemo } from 'react';
import {
  useCreateCertification,
  useUpdateCertification,
} from '@/hooks/useCandidateProfile';
import type { CandidateCertification, CertificationUpsertRequest } from '@/types/candidate.types';
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

const MONTHS = [
  { value: '01', label: 'January' },
  { value: '02', label: 'February' },
  { value: '03', label: 'March' },
  { value: '04', label: 'April' },
  { value: '05', label: 'May' },
  { value: '06', label: 'June' },
  { value: '07', label: 'July' },
  { value: '08', label: 'August' },
  { value: '09', label: 'September' },
  { value: '10', label: 'October' },
  { value: '11', label: 'November' },
  { value: '12', label: 'December' },
];

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
    if (!name.trim()) newErrors.name = 'Certification Name is required';
    if (!issuingOrganization.trim()) newErrors.issuingOrganization = 'Issuing Organization is required';

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
            toast.error(error?.response?.data?.message || 'Failed to update certification. Please try again.');
          }
        }
      );
    } else {
      createCertification(payload, {
        onSuccess: () => onSuccess(),
        onError: (error: any) => {
          toast.error(error?.response?.data?.message || 'Failed to save certification. Please try again.');
        }
      });
    }
  };

  return (
    <Card className="border border-primary/20 bg-primary/5 rounded-xl overflow-hidden shadow-sm animate-in slide-in-from-top-4 duration-300">
      <CardContent className="p-5 sm:p-6">
        <form onSubmit={handleSubmit} className="space-y-4" noValidate>
          <div className="mb-4">
            <h3 className="text-lg font-bold text-foreground">
              {initialData ? 'Edit Certification' : 'Add Certification'}
            </h3>
            <p className="text-xs text-muted-foreground">Fill in the details of your certification below</p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="name" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Certification Name *</Label>
              <Input
                id="name"
                placeholder="e.g. AWS Certified Solutions Architect"
                value={name}
                autoFocus
                onChange={(e) => {
                  setName(e.target.value);
                  if (errors.name) setErrors((prev) => ({ ...prev, name: undefined }));
                }}
                className={`bg-background/80 focus-visible:ring-primary/30 ${errors.name ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
              />
              {errors.name && <p className="text-xs text-destructive mt-1 font-medium">{errors.name}</p>}
            </div>

            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="issuingOrganization" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Issuing Organization *</Label>
              <Input
                id="issuingOrganization"
                placeholder="e.g. Amazon Web Services (AWS)"
                value={issuingOrganization}
                onChange={(e) => {
                  setIssuingOrganization(e.target.value);
                  if (errors.issuingOrganization) setErrors((prev) => ({ ...prev, issuingOrganization: undefined }));
                }}
                className={`bg-background/80 focus-visible:ring-primary/30 ${errors.issuingOrganization ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
              />
              {errors.issuingOrganization && <p className="text-xs text-destructive mt-1 font-medium">{errors.issuingOrganization}</p>}
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Issue Date</Label>
              <div className="flex gap-2">
                <div className="w-1/2">
                  <Select
                    value={parseDateString(issueDate).month}
                    onValueChange={(val) => setIssueDate(buildDateString(parseDateString(issueDate).year || '', val || ''))}
                  >
                    <SelectTrigger className="w-full bg-background/80 border-border/60 focus:ring-primary/30">
                      <SelectValue placeholder="Month" />
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
                    <SelectTrigger className="w-full bg-background/80 border-border/60 focus:ring-primary/30">
                      <SelectValue placeholder="Year" />
                    </SelectTrigger>
                    <SelectContent>
                      {YEARS.map(y => <SelectItem key={y} value={y.toString()}>{y}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Expiration Date (Optional)</Label>
              <div className="flex gap-2">
                <div className="w-1/2">
                  <Select
                    value={parseDateString(expirationDate).month}
                    onValueChange={(val) => setExpirationDate(buildDateString(parseDateString(expirationDate).year || '', val || ''))}
                  >
                    <SelectTrigger className="w-full bg-background/80 border-border/60 focus:ring-primary/30">
                      <SelectValue placeholder="Month" />
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
                    <SelectTrigger className="w-full bg-background/80 border-border/60 focus:ring-primary/30">
                      <SelectValue placeholder="Year" />
                    </SelectTrigger>
                    <SelectContent>
                      {YEARS.map(y => <SelectItem key={y} value={y.toString()}>{y}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="credentialUrl" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Credential URL</Label>
              <Input
                id="credentialUrl"
                type="url"
                placeholder="e.g. https://www.credly.com/badges/..."
                value={credentialUrl}
                onChange={(e) => setCredentialUrl(e.target.value)}
                className="bg-background/80 border-border/60 focus-visible:ring-primary/30"
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
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={!isDirty || isCreating || isUpdating}
              className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-6 shadow-md shadow-primary/10 rounded-lg flex items-center gap-2"
            >
              {(isCreating || isUpdating) && <Loader2 className="w-4 h-4 animate-spin" />}
              Save
            </Button>
          </div>
        </form>
      </CardContent>

      {/* Cancel Confirmation Dialog */}
      <Dialog open={showConfirmCancel} onOpenChange={setShowConfirmCancel}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg z-[60]">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Discard Unsaved Changes?</DialogTitle>
            <DialogDescription className="text-xs">
              You have unsaved changes. Are you sure you want to discard them? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setShowConfirmCancel(false)}
              className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
            >
              Continue Editing
            </Button>
            <Button
              onClick={() => {
                setShowConfirmCancel(false);
                onCancel();
              }}
              className="bg-destructive hover:bg-destructive/95 transition-all text-destructive-foreground font-semibold px-6 shadow-md shadow-destructive/10 rounded-lg"
            >
              Discard Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
