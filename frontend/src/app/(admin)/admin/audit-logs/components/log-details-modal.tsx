'use client';

import React from 'react';
import { X, FileText, Database } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { AuditLogDto } from '@/types/audit-log.types';
import { SnapshotDiff } from './snapshot-diff';

interface LogDetailsModalProps {
  log: AuditLogDto | null;
  onClose: () => void;
  getOperationBadgeColor: (op: string | null) => string;
  getCategoryBadgeColor: (cat: string) => string;
}

export function LogDetailsModal({
  log,
  onClose,
  getOperationBadgeColor,
  getCategoryBadgeColor,
}: LogDetailsModalProps) {
  if (!log) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 animate-in fade-in duration-200">
      <div className="w-full max-w-4xl bg-card text-card-foreground border border-border rounded-xl flex flex-col max-h-[90vh] overflow-hidden">
        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <div className="flex items-center gap-2.5">
            <FileText className="h-5 w-5 text-muted-foreground" />
            <h3 className="text-sm font-bold text-foreground">Chi tiết bản ghi nhật ký kiểm toán</h3>
          </div>
          <Button
            variant="ghost"
            size="icon-sm"
            onClick={onClose}
            className="text-muted-foreground hover:text-foreground cursor-pointer"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Modal Body */}
        <div className="p-6 overflow-y-auto space-y-6 flex-1 text-sm">
          {/* Metadata Cards Grid */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Col 1 */}
            <div className="bg-muted/30 p-4 rounded-lg border border-border/60 space-y-2">
              <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                Tác nhân (Actor)
              </div>
              <div>
                <div className="text-xs font-semibold text-foreground">{log.actorEmail}</div>
                <div className="text-[10px] text-muted-foreground font-bold uppercase mt-1 tracking-wider">
                  {log.actorRole}
                </div>
              </div>
            </div>

            {/* Col 2 */}
            <div className="bg-muted/30 p-4 rounded-lg border border-border/60 space-y-2">
              <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                Môi trường mạng
              </div>
              <div className="text-xs space-y-1">
                <div>
                  <span className="text-muted-foreground">IP:</span>{' '}
                  <span className="font-mono text-foreground">{log.ipAddress}</span>
                </div>
                <div className="truncate" title={log.userAgent}>
                  <span className="text-muted-foreground">UA:</span>{' '}
                  <span className="text-muted-foreground font-mono text-[10px]">
                    {log.userAgent}
                  </span>
                </div>
              </div>
            </div>

            {/* Col 3 */}
            <div className="bg-muted/30 p-4 rounded-lg border border-border/60 space-y-2">
              <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                Thời gian ghi nhận
              </div>
              <div className="text-xs text-foreground">
                <div>{new Date(log.createdAt).toLocaleString()}</div>
                <div className="text-[10px] text-muted-foreground font-mono mt-1">
                  UTC: {log.createdAt}
                </div>
              </div>
            </div>
          </div>

          {/* Action Description */}
          <div className="bg-muted/30 p-4 rounded-lg border border-border/60 space-y-2">
            <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
              Hành vi (Action)
            </div>
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-semibold text-foreground">{log.action}</p>
                <div className="flex items-center gap-2 mt-2">
                  <span
                    className={`px-2 py-0.5 rounded text-[9px] font-bold tracking-wider ${getCategoryBadgeColor(
                      log.actionCategory
                    )}`}
                  >
                    {log.actionCategory}
                  </span>
                  {log.tableName && (
                    <span className="font-mono text-[10px] bg-muted border border-border px-2 py-0.5 rounded text-muted-foreground">
                      Bảng: {log.tableName}
                    </span>
                  )}
                </div>
              </div>
              {log.operationType && (
                <span
                  className={`px-2.5 py-0.5 rounded text-[10px] font-bold tracking-wider ${getOperationBadgeColor(
                    log.operationType
                  )}`}
                >
                  {log.operationType}
                </span>
              )}
            </div>
          </div>

          {/* Payload Diff */}
          <div className="space-y-2">
            <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider flex items-center gap-1.5">
              <Database className="h-3.5 w-3.5 text-muted-foreground" />
              Ảnh chụp dữ liệu thay đổi (Payload Diff)
            </div>
            <SnapshotDiff diffStr={log.snapshotDiff} operation={log.operationType} />
          </div>
        </div>

        {/* Modal Footer */}
        <div className="flex items-center justify-end px-6 py-4 border-t border-border bg-muted/10">
          <Button variant="outline" size="default" onClick={onClose} className="cursor-pointer">
            Đóng
          </Button>
        </div>
      </div>
    </div>
  );
}
