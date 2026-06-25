'use client';

import React, { useState, useMemo } from 'react';
import {
  useCandidateExperiences,
  useCreateExperience,
  useUpdateExperience,
  useDeleteExperience,
} from '@/hooks/useCandidateProfile';
import type { CandidateExperience, ExperienceUpsertRequest } from '@/types/candidate.types';
import { PageLoader } from '@/components/shared/PageLoader';
import { ExperienceCard } from '@/components/shared/ExperienceCard';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Briefcase, Plus, AlertTriangle, Loader2 } from 'lucide-react';

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

const parseDateString = (dateStr: string) => {
  if (!dateStr) return { month: '', year: '' };
  const parts = dateStr.split('-');
  return { year: parts[0] || '', month: parts[1] || '' };
};

const buildDateString = (year: string, month: string) => {
  if (!year && !month) return '';
  return `${year || CURRENT_YEAR}-${month || '01'}-01`;
};

export function ExperienceTab() {
  const { data: experiences, isLoading, isError } = useCandidateExperiences();
  const { mutate: createExperience, isPending: isCreating } = useCreateExperience();
  const { mutate: updateExperience, isPending: isUpdating } = useUpdateExperience();
  const { mutate: deleteExperience, isPending: isDeleting } = useDeleteExperience();

  // Dialog states
  const [isOpen, setIsOpen] = useState(false);
  const [editingExp, setEditingExp] = useState<CandidateExperience | null>(null);

  // Confirm Delete states
  const [deleteId, setDeleteId] = useState<string | null>(null);

  // Form states
  const [title, setTitle] = useState('');
  const [companyName, setCompanyName] = useState('');
  const [location, setLocation] = useState('');
  const [employmentType, setEmploymentType] = useState('FULL_TIME');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [isCurrent, setIsCurrent] = useState(false);
  const [description, setDescription] = useState('');

  // Validation errors
  const [errors, setErrors] = useState<{ title?: string; companyName?: string }>({});

  // Unsaved changes tracking
  const [showConfirmCancel, setShowConfirmCancel] = useState(false);

  const isDirty = useMemo(() => {
    if (!editingExp) {
      // For new experience, dirty if any field has a value (other than defaults)
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
    // For editing, dirty if any field differs from the original
    return (
      title !== (editingExp.title || '') ||
      companyName !== (editingExp.companyName || '') ||
      location !== (editingExp.location || '') ||
      employmentType !== (editingExp.employmentType || 'FULL_TIME') ||
      startDate !== (editingExp.startDate ? editingExp.startDate.split('T')[0] : '') ||
      endDate !== (editingExp.endDate ? editingExp.endDate.split('T')[0] : '') ||
      isCurrent !== (editingExp.isCurrent || false) ||
      description !== (editingExp.description || '')
    );
  }, [title, companyName, location, employmentType, startDate, endDate, isCurrent, description, editingExp]);

  const openAddDialog = () => {
    setEditingExp(null);
    setTitle('');
    setCompanyName('');
    setLocation('');
    setEmploymentType('FULL_TIME');
    setStartDate('');
    setEndDate('');
    setIsCurrent(false);
    setDescription('');
    setErrors({});
    setIsOpen(true);
  };

  const openEditDialog = (exp: CandidateExperience) => {
    setEditingExp(exp);
    setTitle(exp.title || '');
    setCompanyName(exp.companyName || '');
    setLocation(exp.location || '');
    setEmploymentType(exp.employmentType || 'FULL_TIME');
    setStartDate(exp.startDate ? exp.startDate.split('T')[0] : '');
    setEndDate(exp.endDate ? exp.endDate.split('T')[0] : '');
    setIsCurrent(exp.isCurrent || false);
    setDescription(exp.description || '');
    setErrors({});
    setIsOpen(true);
  };

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      // Attempting to close
      if (isDirty) {
        setShowConfirmCancel(true);
      } else {
        setIsOpen(false);
      }
    } else {
      setIsOpen(true);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const newErrors: { title?: string; companyName?: string } = {};
    if (!title.trim()) newErrors.title = 'Job Title is required';
    if (!companyName.trim()) newErrors.companyName = 'Company Name is required';
    
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

    if (editingExp) {
      updateExperience(
        { id: editingExp.id, payload },
        {
          onSuccess: () => setIsOpen(false),
        }
      );
    } else {
      createExperience(payload, {
        onSuccess: () => setIsOpen(false),
      });
    }
  };

  const handleDelete = () => {
    if (deleteId) {
      deleteExperience(deleteId, {
        onSuccess: () => setDeleteId(null),
      });
    }
  };

  if (isLoading) {
    return <PageLoader message="Loading work experiences..." />;
  }

  if (isError || !experiences) {
    return (
      <div className="p-8 border rounded-xl bg-card text-center text-muted-foreground">
        Failed to load work experiences. Please try again.
      </div>
    );
  }

  // Sắp xếp: experiences có isCurrent trước, sau đó theo startDate giảm dần
  const sortedExperiences = [...experiences].sort((a, b) => {
    if (a.isCurrent && !b.isCurrent) return -1;
    if (!a.isCurrent && b.isCurrent) return 1;

    const dateA = a.startDate ? new Date(a.startDate).getTime() : 0;
    const dateB = b.startDate ? new Date(b.startDate).getTime() : 0;
    return dateB - dateA;
  });

  return (
    <div className="space-y-6">
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4 flex flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Briefcase className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Work Experience</CardTitle>
              <CardDescription className="text-xs">Your employment history and job details</CardDescription>
            </div>
          </div>
          <Button
            onClick={openAddDialog}
            className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-4 rounded-lg flex items-center gap-1.5 shadow-md shadow-primary/10"
          >
            <Plus className="w-4 h-4" />
            Add Experience
          </Button>
        </CardHeader>
        <CardContent className="p-6">
          {sortedExperiences.length === 0 ? (
            <div className="text-center py-12 border border-dashed border-border/30 rounded-xl bg-muted/10">
              <p className="text-sm text-muted-foreground italic mb-3">No work experiences added yet.</p>
              <Button onClick={openAddDialog} variant="outline" className="text-xs font-semibold rounded-lg">
                Add your first experience
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {sortedExperiences.map((exp) => (
                <ExperienceCard
                  key={exp.id}
                  experience={exp}
                  onEdit={openEditDialog}
                  onDelete={setDeleteId}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Add/Edit Dialog */}
      <Dialog open={isOpen} onOpenChange={handleOpenChange}>
        <DialogContent className="max-w-lg rounded-2xl border-border/40 backdrop-blur-lg">
          <form onSubmit={handleSubmit} className="space-y-4">
            <DialogHeader>
              <DialogTitle className="text-lg font-bold">
                {editingExp ? 'Edit Work Experience' : 'Add Work Experience'}
              </DialogTitle>
              <DialogDescription className="text-xs">
                Fill in the details of your job position below
              </DialogDescription>
            </DialogHeader>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="title" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Job Title *</Label>
                <Input
                  id="title"
                  placeholder="e.g. Senior Frontend Developer"
                  value={title}
                  onChange={(e) => {
                    setTitle(e.target.value);
                    if (errors.title) setErrors((prev) => ({ ...prev, title: undefined }));
                  }}
                  className={`bg-background/50 focus-visible:ring-primary/30 ${errors.title ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
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
                  className={`bg-background/50 focus-visible:ring-primary/30 ${errors.companyName ? 'border-destructive focus-visible:ring-destructive' : 'border-border/60'}`}
                  required
                />
                {errors.companyName && <p className="text-xs text-destructive mt-1 font-medium">{errors.companyName}</p>}
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="employmentType" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Employment Type</Label>
                <select
                  id="employmentType"
                  value={employmentType}
                  onChange={(e) => setEmploymentType(e.target.value)}
                  className="w-full rounded-lg border border-border/60 bg-background/50 px-3 py-2 text-sm shadow-sm transition-all focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30"
                >
                  <option value="FULL_TIME">Full-time</option>
                  <option value="PART_TIME">Part-time</option>
                  <option value="CONTRACT">Contract</option>
                  <option value="FREELANCE">Freelance</option>
                  <option value="INTERNSHIP">Internship</option>
                </select>
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="location" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Location</Label>
                <Input
                  id="location"
                  placeholder="e.g. San Francisco, CA (or Remote)"
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>

              <div className="space-y-1.5">
                <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Start Date</Label>
                <div className="flex gap-2">
                  <select
                    value={parseDateString(startDate).month}
                    onChange={(e) => setStartDate(buildDateString(parseDateString(startDate).year, e.target.value))}
                    className="w-1/2 rounded-lg border border-border/60 bg-background/50 px-3 py-2 text-sm shadow-sm transition-all focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30"
                  >
                    <option value="" disabled>Month</option>
                    {MONTHS.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
                  </select>
                  <select
                    value={parseDateString(startDate).year}
                    onChange={(e) => setStartDate(buildDateString(e.target.value, parseDateString(startDate).month))}
                    className="w-1/2 rounded-lg border border-border/60 bg-background/50 px-3 py-2 text-sm shadow-sm transition-all focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30"
                  >
                    <option value="" disabled>Year</option>
                    {YEARS.map(y => <option key={y} value={y.toString()}>{y}</option>)}
                  </select>
                </div>
              </div>

              <div className="space-y-1.5">
                <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">End Date</Label>
                <div className="flex gap-2">
                  <select
                    value={parseDateString(endDate).month}
                    onChange={(e) => setEndDate(buildDateString(parseDateString(endDate).year, e.target.value))}
                    disabled={isCurrent}
                    className="w-1/2 rounded-lg border border-border/60 bg-background/50 px-3 py-2 text-sm shadow-sm transition-all focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <option value="" disabled>Month</option>
                    {MONTHS.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
                  </select>
                  <select
                    value={parseDateString(endDate).year}
                    onChange={(e) => setEndDate(buildDateString(e.target.value, parseDateString(endDate).month))}
                    disabled={isCurrent}
                    className="w-1/2 rounded-lg border border-border/60 bg-background/50 px-3 py-2 text-sm shadow-sm transition-all focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <option value="" disabled>Year</option>
                    {YEARS.map(y => <option key={y} value={y.toString()}>{y}</option>)}
                  </select>
                </div>
              </div>

              <div className="flex items-center gap-2.5 pt-2 sm:col-span-2">
                <input
                  id="isCurrent"
                  type="checkbox"
                  checked={isCurrent}
                  onChange={(e) => setIsCurrent(e.target.checked)}
                  className="w-4 h-4 rounded border-border/60 text-primary focus:ring-primary/30"
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
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30 resize-none"
                />
              </div>
            </div>

            <DialogFooter className="pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => handleOpenChange(false)}
                className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={!isDirty || isCreating || isUpdating}
                className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-6 shadow-md shadow-primary/10 rounded-lg flex items-center gap-2"
              >
                {(isCreating || isUpdating) && <Loader2 className="w-4 h-4 animate-spin" />}
                {editingExp ? 'Update' : 'Add'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Delete Work Experience</DialogTitle>
            <DialogDescription className="text-xs">
              Are you sure you want to delete this work experience? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setDeleteId(null)}
              className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
            >
              Cancel
            </Button>
            <Button
              onClick={handleDelete}
              disabled={isDeleting}
              className="bg-destructive hover:bg-destructive/95 transition-all text-destructive-foreground font-semibold px-6 shadow-md shadow-destructive/10 rounded-lg flex items-center gap-2"
            >
              {isDeleting && <Loader2 className="w-4 h-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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
                setIsOpen(false);
              }}
              className="bg-destructive hover:bg-destructive/95 transition-all text-destructive-foreground font-semibold px-6 shadow-md shadow-destructive/10 rounded-lg"
            >
              Discard Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
