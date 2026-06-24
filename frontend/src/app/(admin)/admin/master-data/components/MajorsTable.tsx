"use client";

import React, { memo } from "react";
import { Edit2, Trash2, ChevronLeft, ChevronRight } from "lucide-react";
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
            {majors.map((major) => (
              <tr
                key={major.id}
                className="hover:bg-muted/20 transition-colors"
              >
                <td className="px-6 py-4">
                  <span className="text-sm font-mono font-semibold bg-neutral-200/60 dark:bg-neutral-800 text-neutral-800 dark:text-neutral-200 px-2 py-1 rounded-md">
                    {major.code}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <span className="text-sm font-medium text-foreground">
                    {major.name}
                  </span>
                </td>
                <td className="px-6 py-4 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => onEdit(major)}
                      className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
                      title="Edit"
                    >
                      <Edit2 size={15} />
                    </button>
                    <button
                      onClick={() => onDelete(major)}
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
          {Math.min(currentPage * pageSize, totalItems)} of {totalItems} majors
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
});
