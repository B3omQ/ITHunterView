'use client';

import React, { useState, useEffect } from 'react';
import { Loader2, XCircle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useCreateSkill, useUpdateSkill } from '@/hooks/useSkill';
import type { SkillDto, SkillStatus } from '@/types/master-data.types';

interface SkillModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: 'create' | 'edit';
  initialData: SkillDto | null;
  categories: { id: number; name: string }[];
  onSuccess: (message: string) => void;
}

const validateSkillForm = (name: string, categoryId: string): string | null => {
  if (!name.trim()) return 'Skill name cannot be empty.';
  if (name.trim().length < 2) return 'Skill name must be at least 2 characters.';
  if (!categoryId) return 'Skill category cannot be empty.';
  return null;
};

export function SkillModal({ isOpen, onClose, mode, initialData, categories, onSuccess }: SkillModalProps) {
  const [skillForm, setSkillForm] = useState({ name: '', categoryId: '', status: 'ACTIVE' as SkillStatus });
  const [skillFormError, setSkillFormError] = useState('');

  const createSkillMutation = useCreateSkill();
  const updateSkillMutation = useUpdateSkill();

  useEffect(() => {
    if (isOpen) {
      if (mode === 'edit' && initialData) {
        setSkillForm({
          name: initialData.name,
          categoryId: initialData.categoryId?.toString() || '',
          status: initialData.status,
        });
      } else {
        setSkillForm({
          name: '',
          categoryId: categories?.[0]?.id?.toString() || '',
          status: 'ACTIVE',
        });
      }
      setSkillFormError('');
    }
  }, [isOpen, mode, initialData, categories]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSkillFormError('');

    const validationError = validateSkillForm(skillForm.name, skillForm.categoryId);
    if (validationError) {
      setSkillFormError(validationError);
      return;
    }

    const categoryIdNum = parseInt(skillForm.categoryId, 10);

    if (mode === 'create') {
      createSkillMutation.mutate(
        {
          name: skillForm.name.trim(),
          categoryId: categoryIdNum,
          status: skillForm.status,
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              onSuccess('New skill added successfully!');
              onClose();
            } else {
              setSkillFormError(res.message || 'An error occurred.');
            }
          },
          onError: (err: any) => {
            setSkillFormError(err.response?.data?.message || 'An error occurred while creating skill.');
          },
        }
      );
    } else if (mode === 'edit' && initialData) {
      updateSkillMutation.mutate(
        {
          id: initialData.id,
          dto: {
            name: skillForm.name.trim(),
            categoryId: categoryIdNum,
            status: skillForm.status,
          },
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              onSuccess('Skill updated successfully!');
              onClose();
            } else {
              setSkillFormError(res.message || 'An error occurred.');
            }
          },
          onError: (err: any) => {
            setSkillFormError(err.response?.data?.message || 'An error occurred while updating.');
          },
        }
      );
    }
  };

  const isPending = createSkillMutation.isPending || updateSkillMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{mode === 'create' ? 'Add New Skill' : 'Edit Skill'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          {skillFormError && (
            <div className="p-3 bg-destructive/10 text-destructive text-xs font-medium rounded-xl border border-destructive/20 flex items-center gap-2">
              <XCircle size={16} className="shrink-0" />
              <span>{skillFormError}</span>
            </div>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Skill Name</label>
            <input
              type="text"
              placeholder="Enter skill name (e.g. React, .NET Core...)"
              value={skillForm.name}
              onChange={(e) => setSkillForm({ ...skillForm, name: e.target.value })}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60"
              required
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Category</label>
            <select
              value={skillForm.categoryId}
              onChange={(e) => setSkillForm({ ...skillForm, categoryId: e.target.value })}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
              required
            >
              <option value="" disabled>
                Select skill category
              </option>
              {categories?.map((cat) => (
                <option key={cat.id} value={cat.id}>
                  {cat.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Status</label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
                <input
                  type="radio"
                  name="skill-status"
                  checked={skillForm.status === 'ACTIVE'}
                  onChange={() => setSkillForm({ ...skillForm, status: 'ACTIVE' })}
                  className="accent-primary"
                />
                <span>Active</span>
              </label>
              <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
                <input
                  type="radio"
                  name="skill-status"
                  checked={skillForm.status === 'DEACTIVE'}
                  onChange={() => setSkillForm({ ...skillForm, status: 'DEACTIVE' })}
                  className="accent-primary"
                />
                <span>Disabled</span>
              </label>
            </div>
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
