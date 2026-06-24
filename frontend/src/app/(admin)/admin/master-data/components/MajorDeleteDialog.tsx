'use client';

import React from 'react';
import { Loader2, AlertTriangle } from 'lucide-react';
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

  const handleConfirm = () => {
    if (!majorToDelete) return;

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
          err.response?.data?.message || 'Cannot delete this major due to foreign key constraints.'
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
        <div className="flex items-start gap-3 pt-4">
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
            disabled={deleteMajorMutation.isPending}
            className="px-4 py-2 bg-destructive text-destructive-foreground hover:bg-destructive/90 font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
          >
            {deleteMajorMutation.isPending && <Loader2 size={14} className="animate-spin" />}
            <span>Confirm Delete</span>
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
