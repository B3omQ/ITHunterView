'use client';

import React from 'react';
import { X, AlertTriangle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface PurgeModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (days: number) => void;
  isPending: boolean;
  purgeDays: number;
  setPurgeDays: (days: number) => void;
  getPastDateStr: (daysAgo: number) => string;
}

export function PurgeModal({
  isOpen,
  onClose,
  onSubmit,
  isPending,
  purgeDays,
  setPurgeDays,
  getPastDateStr,
}: PurgeModalProps) {
  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(purgeDays);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 animate-in fade-in duration-200">
      <div className="w-full max-w-md bg-card text-card-foreground border border-border rounded-xl overflow-hidden flex flex-col">
        <form onSubmit={handleSubmit}>
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-border">
            <div className="flex items-center gap-2.5 text-destructive">
              <AlertTriangle className="h-5 w-5" />
              <h3 className="text-sm font-bold text-foreground">Xác nhận dọn dẹp logs</h3>
            </div>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              onClick={onClose}
              className="text-muted-foreground hover:text-foreground cursor-pointer"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>

          {/* Body */}
          <div className="p-6 space-y-4 text-xs">
            <div className="p-3 bg-destructive/10 border border-destructive/20 text-destructive rounded-lg leading-relaxed font-medium">
              <strong className="text-destructive block mb-1">CẢNH BÁO QUAN TRỌNG:</strong>
              Hành động này sẽ xoá hoàn toàn các bản ghi logs kiểm toán cũ hơn số ngày quy định. Dữ
              liệu logs sau khi bị xoá sẽ không thể khôi phục lại.
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="purge-days-input" className="text-xs text-muted-foreground font-medium">
                Giữ lại logs trong phạm vi (ngày):
              </Label>
              <Input
                id="purge-days-input"
                type="number"
                min="1"
                max="365"
                value={purgeDays || ''}
                onChange={(e) => setPurgeDays(parseInt(e.target.value) || 0)}
                className="w-full cursor-text"
              />
              <p className="text-[10px] text-muted-foreground mt-1">
                Các logs có thời gian tạo trước ngày {getPastDateStr(purgeDays)} sẽ bị xoá vĩnh viễn.
              </p>
            </div>
          </div>

          {/* Footer */}
          <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-border bg-muted/10">
            <Button type="button" variant="outline" size="default" onClick={onClose} className="cursor-pointer">
              Huỷ bỏ
            </Button>
            <Button
              type="submit"
              variant="destructive"
              size="default"
              disabled={isPending}
              className="font-semibold cursor-pointer"
            >
              {isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Thực hiện xoá
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
