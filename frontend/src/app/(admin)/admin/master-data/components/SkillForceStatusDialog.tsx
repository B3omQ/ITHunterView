'use client';

import React from 'react';
import { Loader2, AlertTriangle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useUpdateSkillStatus } from '@/hooks/useSkill';
import type { SkillStatus } from '@/types/master-data.types';

interface SkillForceStatusDialogProps {
  isOpen: boolean;
  onClose: () => void;
  forceStatusData: { id: number; status: SkillStatus; message: string } | null;
  onSuccess: (message: string) => void;
  onError: (message: string) => void;
}

export function SkillForceStatusDialog({ isOpen, onClose, forceStatusData, onSuccess, onError }: SkillForceStatusDialogProps) {
  const updateSkillStatusMutation = useUpdateSkillStatus();

  const handleConfirm = () => {
    if (!forceStatusData) return;

    updateSkillStatusMutation.mutate(
      {
        id: forceStatusData.id,
        dto: {
          status: forceStatusData.status,
          force: true,
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            onSuccess(`Force updated skill status to ${forceStatusData.status} successfully!`);
            onClose();
          }
        },
        onError: (err: any) => {
          onError(err.response?.data?.message || 'Error force updating status.');
          onClose();
        },
      }
    );
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader className="hidden">
            <DialogTitle>Force Status Change Required</DialogTitle>
        </DialogHeader>
        <div className="flex items-start gap-3 pt-4">
          <div className="p-2 rounded-full bg-amber-500/10 text-amber-500 shrink-0">
            <AlertTriangle size={24} />
          </div>
          <div className="space-y-1.5">
            <h3 className="text-base font-bold text-foreground">Force Status Change Required</h3>
            <p className="text-sm text-muted-foreground leading-relaxed">
              {forceStatusData?.message}
            </p>
            <p className="text-xs text-muted-foreground italic">
              * When disabled, this skill will be hidden and cannot be searched by new candidates or job postings.
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
            disabled={updateSkillStatusMutation.isPending}
            className="px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
          >
            {updateSkillStatusMutation.isPending && <Loader2 size={14} className="animate-spin" />}
            <span>Agree to Disable</span>
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
