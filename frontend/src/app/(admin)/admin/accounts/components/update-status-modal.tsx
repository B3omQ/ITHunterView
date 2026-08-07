import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Loader2, XCircle } from 'lucide-react';
import { useUpdateUserStatus } from '@/hooks/useUserGovernance';
import { UserStatus } from '@/types/user-governance.types';
import { useTranslations } from 'next-intl';

interface UpdateStatusModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  targetUser: { id: string; email: string; currentStatus: UserStatus } | null;
  onSuccess?: (message: string) => void;
}

export function UpdateStatusModal({ open, onOpenChange, targetUser, onSuccess }: UpdateStatusModalProps) {
  const t = useTranslations('AdminAccounts');
  const [newStatus, setNewStatus] = useState<UserStatus>('ACTIVE');
  const [statusReason, setStatusReason] = useState('');
  const [error, setError] = useState('');

  const updateStatusMutation = useUpdateUserStatus();

  useEffect(() => {
    if (open && targetUser) {
      setNewStatus(targetUser.currentStatus);
      setStatusReason('');
      setError('');
    }
  }, [open, targetUser]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetUser) return;
    if (!statusReason.trim()) {
      setError(t('updateStatusModal.reasonRequired'));
      return;
    }
    if (statusReason.trim().length < 5) {
      setError(t('updateStatusModal.reasonLength'));
      return;
    }

    setError('');
    updateStatusMutation.mutate(
      {
        id: targetUser.id,
        dto: {
          status: newStatus,
          reason: statusReason.trim(),
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            onSuccess?.(t('updateStatusModal.successMsg'));
            onOpenChange(false);
          } else {
            setError(res.message || t('updateStatusModal.failMsg'));
          }
        },
        onError: (err: any) => {
          setError(err.response?.data?.message || t('updateStatusModal.defaultErrorMsg'));
        },
      }
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t('updateStatusModal.title')}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
          <div className="space-y-1">
            <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">{t('updateStatusModal.accountLabel')}</label>
            <p className="text-sm font-mono font-semibold text-foreground bg-muted p-2 rounded-lg border border-border">
              {targetUser?.email}
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">{t('updateStatusModal.newStatusLabel')}</label>
            <select
              value={newStatus}
              onChange={(e) => setNewStatus(e.target.value as UserStatus)}
              className="w-full py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary transition-all"
            >
              <option value="ACTIVE">{t('updateStatusModal.activeOpt')}</option>
              <option value="INACTIVE">{t('updateStatusModal.inactiveOpt')}</option>
              <option value="BANNED">{t('updateStatusModal.bannedOpt')}</option>
              <option value="PENDING_VERIFICATION">{t('updateStatusModal.pendingOpt')}</option>
            </select>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">
              {t('updateStatusModal.reasonLabel')} <span className="text-rose-500">*</span>
            </label>
            <textarea
              placeholder={t('updateStatusModal.reasonPlaceholder')}
              value={statusReason}
              onChange={(e) => setStatusReason(e.target.value)}
              rows={3}
              className="w-full p-3 border border-border rounded-xl bg-background text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground resize-none"
            />
            <span className="text-xs text-muted-foreground">
              {t('updateStatusModal.reasonNote')}
            </span>
          </div>

          {error && (
            <div className="p-3 bg-rose-500/10 border border-rose-500/25 rounded-xl text-rose-500 text-xs font-semibold flex items-center gap-1.5">
              <XCircle size={14} className="shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              className="px-4 py-2 border border-border hover:bg-muted text-foreground font-semibold text-sm rounded-xl transition-colors"
            >
              {t('updateStatusModal.cancelBtn')}
            </button>
            <button
              type="submit"
              disabled={updateStatusMutation.isPending}
              className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
            >
              {updateStatusMutation.isPending && <Loader2 size={14} className="animate-spin" />}
              <span>{t('updateStatusModal.confirmBtn')}</span>
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
