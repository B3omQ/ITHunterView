'use client';

import React, { useState, useMemo } from 'react';
import {
  useCreateEducation,
  useUpdateEducation,
  useMajors,
} from '@/hooks/useCandidateProfile';
import type { CandidateEducation, EducationUpsertRequest } from '@/types/candidate.types';
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
const YEARS = Array.from({ length: 50 }, (_, i) => CURRENT_YEAR - i + 5); // +5 for future graduations

const parseDateString = (dateStr: string | null | undefined) => {
  if (!dateStr) return { month: '', year: '' };
  const parts = dateStr.split('T')[0].split('-');
  return { year: parts[0] || '', month: parts[1] || '' };
};

const buildDateString = (year: string, month: string) => {
  if (!year && !month) return '';
  return `${year || CURRENT_YEAR}-${month || '01'}-01`;
};

interface EducationFormProps {
  initialData?: CandidateEducation | null;
  onCancel: () => void;
  onSuccess: () => void;
}

export function EducationForm({ initialData, onCancel, onSuccess }: EducationFormProps) {
  const { data: majors } = useMajors();
  const { mutate: createEducation, isPending: isCreating } = useCreateEducation();
  const { mutate: updateEducation, isPending: isUpdating } = useUpdateEducation();

  // Form states
  const [degree, setDegree] = useState(initialData?.degree || '');
  const [institutionName, setInstitutionName] = useState(initialData?.institutionName || '');
  const [majorId, setMajorId] = useState<string>(initialData?.majorId ? String(initialData.majorId) : '');
  const [gpa, setGpa] = useState(initialData?.gpa !== null && initialData?.gpa !== undefined ? String(initialData.gpa) : '');
  const [maxGpa, setMaxGpa] = useState(initialData?.maxGpa !== null && initialData?.maxGpa !== undefined ? String(initialData.maxGpa) : '4.0');
  const [startDate, setStartDate] = useState(initialData?.startDate ? initialData.startDate.split('T')[0] : '');
  const [endDate, setEndDate] = useState(initialData?.endDate ? initialData.endDate.split('T')[0] : '');
  const [description, setDescription] = useState(initialData?.description || '');

  // Validation errors
  const [errors, setErrors] = useState<{ degree?: string; institutionName?: string; gpa?: string }>({});

  // Unsaved changes tracking
  const [showConfirmCancel, setShowConfirmCancel] = useState(false);

  const isDirty = useMemo(() => {
    if (!initialData) {
      return (
        degree !== '' ||
        institutionName !== '' ||
        majorId !== '' ||
        gpa !== '' ||
        maxGpa !== '4.0' ||
        startDate !== '' ||
        endDate !== '' ||
        description !== ''
      );
    }
    return (
      degree !== (initialData.degree || '') ||
      institutionName !== (initialData.institutionName || '') ||
      majorId !== (initialData.majorId ? String(initialData.majorId) : '') ||
      gpa !== (initialData.gpa !== null && initialData.gpa !== undefined ? String(initialData.gpa) : '') ||
      maxGpa !== (initialData.maxGpa !== null && initialData.maxGpa !== undefined ? String(initialData.maxGpa) : '4.0') ||
      startDate !== (initialData.startDate ? initialData.startDate.split('T')[0] : '') ||
      endDate !== (initialData.endDate ? initialData.endDate.split('T')[0] : '') ||
      description !== (initialData.description || '')
    );
  }, [degree, institutionName, majorId, gpa, maxGpa, startDate, endDate, description, initialData]);

  const handleCancelClick = () => {
    if (isDirty) {
      setShowConfirmCancel(true);
    } else {
      onCancel();
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: { degree?: string; institutionName?: string; gpa?: string } = {};
    if (!degree.trim()) newErrors.degree = 'Degree is required';
    if (!institutionName.trim()) newErrors.institutionName = 'Institution is required';
    
    if (gpa && maxGpa && parseFloat(gpa) > parseFloat(maxGpa)) {
      newErrors.gpa = 'GPA cannot be greater than Max GPA';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setErrors({});

    const payload: EducationUpsertRequest = {
      degree,
      institutionName,
      majorId: majorId ? Number(majorId) : null,
      gpa: gpa ? parseFloat(gpa) : null,
      maxGpa: maxGpa ? parseFloat(maxGpa) : null,
      startDate: startDate || null,
      endDate: endDate || null,
      description: description || null,
    };

    if (initialData) {
      updateEducation(
        { id: initialData.id, payload },
        {
          onSuccess: () => onSuccess(),
          onError: (error: any) => {
            toast.error(error?.response?.data?.message || 'Failed to update education. Please try again.');
          }
        }
      );
    } else {
      createEducation(payload, {
        onSuccess: () => onSuccess(),
        onError: (error: any) => {
          toast.error(error?.response?.data?.message || 'Failed to save education. Please try again.');
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
              {initialData ? 'Edit Education' : 'Add Education'}
            </h3>
            <p className="text-xs text-muted-foreground">Fill in the details of your academic background below</p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="institutionName" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Institution Name *</Label>
              <Input
                id="institutionName"
                placeholder="e.g. Stanford University"
                value={institutionName}
                autoFocus
                onChange={(e) => {
                  setInstitutionName(e.target.value);
                  if (errors.institutionName) setErrors((prev) => ({ ...prev, institutionName: undefined }));
                }}
                className={`bg-background/80 focus-visible:ring-primary/30 ${errors.institutionName ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
              />
              {errors.institutionName && <p className="text-xs text-destructive mt-1 font-medium">{errors.institutionName}</p>}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="degree" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Degree *</Label>
              <Input
                id="degree"
                placeholder="e.g. Bachelor of Science"
                value={degree}
                onChange={(e) => {
                  setDegree(e.target.value);
                  if (errors.degree) setErrors((prev) => ({ ...prev, degree: undefined }));
                }}
                className={`bg-background/80 focus-visible:ring-primary/30 ${errors.degree ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
              />
              {errors.degree && <p className="text-xs text-destructive mt-1 font-medium">{errors.degree}</p>}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="majorId" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Field of Study</Label>
              <Select value={majorId} onValueChange={(val) => setMajorId(val || '')}>
                <SelectTrigger id="majorId" className="w-full bg-background/80 border-border/60 focus:ring-primary/30">
                  <SelectValue placeholder="Select a major" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">None / Other</SelectItem>
                  {majors?.map((major) => (
                    <SelectItem key={major.id} value={String(major.id)}>
                      {major.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Start Date</Label>
              <div className="flex gap-2">
                <div className="w-1/2">
                  <Select
                    value={parseDateString(startDate).month}
                    onValueChange={(val) => setStartDate(buildDateString(parseDateString(startDate).year || '', val || ''))}
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
                    value={parseDateString(startDate).year}
                    onValueChange={(val) => setStartDate(buildDateString(val || '', parseDateString(startDate).month || ''))}
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
              <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">End Date (or Expected)</Label>
              <div className="flex gap-2">
                <div className="w-1/2">
                  <Select
                    value={parseDateString(endDate).month}
                    onValueChange={(val) => setEndDate(buildDateString(parseDateString(endDate).year || '', val || ''))}
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
                    onValueChange={(val) => setEndDate(buildDateString(val || '', parseDateString(endDate).month || ''))}
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
              <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Grade / GPA</Label>
              <div className="flex items-center gap-3">
                <Input
                  type="number"
                  step="0.01"
                  placeholder="e.g. 3.8"
                  value={gpa}
                  onChange={(e) => {
                    setGpa(e.target.value);
                    if (errors.gpa) setErrors((prev) => ({ ...prev, gpa: undefined }));
                  }}
                  className={`w-24 bg-background/80 focus-visible:ring-primary/30 ${errors.gpa ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
                />
                <span className="text-muted-foreground font-medium">out of</span>
                <Input
                  type="number"
                  step="0.1"
                  placeholder="e.g. 4.0"
                  value={maxGpa}
                  onChange={(e) => {
                    setMaxGpa(e.target.value);
                    if (errors.gpa) setErrors((prev) => ({ ...prev, gpa: undefined }));
                  }}
                  className={`w-24 bg-background/80 border-border/60 focus-visible:ring-primary/30 ${errors.gpa ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
                />
              </div>
              {errors.gpa && <p className="text-xs text-destructive mt-1 font-medium">{errors.gpa}</p>}
            </div>

            <div className="space-y-1.5 sm:col-span-2">
              <Label htmlFor="description" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Description</Label>
              <Textarea
                id="description"
                placeholder="Activities and societies, coursework, achievements..."
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
