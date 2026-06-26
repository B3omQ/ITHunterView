'use client';

import React from 'react';
import { Loader2, AlertTriangle, XCircle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useDeleteMajor } from '@/hooks/useMajor';
import type { MajorDto } from '@/types/master-data.types';

interface MajorDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  majorToDelete: MajorDto | null;
  onSuccess: (message: string, deletedId: number, deletedName: string) => void;
  onError: (message: string) => void;
}

export function MajorDeleteDialog({ isOpen, onClose, majorToDelete, onSuccess, onError }: MajorDeleteDialogProps) {
  const deleteMajorMutation = useDeleteMajor();

  const activeChildren = majorToDelete?.children || [];
  const hasActiveChildren = activeChildren.length > 0;

  const handleConfirm = () => {
    if (!majorToDelete || hasActiveChildren) return;

    deleteMajorMutation.mutate(majorToDelete.id, {
      onSuccess: (res) => {
        if (res.success) {
          onSuccess(
            `Soft deleted major "${majorToDelete.name}".`,
            majorToDelete.id,
            majorToDelete.name
          );
          onClose();
        } else {
          onError(res.message || 'Error deleting major.');
        }
      },
      onError: (err: any) => {
        onError(
          err.response?.data?.message || 'Cannot delete this major due to active references or candidate enrollments.'
        );
        onClose();
      },
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader className="hidden">
          <DialogTitle>Delete Major</DialogTitle>
        </DialogHeader>

        {hasActiveChildren ? (
          // Cảnh báo cấm xóa khi có ngành con hoạt động
          <div className="space-y-4 pt-4">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-full bg-destructive/10 text-destructive shrink-0">
                <XCircle size={24} />
              </div>
              <div className="space-y-1.5">
                <h3 className="text-base font-bold text-foreground">Deletion Blocked</h3>
                <p className="text-sm text-muted-foreground">
                  You cannot delete major <strong className="text-foreground">"{majorToDelete?.name}"</strong> because it contains active sub-majors (children). The database enforces restrict deletion to keep parent-child structure valid.
                </p>
              </div>
            </div>

            <div className="p-3 bg-muted/40 rounded-xl border border-border/80 max-h-40 overflow-y-auto space-y-2">
              <p className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                Active Sub-Majors ({activeChildren.length})
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
                Close
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
                <h3 className="text-base font-bold text-foreground">Delete Major?</h3>
                <p className="text-sm text-muted-foreground">
                  This action will <strong className="text-foreground">soft-delete</strong> the major <strong className="text-foreground">"{majorToDelete?.name}"</strong>.
                  The system will keep the record to maintain data integrity for candidates registered in this major. You can restore it later.
                </p>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 border border-border bg-card hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleConfirm}
                disabled={deleteMajorMutation.isPending}
                className="px-4 py-2 bg-destructive text-destructive-foreground hover:bg-destructive/90 font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 cursor-pointer"
              >
                {deleteMajorMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                <span>Confirm Delete</span>
              </button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
