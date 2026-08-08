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
import { useTranslations } from 'next-intl';

interface SkillDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  skillToDelete: SkillDto | null;
  onSuccess: (message: string) => void;
  onError: (message: string) => void;
}

export function SkillDeleteDialog({ isOpen, onClose, skillToDelete, onSuccess, onError }: SkillDeleteDialogProps) {
  const t = useTranslations('AdminMasterData');
  const deleteSkillMutation = useDeleteSkill();

  const handleConfirm = () => {
    if (!skillToDelete) return;

    deleteSkillMutation.mutate(skillToDelete.id, {
      onSuccess: (res) => {
        if (res.success) {
          onSuccess(t('toastSkillDeleteSuccess', { name: skillToDelete.name }));
          onClose();
        } else {
          onError(res.message || t('toastSkillDeleteError'));
        }
      },
      onError: (err: any) => {
        onError(
          err.response?.data?.message || t('toastSkillDeleteConstraintError')
        );
        onClose();
      },
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader className="hidden">
            <DialogTitle>{t('deleteSkill')}</DialogTitle>
        </DialogHeader>
        <div className="flex items-start gap-3 pt-4">
          <div className="p-2 rounded-full bg-destructive/10 text-destructive shrink-0">
            <AlertTriangle size={24} />
          </div>
          <div className="space-y-1.5">
            <h3 className="text-base font-bold text-foreground">{t('skillDeleteConfirmTitle')}</h3>
            <p className="text-sm text-muted-foreground">
              {t('skillDeleteConfirmDesc').replace('{name}', skillToDelete?.name || '')}
            </p>
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-4">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
          >
            {t('cancelBtn')}
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={deleteSkillMutation.isPending}
            className="px-4 py-2 bg-destructive text-destructive-foreground hover:bg-destructive/90 font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
          >
            {deleteSkillMutation.isPending && <Loader2 size={14} className="animate-spin" />}
            <span>{t('skillDeleteBtn')}</span>
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
