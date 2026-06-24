"use client";

import React, { memo } from "react";
import { Edit2, Trash2, ChevronLeft, ChevronRight, AlertTriangle } from "lucide-react";
import type { SkillDto } from "@/types/master-data.types";

interface SkillsTableProps {
  skills: SkillDto[];
  isLoading: boolean;
  isError: boolean;
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onEdit: (skill: SkillDto) => void;
  onDelete: (skill: SkillDto) => void;
  onStatusToggle: (skill: SkillDto) => void;
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

export const SkillsTable = memo(function SkillsTable({
  skills,
  isLoading,
  isError,
  totalItems,
  totalPages,
  currentPage,
  pageSize,
  onPageChange,
  onEdit,
  onDelete,
  onStatusToggle,
  onRetry,
}: SkillsTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={4} />;
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

  if (skills.length === 0) {
    return (
      <div className="text-center py-20 text-muted-foreground">
        <p className="text-sm">No matching skills found.</p>
      </div>
    );
  }

  return (
    <>
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                Skill
              </th>
              <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                Category
              </th>
              <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider text-center">
                Status
              </th>
              <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider text-right">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/60">
            {skills.map((skill) => (
              <tr
                key={skill.id}
                className="hover:bg-muted/20 transition-colors"
              >
                <td className="px-6 py-4">
                  <span className="text-sm font-medium text-foreground">
                    {skill.name}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-secondary text-secondary-foreground">
                    {skill.categoryName || "Uncategorized"}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <div className="flex items-center justify-center">
                    <label className="relative inline-flex items-center cursor-pointer">
                      <input
                        type="checkbox"
                        checked={skill.status === "ACTIVE"}
                        onChange={() => onStatusToggle(skill)}
                        className="sr-only peer"
                      />
                      <div className="w-9 h-5 bg-muted peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-border after:border after:rounded-full after:height-4 after:h-4 after:w-4 after:transition-all peer-checked:bg-primary"></div>
                    </label>
                  </div>
                </td>
                <td className="px-6 py-4 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => onEdit(skill)}
                      className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
                      title="Edit"
                    >
                      <Edit2 size={15} />
                    </button>
                    <button
                      onClick={() => onDelete(skill)}
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
          {Math.min(currentPage * pageSize, totalItems)} of {totalItems} skills
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
