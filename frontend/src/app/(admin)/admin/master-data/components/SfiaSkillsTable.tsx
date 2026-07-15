"use client";

import React from "react";
import { Edit2, Trash2, AlertCircle } from "lucide-react";
import { SfiaSkillDto } from "@/types/master-data.types";
import { ChevronLeft, ChevronRight } from "lucide-react";

interface SfiaSkillsTableProps {
  skills: SfiaSkillDto[];
  isLoading: boolean;
  isError: boolean;
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onEdit: (skill: SfiaSkillDto) => void;
  onDelete: (skill: SfiaSkillDto) => void;
  onRetry: () => void;
}

export function SfiaSkillsTable({
  skills,
  isLoading,
  isError,
  totalPages,
  currentPage,
  onPageChange,
  onEdit,
  onDelete,
  onRetry,
}: SfiaSkillsTableProps) {
  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-muted-foreground">
        <div className="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin mb-4"></div>
        <p className="text-sm">Loading SFIA skills...</p>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-destructive space-y-4">
        <AlertCircle size={40} className="opacity-50" />
        <div className="text-center">
          <p className="text-base font-semibold">Failed to load skills</p>
          <p className="text-sm opacity-80 mt-1">There was a problem communicating with the server.</p>
        </div>
        <button
          onClick={onRetry}
          className="px-4 py-2 bg-destructive/10 text-destructive hover:bg-destructive/20 rounded-xl text-sm font-medium transition-colors"
        >
          Try Again
        </button>
      </div>
    );
  }

  if (!skills || skills.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-muted-foreground">
        <div className="w-16 h-16 bg-muted/50 rounded-full flex items-center justify-center mb-4">
          <AlertCircle size={24} className="opacity-50" />
        </div>
        <p className="text-base font-medium text-foreground">No SFIA skills found</p>
        <p className="text-sm mt-1">Try adjusting your search criteria or add a new skill.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex-1 overflow-auto">
        <table className="w-full text-sm text-left">
          <thead className="text-xs text-muted-foreground uppercase bg-muted/30 sticky top-0 z-10 backdrop-blur-sm">
            <tr>
              <th className="px-4 py-3 font-semibold">Code</th>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Category</th>
              <th className="px-4 py-3 font-semibold">Subcategory</th>
              <th className="px-4 py-3 font-semibold text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {skills.map((skill) => (
              <tr key={skill.id} className="hover:bg-muted/20 transition-colors group">
                <td className="px-4 py-3 font-medium text-foreground whitespace-nowrap">
                  {skill.skillCode}
                </td>
                <td className="px-4 py-3 text-foreground font-medium">
                  {skill.skillName}
                </td>
                <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                  {skill.category}
                </td>
                <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                  {skill.subcategory}
                </td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button
                      onClick={() => onEdit(skill)}
                      className="p-1.5 text-muted-foreground hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                      title="Edit Skill"
                    >
                      <Edit2 size={16} />
                    </button>
                    <button
                      onClick={() => onDelete(skill)}
                      className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors"
                      title="Delete Skill"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between px-4 py-3 border-t border-border bg-card">
          <span className="text-sm text-muted-foreground">
            Page <span className="font-medium text-foreground">{currentPage}</span> of{" "}
            <span className="font-medium text-foreground">{totalPages}</span>
          </span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => onPageChange(currentPage - 1)}
              disabled={currentPage === 1}
              className="p-1.5 rounded-lg border border-border hover:bg-muted text-foreground disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronLeft size={16} />
            </button>
            <button
              onClick={() => onPageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
              className="p-1.5 rounded-lg border border-border hover:bg-muted text-foreground disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
