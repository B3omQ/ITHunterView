'use client';

import React, { useState, useMemo } from 'react';
import {
  useCreateExperience,
  useUpdateExperience,
} from '@/hooks/useCandidateProfile';
import type { CandidateExperience, ExperienceUpsertRequest } from '@/types/candidate.types';
import { cn } from '@/lib/utils';
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
import { VIETNAM_PROVINCES } from '@/lib/job-constants';
import { LocationCombobox } from '@/components/shared/LocationCombobox';
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
  const [locationType, setLocationType] = useState(() => {
    const standardLocations = [...VIETNAM_PROVINCES];
    if (initialData?.location) {
      if (standardLocations.includes(initialData.location)) {
        return initialData.location;
      }
      return "Other";
    }
    return "TP Hồ Chí Minh";
  });
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
    <div className="w-full mt-2">
        <form onSubmit={handleSubmit} className="space-y-4">

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className={cn("sm:col-span-2 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm", errors.title && "border-destructive focus-within:border-destructive focus-within:ring-destructive/30")}>
              <Label htmlFor="title" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Job Title <span className="text-destructive">*</span></Label>
              <input
                id="title"
                placeholder="e.g. Senior Frontend Developer"
                value={title}
                autoFocus
                onChange={(e) => {
                  setTitle(e.target.value);
                  if (errors.title) setErrors((prev) => ({ ...prev, title: undefined }));
                }}
                className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
                required
              />
              {errors.title && <p className="text-[10px] text-destructive mt-1 font-medium">{errors.title}</p>}
            </div>

            <div className={cn("border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm", errors.companyName && "border-destructive focus-within:border-destructive focus-within:ring-destructive/30")}>
              <Label htmlFor="companyName" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Company Name <span className="text-destructive">*</span></Label>
              <input
                id="companyName"
                placeholder="e.g. Stripe"
                value={companyName}
                onChange={(e) => {
                  setCompanyName(e.target.value);
                  if (errors.companyName) setErrors((prev) => ({ ...prev, companyName: undefined }));
                }}
                className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
                required
              />
              {errors.companyName && <p className="text-[10px] text-destructive mt-1 font-medium">{errors.companyName}</p>}
            </div>

            <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
              <Label htmlFor="employmentType" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Employment Type</Label>
              <Select value={employmentType} onValueChange={(val) => setEmploymentType(val || '')}>
                <SelectTrigger id="employmentType" className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
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

            <div className="sm:col-span-2 flex gap-4">
              <div className="flex-1 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                <Label htmlFor="locationType" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Location</Label>
                <LocationCombobox
                  value={locationType}
                  onChange={(val) => {
                    setLocationType(val)
                    if (val !== "Other") {
                      setLocation(val)
                    } else {
                      setLocation("")
                    }
                  }}
                  className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none outline-none focus:ring-0 text-sm font-medium justify-between"
                />
              </div>
              
              {locationType === "Other" && (
                <div className="flex-1 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                  <Label htmlFor="location" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Specific address</Label>
                  <input
                    id="location"
                    placeholder="e.g. Can Tho, Binh Duong"
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                    className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
                  />
                </div>
              )}
            </div>

            <div className={cn("border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm", errors.startDate && "border-destructive focus-within:border-destructive focus-within:ring-destructive/30")}>
              <Label className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Start Date <span className="text-destructive">*</span></Label>
              <div className="flex gap-2">
                <div className="w-1/2 border-r border-border/50 pr-2">
                  <Select
                    value={parseDateString(startDate).month}
                    onValueChange={(val) => {
                      setStartDate(buildDateString(parseDateString(startDate).year || '', val || ''));
                      if (errors.startDate) setErrors((prev) => ({ ...prev, startDate: undefined }));
                    }}
                  >
                    <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
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
                      setStartDate(buildDateString(val || '', parseDateString(startDate).month || ''));
                      if (errors.startDate) setErrors((prev) => ({ ...prev, startDate: undefined }));
                    }}
                  >
                    <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
                      <SelectValue placeholder="Year" />
                    </SelectTrigger>
                    <SelectContent>
                      {YEARS.map(y => <SelectItem key={y} value={y.toString()}>{y}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              {errors.startDate && <p className="text-[10px] text-destructive mt-1 font-medium">{errors.startDate}</p>}
            </div>

            <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
              <Label className="text-[11px] text-muted-foreground font-semibold block mb-0.5">End Date</Label>
              {isCurrent ? (
                <div className="w-full bg-transparent p-0 text-sm font-medium text-foreground/70 cursor-not-allowed select-none">
                  Present
                </div>
              ) : (
                <div className="flex gap-2">
                  <div className="w-1/2 border-r border-border/50 pr-2">
                    <Select
                      value={parseDateString(endDate).month}
                      onValueChange={(val) => setEndDate(buildDateString(parseDateString(endDate).year || '', val || ''))}
                    >
                      <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
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
                      onValueChange={(val) => setEndDate(buildDateString(val || '', parseDateString(endDate).month || ''))}
                    >
                      <SelectTrigger className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none focus:ring-0 text-sm font-medium">
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

            <div className="flex items-center gap-2.5 sm:col-span-2 ml-1">
              <Checkbox
                id="isCurrent"
                checked={isCurrent}
                onCheckedChange={(checked) => setIsCurrent(checked === true)}
              />
              <Label htmlFor="isCurrent" className="text-sm font-medium cursor-pointer">
                I am currently working in this role
              </Label>
            </div>

            <div className="sm:col-span-2 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
              <Label htmlFor="description" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Description</Label>
              <textarea
                id="description"
                placeholder="Describe your achievements, responsibilities, and key accomplishments..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={4}
                className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50 resize-none min-h-[80px]"
              />
            </div>
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t mt-6">
            <Button
              type="button"
              variant="ghost"
              onClick={handleCancelClick}
              disabled={isCreating || isUpdating}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={!isDirty || isCreating || isUpdating}
              className="px-6 flex items-center gap-2"
            >
              {(isCreating || isUpdating) && <Loader2 className="w-4 h-4 animate-spin" />}
              Save
            </Button>
          </div>
        </form>

      {/* Cancel Confirmation Dialog */}
      <Dialog open={showConfirmCancel} onOpenChange={setShowConfirmCancel}>
        <DialogContent className="max-w-md z-[60]">
          <DialogHeader>
            <div className="w-12 h-12 rounded-md bg-muted text-muted-foreground flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Discard Unsaved Changes?</DialogTitle>
            <DialogDescription className="text-sm">
              You have unsaved changes. Are you sure you want to discard them? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setShowConfirmCancel(false)}
            >
              Continue Editing
            </Button>
            <Button
              onClick={() => {
                setShowConfirmCancel(false);
                onCancel();
              }}
              variant="destructive"
            >
              Discard Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
