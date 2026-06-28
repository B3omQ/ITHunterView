'use client';

import React, { useState, useEffect, useMemo } from 'react';
import { Loader2, XCircle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useCreateMajor, useUpdateMajor, useMajorTree } from '@/hooks/useMajor';
import type { MajorDto } from '@/types/master-data.types';

interface MajorModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: 'create' | 'edit';
  initialData: MajorDto | null;
  onSuccess: (message: string) => void;
}

interface DropdownMajorOption {
  id: number;
  name: string;
  code: string;
  level: number;
}

const validateMajorForm = (code: string, name: string): string | null => {
  const trimmedCode = code.trim();
  const trimmedName = name.trim();

  if (!trimmedCode) return 'Major code cannot be empty.';
  if (trimmedCode.length < 2) return 'Major code must be at least 2 characters.';
  if (trimmedCode.length > 50) return 'Major code cannot exceed 50 characters.';
  if (!/^[A-Za-z0-9\-_]+$/.test(trimmedCode)) {
    return 'Major code can only contain letters, numbers, hyphens, and underscores.';
  }

  if (!trimmedName) return 'Major name cannot be empty.';
  if (trimmedName.length < 3) return 'Major name must be at least 3 characters.';
  if (trimmedName.length > 255) return 'Major name cannot exceed 255 characters.';
  return null;
};

const generateSuggestedCode = (name: string): string => {
  return name
    .trim()
    .split(/\s+/)
    .map(word => word[0])
    .join('')
    .replace(/[^A-Za-z0-9]/g, '')
    .toUpperCase();
};

const getDropdownOptions = (
  items: MajorDto[],
  currentId?: number,
  level: number = 1
): DropdownMajorOption[] => {
  if (level >= 3) return []; // Chỉ cho phép Level 1 và 2 làm cha

  const options: DropdownMajorOption[] = [];

  for (const item of items) {
    if (currentId && item.id === currentId) {
      continue; // Chặn circular reference
    }

    options.push({
      id: item.id,
      name: item.name,
      code: item.code,
      level,
    });

    if (item.children && item.children.length > 0) {
      options.push(...getDropdownOptions(item.children, currentId, level + 1));
    }
  }

  return options;
};

export function MajorModal({ isOpen, onClose, mode, initialData, onSuccess }: MajorModalProps) {
  const [majorForm, setMajorForm] = useState({ name: '', code: '' });
  const [parentId, setParentId] = useState<number | null>(null);
  const [majorFormError, setMajorFormError] = useState('');

  const createMajorMutation = useCreateMajor();
  const updateMajorMutation = useUpdateMajor();

  // Fetch danh sách cây phục vụ dropdown chọn cha
  const { data: treeResponse, isLoading: isTreeLoading } = useMajorTree({ page: 1, pageSize: 9999 });
  const rootMajors = treeResponse?.data?.items || [];

  const dropdownOptions = useMemo(() => {
    return getDropdownOptions(rootMajors, initialData?.id);
  }, [rootMajors, initialData]);

  useEffect(() => {
    if (isOpen) {
      if (mode === 'edit' && initialData) {
        setMajorForm({ name: initialData.name, code: initialData.code });
        setParentId(initialData.parentId ?? null);
      } else {
        setMajorForm({ name: '', code: '' });
        setParentId(null);
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
          parentId: parentId,
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
            parentId: parentId,
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

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newName = e.target.value;
    setMajorForm(prev => {
      const oldSuggestedCode = generateSuggestedCode(prev.name);
      const shouldSuggest = mode === 'create' && (!prev.code || prev.code === oldSuggestedCode);
      const nextCode = shouldSuggest ? generateSuggestedCode(newName) : prev.code;
      return {
        name: newName,
        code: nextCode
      };
    });
  };

  const isPending = createMajorMutation.isPending || updateMajorMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{mode === 'create' ? 'Add New Specialization' : 'Edit Specialization'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          {majorFormError && (
            <div className="p-3 bg-destructive/10 text-destructive text-xs font-medium rounded-xl border border-destructive/20 flex flex-col gap-2">
              <div className="flex items-center gap-2">
                <XCircle size={16} className="shrink-0" />
                <span>{majorFormError}</span>
              </div>
              {(majorFormError.toLowerCase().includes('exists') || majorFormError.toLowerCase().includes('duplicate')) && (
                <p className="text-xs text-muted-foreground leading-relaxed mt-1">
                  Tip: Check if this major code or name was soft-deleted before. You can restore it via the Undo action if needed.
                </p>
              )}
            </div>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Major Name</label>
            <input
              type="text"
              placeholder="Enter full major name..."
              value={majorForm.name}
              onChange={handleNameChange}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60"
              required
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Major Code</label>
            <input
              type="text"
              placeholder="Enter major code (e.g. DEV, BA...)"
              value={majorForm.code}
              onChange={(e) => setMajorForm({ ...majorForm, code: e.target.value.toUpperCase() })}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60"
              required
              disabled={mode === 'edit'}
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Parent Specialization</label>
            <select
              value={parentId ?? ''}
              onChange={(e) => {
                const val = e.target.value;
                setParentId(val ? Number(val) : null);
              }}
              disabled={mode === 'edit' || isTreeLoading}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isTreeLoading ? (
                <option disabled>Loading parents...</option>
              ) : (
                <>
                  <option value="" className="text-muted-foreground">-- None (Root Level 1) --</option>
                  {dropdownOptions.map((opt) => (
                    <option key={opt.id} value={opt.id}>
                      {'\u00A0'.repeat((opt.level - 1) * 4)}
                      {opt.level === 2 ? '└─ ' : ''}
                      {opt.name} ({opt.code})
                    </option>
                  ))}
                </>
              )}
            </select>
            <p className="text-[10px] text-muted-foreground leading-relaxed mt-1">
              {mode === 'edit'
                ? '* Parent specialization cannot be changed after creation.'
                : '* Only level 1 or level 2 specializations can be selected as parents to ensure the hierarchy depth does not exceed 3 levels.'}
            </p>
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
              disabled={isPending || isTreeLoading}
              className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
            >
              {(isPending || isTreeLoading) && <Loader2 size={14} className="animate-spin" />}
              <span>Save Changes</span>
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
