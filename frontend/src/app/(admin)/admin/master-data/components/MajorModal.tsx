'use client';

import React, { useState, useEffect } from 'react';
import { Loader2, XCircle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useCreateMajor, useUpdateMajor } from '@/hooks/useMajor';
import type { MajorDto } from '@/types/master-data.types';

interface MajorModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: 'create' | 'edit';
  initialData: MajorDto | null;
  onSuccess: (message: string) => void;
}

const validateMajorForm = (code: string, name: string): string | null => {
  if (!code.trim()) return 'Major code cannot be empty.';
  if (!/^[A-Za-z0-9\-]+$/.test(code.trim())) {
    return 'Major code can only contain letters, numbers, and hyphens.';
  }
  if (!name.trim()) return 'Major name cannot be empty.';
  if (name.trim().length < 3) return 'Major name must be at least 3 characters.';
  return null;
};

export function MajorModal({ isOpen, onClose, mode, initialData, onSuccess }: MajorModalProps) {
  const [majorForm, setMajorForm] = useState({ name: '', code: '' });
  const [majorFormError, setMajorFormError] = useState('');

  const createMajorMutation = useCreateMajor();
  const updateMajorMutation = useUpdateMajor();

  useEffect(() => {
    if (isOpen) {
      if (mode === 'edit' && initialData) {
        setMajorForm({ name: initialData.name, code: initialData.code });
      } else {
        setMajorForm({ name: '', code: '' });
      }
      setMajorFormError('');
    }
  }, [isOpen, mode, initialData]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMajorFormError('');

    const validationError = validateMajorForm(majorForm.code, majorForm.name);
    if (validationError) {
      setMajorFormError(validationError);
      return;
    }

    if (mode === 'create') {
      createMajorMutation.mutate(
        {
          code: majorForm.code.trim().toUpperCase(),
          name: majorForm.name.trim(),
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              onSuccess('New major added successfully!');
              onClose();
            } else {
              setMajorFormError(res.message || 'An error occurred.');
            }
          },
          onError: (err: any) => {
            setMajorFormError(err.response?.data?.message || 'An error occurred while creating major.');
          },
        }
      );
    } else if (mode === 'edit' && initialData) {
      updateMajorMutation.mutate(
        {
          id: initialData.id,
          dto: {
            code: majorForm.code.trim().toUpperCase(),
            name: majorForm.name.trim(),
          },
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              onSuccess('Major updated successfully!');
              onClose();
            } else {
              setMajorFormError(res.message || 'An error occurred.');
            }
          },
          onError: (err: any) => {
            setMajorFormError(err.response?.data?.message || 'An error occurred while updating.');
          },
        }
      );
    }
  };

  const isPending = createMajorMutation.isPending || updateMajorMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{mode === 'create' ? 'Add New Major' : 'Edit Major'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          {majorFormError && (
            <div className="p-3 bg-destructive/10 text-destructive text-xs font-medium rounded-xl border border-destructive/20 flex flex-col gap-2">
              <div className="flex items-center gap-2">
                <XCircle size={16} className="shrink-0" />
                <span>{majorFormError}</span>
              </div>
              {(majorFormError.includes('đã tồn tại') || majorFormError.includes('trùng') || 
                majorFormError.toLowerCase().includes('exists') || majorFormError.toLowerCase().includes('duplicate')) && (
                <p className="text-xs text-muted-foreground leading-relaxed mt-1">
                  Tip: Check if this major code or name was soft-deleted before. You can restore it via the Undo action if needed.
                </p>
              )}
            </div>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Major Code</label>
            <input
              type="text"
              placeholder="Enter major code (e.g. CNTT, MKT...)"
              value={majorForm.code}
              onChange={(e) => setMajorForm({ ...majorForm, code: e.target.value })}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60 uppercase"
              required
              disabled={mode === 'edit'}
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Major Name</label>
            <input
              type="text"
              placeholder="Enter full major name..."
              value={majorForm.name}
              onChange={(e) => setMajorForm({ ...majorForm, name: e.target.value })}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60"
              required
            />
          </div>

          <div className="flex justify-end gap-3 pt-3 border-t border-border/80">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
            >
              {isPending && <Loader2 size={14} className="animate-spin" />}
              <span>Save Changes</span>
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
