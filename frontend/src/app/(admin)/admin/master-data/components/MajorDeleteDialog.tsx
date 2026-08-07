'use client';

import React from 'react';
import { Loader2, AlertTriangle, XCircle, Trash2 } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useDeleteMajor } from '@/hooks/useMajor';
import type { MajorDto } from '@/types/master-data.types';
import { useTranslations } from 'next-intl';

interface MajorDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  majorToDelete: MajorDto | null;
  onSuccess: (message: string, deletedId: number, deletedName: string) => void;
  onError: (message: string) => void;
}

export function MajorDeleteDialog({ isOpen, onClose, majorToDelete, onSuccess, onError }: MajorDeleteDialogProps) {
  const t = useTranslations('AdminMasterData');
  const deleteMajorMutation = useDeleteMajor();

  const activeChildren = majorToDelete?.children || [];
  const hasActiveChildren = activeChildren.length > 0;

  const handleConfirm = () => {
    if (!majorToDelete || hasActiveChildren) return;

    deleteMajorMutation.mutate(majorToDelete.id, {
      onSuccess: (res) => {
        if (res.success) {
          onSuccess(
            t('toastMajorDeleteSuccess').replace('{name}', majorToDelete.name),
            majorToDelete.id,
            majorToDelete.name
          );
          onClose();
        } else {
          onError(res.message || 'Error deleting specialization.');
        }
      },
      onError: (err: any) => {
        onError(
          err.response?.data?.message || t('toastMajorDeleteError')
        );
        onClose();
      },
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader className="hidden">
          <DialogTitle>{t('deleteMajor')}</DialogTitle>
        </DialogHeader>

        {hasActiveChildren ? (
          // Cảnh báo cấm xóa khi có ngành con hoạt động
          <div className="space-y-4 pt-4">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-full bg-destructive/10 text-destructive shrink-0">
                <XCircle size={24} />
              </div>
              <div className="space-y-1.5">
                <h3 className="text-base font-bold text-foreground">{t('majorDeleteBlockedTitle')}</h3>
                <p className="text-sm text-muted-foreground">
                  {t('majorDeleteBlockedDesc').replace('{name}', majorToDelete?.name || '')}
                </p>
              </div>
            </div>

            <div className="p-3 bg-muted/40 rounded-xl border border-border/80 max-h-40 overflow-y-auto space-y-2">
              <p className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                {t('activeSubSpecializations').replace('{count}', activeChildren.length.toString())}
              </p>
              <ul className="space-y-1.5">
                {activeChildren.map((child) => (
                  <li key={child.id} className="text-xs font-medium text-foreground flex items-center gap-2">
                    <span className="w-1.5 h-1.5 rounded-full bg-destructive shrink-0" />
                    <span className="font-mono bg-neutral-200 dark:bg-neutral-800 text-[10px] px-1 rounded-sm">
                      {child.code}
                    </span>
                    <span>{child.name}</span>
                  </li>
                ))}
              </ul>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 border border-border bg-card hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors cursor-pointer"
              >
                {t('cancelBtn')}
              </button>
              <button
                type="button"
                disabled
                className="px-4 py-2 bg-muted text-muted-foreground border border-border font-medium text-sm rounded-xl transition-colors cursor-not-allowed flex items-center gap-1.5"
              >
                <span>Deletion Blocked</span>
              </button>
            </div>
          </div>
        ) : (
          // Cảnh báo soft-delete bình thường
          <div className="space-y-4 pt-4">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-full bg-destructive/10 text-destructive shrink-0">
                <AlertTriangle size={24} />
              </div>
              <div className="space-y-1.5">
                <h3 className="text-base font-bold text-foreground">{t('deleteMajorConfirmTitle')}</h3>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  {t('deleteMajorConfirmDesc').replace('{name}', majorToDelete?.name || '')}
                </p>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 border border-border bg-card hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors cursor-pointer"
              >
                {t('cancelBtn')}
              </button>
              <button
                type="button"
                onClick={handleConfirm}
                disabled={deleteMajorMutation.isPending}
                className="px-4 py-2 bg-destructive hover:bg-destructive/90 text-destructive-foreground font-medium text-sm rounded-xl transition-all shadow-sm flex items-center gap-2 active:scale-[0.98] disabled:opacity-50 disabled:active:scale-100 cursor-pointer"
              >
                {deleteMajorMutation.isPending ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                <span>{t('deleteMajorBtn')}</span>
              </button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
