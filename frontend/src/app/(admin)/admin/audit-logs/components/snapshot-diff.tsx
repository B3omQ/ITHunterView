'use client';

import React from 'react';

interface SnapshotDiffProps {
  diffStr: string | null;
  operation: string | null;
}

export function SnapshotDiff({ diffStr, operation }: SnapshotDiffProps) {
  if (!diffStr) {
    return (
      <p className="text-muted-foreground italic text-xs">
        Không ghi nhận thay đổi cấu trúc/dữ liệu hoặc không có payload diff.
      </p>
    );
  }

  try {
    const parsed = JSON.parse(diffStr);
    if (parsed.error) {
      return <p className="text-destructive italic text-xs">{parsed.error}</p>;
    }

    if (parsed.changes) {
      const changeEntries = Object.entries(parsed.changes);
      if (changeEntries.length === 0) {
        return (
          <p className="text-muted-foreground italic text-xs">
            Không có trường nào bị sửa đổi giá trị.
          </p>
        );
      }
      return (
        <div className="overflow-hidden border border-border rounded-lg bg-card text-foreground">
          <table className="w-full text-left text-xs border-collapse">
            <thead className="bg-muted/80 text-muted-foreground uppercase text-[10px] tracking-wider font-semibold border-b border-border">
              <tr>
                <th className="p-2.5 border-r border-border">Trường dữ liệu</th>
                <th className="p-2.5 bg-rose-500/5 text-rose-700 dark:text-rose-400 border-r border-border">Giá trị cũ</th>
                <th className="p-2.5 bg-emerald-500/5 text-emerald-700 dark:text-emerald-400">Giá trị mới</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60 font-mono text-[11px]">
              {changeEntries.map(([field, diff]: [string, any]) => (
                <tr key={field} className="hover:bg-muted/10 transition-colors">
                  <td className="p-2.5 font-medium text-foreground border-r border-border/60">{field}</td>
                  <td className="p-2.5 bg-rose-500/[0.02] text-rose-600 dark:text-rose-400 line-through max-w-xs break-all border-r border-border/60">
                    {diff.old === null || diff.old === undefined ? (
                      <span className="text-muted-foreground italic">null</span>
                    ) : (
                      String(diff.old)
                    )}
                  </td>
                  <td className="p-2.5 bg-emerald-500/[0.02] text-emerald-600 dark:text-emerald-400 max-w-xs break-all">
                    {diff.new === null || diff.new === undefined ? (
                      <span className="text-muted-foreground italic">null</span>
                    ) : (
                      String(diff.new)
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    }

    if (parsed.values) {
      const valEntries = Object.entries(parsed.values);
      return (
        <div className="overflow-hidden border border-border rounded-lg bg-card text-foreground">
          <table className="w-full text-left text-xs border-collapse">
            <thead className="bg-muted/80 text-muted-foreground uppercase text-[10px] tracking-wider font-semibold border-b border-border">
              <tr>
                <th className="p-2.5 border-r border-border">Trường dữ liệu</th>
                <th className="p-2.5">Giá trị ghi nhận</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60 font-mono text-[11px]">
              {valEntries.map(([field, val]) => (
                <tr key={field} className="hover:bg-muted/10 transition-colors">
                  <td className="p-2.5 font-medium text-foreground border-r border-border/60">{field}</td>
                  <td
                    className={`p-2.5 max-w-lg break-all ${
                      operation === 'CREATE'
                        ? 'text-emerald-600 dark:text-emerald-400 bg-emerald-500/[0.02]'
                        : 'text-rose-600 dark:text-rose-400 bg-rose-500/[0.02] line-through'
                    }`}
                  >
                    {val === null || val === undefined ? (
                      <span className="text-muted-foreground italic">null</span>
                    ) : (
                      String(val)
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    }
  } catch (error) {
    // Fallback safe rendering if parse fails
  }

  return (
    <pre className="p-3 bg-muted border border-border rounded-lg font-mono text-xs text-foreground overflow-x-auto whitespace-pre-wrap max-h-60">
      {diffStr}
    </pre>
  );
}
