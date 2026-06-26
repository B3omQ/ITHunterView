"use client";

import React, { memo, useState, useMemo } from "react";
import { Edit2, Trash2, ChevronLeft, ChevronRight, ChevronDown } from "lucide-react";
import type { MajorDto } from "@/types/master-data.types";

interface MajorsTableProps {
  majors: MajorDto[];
  isLoading: boolean;
  isError: boolean;
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onEdit: (major: MajorDto) => void;
  onDelete: (major: MajorDto) => void;
  onRetry: () => void;
}

interface FlattenedMajor {
  item: MajorDto;
  level: number;
  hasChildren: boolean;
}

function TableSkeleton({ columns }: { columns: number }) {
  return (
    <div className="w-full">
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              {Array.from({ length: columns }).map((_, i) => (
                <th key={i} className="px-6 py-4">
                  <div className="h-4 bg-muted animate-pulse rounded-md w-24" />
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border/60">
            {Array.from({ length: 5 }).map((_, rowIndex) => (
              <tr key={rowIndex}>
                {Array.from({ length: columns }).map((_, colIndex) => (
                  <td key={colIndex} className="px-6 py-4">
                    <div
                      className={`h-5 bg-muted/70 animate-pulse rounded-md ${
                        colIndex === columns - 1 ? "ml-auto w-16" : "w-3/4"
                      }`}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export const MajorsTable = memo(function MajorsTable({
  majors,
  isLoading,
  isError,
  totalItems,
  totalPages,
  currentPage,
  pageSize,
  onPageChange,
  onEdit,
  onDelete,
  onRetry,
}: MajorsTableProps) {
  const [expandedIds, setExpandedIds] = useState<Set<number>>(new Set());

  const toggleExpand = (id: number, e: React.MouseEvent) => {
    e.stopPropagation();
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  // Hàm đệ quy làm phẳng cây
  const flattenedMajors = useMemo(() => {
    const flatten = (
      items: MajorDto[],
      level: number = 1
    ): FlattenedMajor[] => {
      const result: FlattenedMajor[] = [];
      for (const item of items) {
        const hasChildren = !!(item.children && item.children.length > 0);
        result.push({ item, level, hasChildren });
        if (hasChildren && expandedIds.has(item.id)) {
          result.push(...flatten(item.children || [], level + 1));
        }
      }
      return result;
    };
    return flatten(majors);
  }, [majors, expandedIds]);

  const getLevelBadge = (level: number) => {
    switch (level) {
      case 1:
        return (
          <span className="text-[10px] font-semibold bg-blue-100 dark:bg-blue-950/40 text-blue-700 dark:text-blue-400 px-1.5 py-0.5 rounded">
            Level 1 (Root)
          </span>
        );
      case 2:
        return (
          <span className="text-[10px] font-semibold bg-purple-100 dark:bg-purple-950/40 text-purple-700 dark:text-purple-400 px-1.5 py-0.5 rounded">
            Level 2 (Category)
          </span>
        );
      case 3:
        return (
          <span className="text-[10px] font-semibold bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400 px-1.5 py-0.5 rounded">
            Level 3 (Major)
          </span>
        );
      default:
        return null;
    }
  };

  if (isLoading) {
    return <TableSkeleton columns={3} />;
  }

  if (isError) {
    return (
      <div className="text-center py-20 space-y-3">
        <p className="text-sm text-destructive font-medium">
          Error loading data from server.
        </p>
        <button
          onClick={onRetry}
          className="px-4 py-2 text-xs font-medium border border-border rounded-xl hover:bg-muted"
        >
          Retry
        </button>
      </div>
    );
  }

  if (majors.length === 0) {
    return (
      <div className="text-center py-20 text-muted-foreground">
        <p className="text-sm">No majors found.</p>
      </div>
    );
  }

  return (
    <>
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider w-1/4">
                Major Code
              </th>
              <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                Major Name
              </th>
              <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider text-right">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/60">
            {flattenedMajors.map(({ item, level, hasChildren }) => (
              <tr
                key={item.id}
                className="hover:bg-muted/20 transition-colors"
              >
                <td className="px-6 py-4">
                  <span className="text-sm font-mono font-semibold bg-neutral-200/60 dark:bg-neutral-800 text-neutral-800 dark:text-neutral-200 px-2 py-1 rounded-md">
                    {item.code}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <div
                    className="flex items-center gap-1.5"
                    style={{ paddingLeft: `${(level - 1) * 24}px` }}
                  >
                    {hasChildren ? (
                      <button
                        onClick={(e) => toggleExpand(item.id, e)}
                        className="p-1 rounded hover:bg-muted/80 text-muted-foreground hover:text-foreground transition-colors"
                      >
                        {expandedIds.has(item.id) ? (
                          <ChevronDown size={14} />
                        ) : (
                          <ChevronRight size={14} />
                        )}
                      </button>
                    ) : (
                      <div className="w-7" />
                    )}
                    <span className="text-sm font-medium text-foreground mr-2">
                      {item.name}
                    </span>
                    {getLevelBadge(level)}
                  </div>
                </td>
                <td className="px-6 py-4 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => onEdit(item)}
                      className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
                      title="Edit"
                    >
                      <Edit2 size={15} />
                    </button>
                    <button
                      onClick={() => onDelete(item)}
                      className="p-1.5 text-muted-foreground hover:text-destructive rounded-lg hover:bg-destructive/10 transition-colors"
                      title="Delete"
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="flex flex-col sm:flex-row items-center justify-between px-6 py-4 border-t border-border gap-4 bg-muted/10">
        <span className="text-xs text-muted-foreground">
          Showing {Math.min((currentPage - 1) * pageSize + 1, totalItems)} -{" "}
          {Math.min(currentPage * pageSize, totalItems)} of {totalItems} root majors
        </span>
        <div className="flex items-center gap-1.5">
          <button
            onClick={() => onPageChange(Math.max(1, currentPage - 1))}
            disabled={currentPage === 1}
            className="p-1.5 rounded-lg border border-border bg-card text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:pointer-events-none transition-colors"
          >
            <ChevronLeft size={16} />
          </button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
            <button
              key={p}
              onClick={() => onPageChange(p)}
              className={`px-3 py-1 rounded-lg text-xs font-medium border transition-colors ${
                currentPage === p
                  ? "bg-primary text-primary-foreground border-primary"
                  : "border-border bg-card text-muted-foreground hover:text-foreground"
              }`}
            >
              {p}
            </button>
          ))}
          <button
            onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
            disabled={currentPage === totalPages || totalPages === 0}
            className="p-1.5 rounded-lg border border-border bg-card text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:pointer-events-none transition-colors"
          >
            <ChevronRight size={16} />
          </button>
        </div>
      </div>
    </>
  );
})
