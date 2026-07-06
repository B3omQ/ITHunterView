'use client';

import React from 'react';
import { Loader2, AlertTriangle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useDeleteSkill } from '@/hooks/useSkill';
import type { SkillDto } from '@/types/master-data.types';

interface SkillDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  skillToDelete: SkillDto | null;
  onSuccess: (message: string) => void;
  onError: (message: string) => void;
}

export function SkillDeleteDialog({ isOpen, onClose, skillToDelete, onSuccess, onError }: SkillDeleteDialogProps) {
  const deleteSkillMutation = useDeleteSkill();

  const handleConfirm = () => {
    if (!skillToDelete) return;

    deleteSkillMutation.mutate(skillToDelete.id, {
      onSuccess: (res) => {
        if (res.success) {
          onSuccess('Skill deleted successfully!');
          onClose();
        } else {
          onError(res.message || 'Error deleting skill.');
        }
      },
      onError: (err: any) => {
        onError(
          err.response?.data?.message || 'Cannot delete this skill (it might be in use or bound by foreign key constraints).'
        );
        onClose();
      },
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader className="hidden">
            <DialogTitle>Delete Skill</DialogTitle>
        </DialogHeader>
        <div className="flex items-start gap-3 pt-4">
          <div className="p-2 rounded-full bg-destructive/10 text-destructive shrink-0">
            <AlertTriangle size={24} />
          </div>
          <div className="space-y-1.5">
            <h3 className="text-base font-bold text-foreground">Delete Skill?</h3>
            <p className="text-sm text-muted-foreground">
              This action will permanently delete the skill <strong className="text-foreground">"{skillToDelete?.name}"</strong> from the database.
              If this skill is linked to other entities, the system will block this action.
            </p>
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-4">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={deleteSkillMutation.isPending}
            className="px-4 py-2 bg-destructive text-destructive-foreground hover:bg-destructive/90 font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
          >
            {deleteSkillMutation.isPending && <Loader2 size={14} className="animate-spin" />}
            <span>Confirm Delete</span>
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
