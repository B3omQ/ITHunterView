'use client';

import React, { useState, useMemo } from 'react';
import {
  useCreateExperience,
  useUpdateExperience,
} from '@/hooks/useCandidateProfile';
import type { CandidateExperience, ExperienceUpsertRequest } from '@/types/candidate.types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
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
const YEARS = Array.from({ length: 50 }, (_, i) => CURRENT_YEAR - i);

const parseDateString = (dateStr: string | null | undefined) => {
  if (!dateStr) return { month: '', year: '' };
  const parts = dateStr.split('T')[0].split('-');
  return { year: parts[0] || '', month: parts[1] || '' };
};

const buildDateString = (year: string, month: string) => {
  if (!year && !month) return '';
  return `${year || CURRENT_YEAR}-${month || '01'}-01`;
};

interface ExperienceFormProps {
  initialData?: CandidateExperience | null;
  onCancel: () => void;
  onSuccess: () => void;
}

export function ExperienceForm({ initialData, onCancel, onSuccess }: ExperienceFormProps) {
  const { mutate: createExperience, isPending: isCreating } = useCreateExperience();
  const { mutate: updateExperience, isPending: isUpdating } = useUpdateExperience();

  // Form states
  const [title, setTitle] = useState(initialData?.title || '');
  const [companyName, setCompanyName] = useState(initialData?.companyName || '');
  const [location, setLocation] = useState(initialData?.location || '');
  const [employmentType, setEmploymentType] = useState(initialData?.employmentType || 'FULL_TIME');
  const [startDate, setStartDate] = useState(initialData?.startDate ? initialData.startDate.split('T')[0] : '');
  const [endDate, setEndDate] = useState(initialData?.endDate ? initialData.endDate.split('T')[0] : '');
  const [isCurrent, setIsCurrent] = useState(initialData?.isCurrent || false);
  const [description, setDescription] = useState(initialData?.description || '');

  // Validation errors
  const [errors, setErrors] = useState<{ title?: string; companyName?: string; startDate?: string }>({});

  // Unsaved changes tracking
  const [showConfirmCancel, setShowConfirmCancel] = useState(false);

  const isDirty = useMemo(() => {
    if (!initialData) {
      return (
        title !== '' ||
        companyName !== '' ||
        location !== '' ||
        employmentType !== 'FULL_TIME' ||
        startDate !== '' ||
        endDate !== '' ||
        isCurrent !== false ||
        description !== ''
      );
    }
    return (
      title !== (initialData.title || '') ||
      companyName !== (initialData.companyName || '') ||
      location !== (initialData.location || '') ||
      employmentType !== (initialData.employmentType || 'FULL_TIME') ||
      startDate !== (initialData.startDate ? initialData.startDate.split('T')[0] : '') ||
      endDate !== (initialData.endDate ? initialData.endDate.split('T')[0] : '') ||
      isCurrent !== (initialData.isCurrent || false) ||
      description !== (initialData.description || '')
    );
  }, [title, companyName, location, employmentType, startDate, endDate, isCurrent, description, initialData]);

  const handleCancelClick = () => {
    if (isDirty) {
      setShowConfirmCancel(true);
    } else {
      onCancel();
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: { title?: string; companyName?: string; startDate?: string } = {};
    if (!title.trim()) newErrors.title = 'Job Title is required';
    if (!companyName.trim()) newErrors.companyName = 'Company Name is required';
    if (!startDate) newErrors.startDate = 'Start Date is required';

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setErrors({});

    const payload: ExperienceUpsertRequest = {
      title,
      companyName,
      location: location || null,
      employmentType: employmentType || null,
      startDate: startDate || null,
      endDate: isCurrent ? null : endDate || null,
      isCurrent,
      description: description || null,
    };

    if (initialData) {
      updateExperience(
        { id: initialData.id, payload },
        {
          onSuccess: () => onSuccess(),
          onError: (error: any) => {
            toast.error(error?.response?.data?.message || 'Failed to update work experience. Please try again.');
          }
        }
      );
    } else {
      createExperience(payload, {
        onSuccess: () => onSuccess(),
        onError: (error: any) => {
          toast.error(error?.response?.data?.message || 'Failed to save work experience. Please try again.');
        }
      });
    }
  };

  return (
    <Card className="border border-primary/20 bg-primary/5 rounded-xl overflow-hidden shadow-sm animate-in slide-in-from-top-4 duration-300">
      <CardContent className="p-5 sm:p-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="mb-4">
            <h3 className="text-lg font-bold text-foreground">
              {initialData ? 'Edit Work Experience' : 'Add Work Experience'}
            </h3>
            <p className="text-xs text-muted-foreground">Fill in the details of your job position below</p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="title" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Job Title *</Label>
              <Input
                id="title"
                placeholder="e.g. Senior Frontend Developer"
                value={title}
                autoFocus
                onChange={(e) => {
                  setTitle(e.target.value);
                  if (errors.title) setErrors((prev) => ({ ...prev, title: undefined }));
                }}
                className={`bg-background/80 focus-visible:ring-primary/30 ${errors.title ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
                required
              />
              {errors.title && <p className="text-xs text-destructive mt-1 font-medium">{errors.title}</p>}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="companyName" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Company Name *</Label>
              <Input
                id="companyName"
                placeholder="e.g. Stripe"
                value={companyName}
                onChange={(e) => {
                  setCompanyName(e.target.value);
                  if (errors.companyName) setErrors((prev) => ({ ...prev, companyName: undefined }));
                }}
                className={`bg-background/80 focus-visible:ring-primary/30 ${errors.companyName ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
                required
              />
              {errors.companyName && <p className="text-xs text-destructive mt-1 font-medium">{errors.companyName}</p>}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="employmentType" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Employment Type</Label>
              <Select value={employmentType} onValueChange={setEmploymentType}>
                <SelectTrigger id="employmentType" className="w-full bg-background/80 border-border/60 focus:ring-primary/30">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="FULL_TIME">Full-time</SelectItem>
                  <SelectItem value="PART_TIME">Part-time</SelectItem>
                  <SelectItem value="CONTRACT">Contract</SelectItem>
                  <SelectItem value="FREELANCE">Freelance</SelectItem>
                  <SelectItem value="INTERNSHIP">Internship</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="location" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Location</Label>
              <Input
                id="location"
                placeholder="e.g. San Francisco, CA (or Remote)"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                className="bg-background/80 border-border/60 focus-visible:ring-primary/30"
              />
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Start Date *</Label>
              <div className="flex gap-2">
                <div className="w-1/2">
                  <Select
                    value={parseDateString(startDate).month}
                    onValueChange={(val) => {
                      setStartDate(buildDateString(parseDateString(startDate).year, val));
                      if (errors.startDate) setErrors((prev) => ({ ...prev, startDate: undefined }));
                    }}
                  >
                    <SelectTrigger className={`w-full bg-background/80 focus:ring-primary/30 ${errors.startDate ? 'border-destructive focus:ring-destructive' : 'border-border/60'}`}>
                      <SelectValue placeholder="Month" />
                    </SelectTrigger>
                    <SelectContent>
                      {MONTHS.map(m => <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="w-1/2">
                  <Select
                    value={parseDateString(startDate).year}
                    onValueChange={(val) => {
                      setStartDate(buildDateString(val, parseDateString(startDate).month));
                      if (errors.startDate) setErrors((prev) => ({ ...prev, startDate: undefined }));
                    }}
                  >
                    <SelectTrigger className={`w-full bg-background/80 focus:ring-primary/30 ${errors.startDate ? 'border-destructive focus:ring-destructive' : 'border-border/60'}`}>
                      <SelectValue placeholder="Year" />
                    </SelectTrigger>
                    <SelectContent>
                      {YEARS.map(y => <SelectItem key={y} value={y.toString()}>{y}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              {errors.startDate && <p className="text-xs text-destructive mt-1 font-medium">{errors.startDate}</p>}
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">End Date</Label>
              {isCurrent ? (
                <div className="w-full h-10 bg-muted/40 border border-border/40 rounded-md flex items-center px-3 text-sm font-semibold text-muted-foreground/70 cursor-not-allowed select-none">
                  Present
                </div>
              ) : (
                <div className="flex gap-2">
                  <div className="w-1/2">
                    <Select
                      value={parseDateString(endDate).month}
                      onValueChange={(val) => setEndDate(buildDateString(parseDateString(endDate).year, val))}
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
                      value={parseDateString(endDate).year}
                      onValueChange={(val) => setEndDate(buildDateString(val, parseDateString(endDate).month))}
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
              )}
            </div>

            <div className="flex items-center gap-2.5 pt-2 sm:col-span-2">
              <Checkbox
                id="isCurrent"
                checked={isCurrent}
                onCheckedChange={(checked) => setIsCurrent(checked === true)}
                className="rounded-[4px] w-4 h-4 border-border/60 text-primary data-[state=checked]:bg-primary data-[state=checked]:border-primary transition-all"
              />
              <Label htmlFor="isCurrent" className="text-sm font-semibold select-none cursor-pointer">
                I am currently working in this role
              </Label>
            </div>

            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="description" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Description</Label>
              <Textarea
                id="description"
                placeholder="Describe your achievements, responsibilities, and key accomplishments..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={4}
                className="bg-background/80 border-border/60 focus-visible:ring-primary/30 resize-none"
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
