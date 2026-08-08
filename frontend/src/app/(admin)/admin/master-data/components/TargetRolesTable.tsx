"use client";

import React, { memo } from "react";
import { Edit2, Trash2, ChevronLeft, ChevronRight, RotateCcw, SearchX } from "lucide-react";
import type { TargetRoleTemplateDto } from "@/types/master-data.types";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslations } from 'next-intl';

interface TargetRolesTableProps {
  roles: TargetRoleTemplateDto[];
  isLoading: boolean;
  isError: boolean;
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  onPageSizeChange?: (pageSize: number) => void;
  onPageChange: (page: number) => void;
  onEdit: (role: TargetRoleTemplateDto) => void;
  onDelete: (role: TargetRoleTemplateDto) => void;
  onRetry: () => void;
  isFilterActive?: boolean;
  onResetFilters?: () => void;
}

export const TargetRolesTable = memo(function TargetRolesTable({
  roles,
  isLoading,
  isError,
  totalItems,
  totalPages,
  currentPage,
  pageSize,
  onPageSizeChange,
  onPageChange,
  onEdit,
  onDelete,
  onRetry,
  isFilterActive,
  onResetFilters,
}: TargetRolesTableProps) {
  const t = useTranslations('AdminMasterData');
  const startResult = totalItems > 0 ? (currentPage - 1) * pageSize + 1 : 0;
  const endResult = Math.min(currentPage * pageSize, totalItems);

  return (
    <div className="space-y-4">
      {/* TẦNG 2: MAIN TABLE CONTAINER (TABLE_STANDARD - SHADCN TABLE) */}
      <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
        <Table className="w-full text-left border-collapse table-fixed">
          {/* Table Header */}
          <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
            <TableRow className="hover:bg-transparent border-none">
              <TableHead className="w-[52%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t('colRoleName')}
              </TableHead>

              <TableHead className="w-[40%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t('colTargetRoleSkills')}
              </TableHead>

              <TableHead className="w-[8%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t('colTargetRoleActions')}
              </TableHead>
            </TableRow>
          </TableHeader>

          {/* Table Body */}
          <TableBody>
            {isLoading ? (
              // Loading Skeleton State (6 rows)
              Array.from({ length: pageSize || 6 }).map((_, index) => (
                <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                  <TableCell className="py-3.5 px-3">
                    <Skeleton className="h-5 w-48 bg-slate-100 dark:bg-zinc-800 rounded-md mb-1.5" />
                    <Skeleton className="h-4 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                  </TableCell>
                  <TableCell className="py-3.5 px-3">
                    <Skeleton className="h-5 w-full bg-slate-100 dark:bg-zinc-800 rounded-md" />
                  </TableCell>
                  <TableCell className="py-3.5 px-2 text-center">
                    <Skeleton className="h-8 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
                  </TableCell>
                </TableRow>
              ))
            ) : isError ? (
              // Error State
              <TableRow>
                <TableCell colSpan={3} className="h-64 text-center">
                  <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                    <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">
                      Failed to load target roles
                    </p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      An error occurred while fetching target roles data. Please try again.
                    </p>
                    <Button
                      onClick={onRetry}
                      variant="outline"
                      className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                    >
                      <RotateCcw className="h-4 w-4 mr-2" /> Retry Loading
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ) : roles.length === 0 ? (
              // Empty State
              <TableRow>
                <TableCell colSpan={3} className="h-72 text-center">
                  <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                    <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                      <SearchX className="h-6 w-6" />
                    </div>
                    <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                      No target roles found
                    </p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      {isFilterActive
                        ? "No target roles match the current search query. Try clearing or adjusting your filter."
                        : "No target roles recorded yet."}
                    </p>
                    {isFilterActive && onResetFilters && (
                      <Button
                        onClick={onResetFilters}
                        variant="outline"
                        className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                      >
                        <RotateCcw className="h-4 w-4 mr-2" /> {t('clearFilters')}
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              // Actual Data Rows
              roles.map((role) => (
                <TableRow
                  key={role.id}
                  className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                >
                  {/* Role Name & Description */}
                  <TableCell className="py-3.5 px-3 align-middle">
                    <div className="flex flex-col gap-0.5">
                      <span className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors">
                        {role.roleName}
                      </span>
                      <span className="text-xs text-[#65676B] dark:text-zinc-400 truncate max-w-[420px]" title={role.description || undefined}>
                        {role.description || "No description provided"}
                      </span>
                    </div>
                  </TableCell>

                  {/* Required Skills */}
                  <TableCell className="py-3.5 px-3 align-middle">
                    <div className="flex items-center gap-2.5">
                      <Badge className="bg-[#E7F3FF] dark:bg-blue-950/50 text-[#1877F2] dark:text-blue-300 border border-blue-200 dark:border-blue-800/60 font-bold px-2 py-0.5 rounded-full text-xs shrink-0 shadow-none">
                        {role.requiredSkills.length} Skills
                      </Badge>
                      <span className="font-mono text-xs text-[#65676B] dark:text-zinc-400 truncate max-w-[320px]" title={role.requiredSkills.map((s) => s.skillCode).join(", ")}>
                        {role.requiredSkills.map((s) => s.skillCode).join(", ") || "None"}
                      </span>
                    </div>
                  </TableCell>

                  {/* Actions (Icon-only buttons) */}
                  <TableCell className="py-3.5 px-2 align-middle text-center">
                    <div className="flex items-center justify-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onEdit(role)}
                        className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                        title={t('targetRoleEdit')}
                      >
                        <Edit2 className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onDelete(role)}
                        className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                        title={t('targetRoleDelete')}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* TẦNG 3: PAGINATION FOOTER */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-1 px-1">
        <div className="flex items-center space-x-3 text-sm text-[#65676B] dark:text-zinc-400">
          <div>
            Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{totalItems}</span> target role templates
          </div>
          {onPageSizeChange && (
            <Select
              value={String(pageSize)}
              onValueChange={(val) => {
                if (val) onPageSizeChange(Number(val));
              }}
            >
              <SelectTrigger className="h-8 w-[110px] border-[#CED0D4] dark:border-zinc-800 text-xs font-medium focus:ring-[#1877F2]">
                <SelectValue placeholder="Page size" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="10">10 / page</SelectItem>
                <SelectItem value="20">20 / page</SelectItem>
                <SelectItem value="50">50 / page</SelectItem>
              </SelectContent>
            </Select>
          )}
        </div>

        {/* Page Buttons */}
        <div className="flex items-center space-x-1.5">
          <Button
            variant="outline"
            size="icon"
            disabled={currentPage === 1 || isLoading}
            onClick={() => onPageChange(Math.max(1, currentPage - 1))}
            className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>

          {Array.from({ length: totalPages }).map((_, index) => {
            const pageNum = index + 1;
            if (
              totalPages <= 5 ||
              pageNum === 1 ||
              pageNum === totalPages ||
              Math.abs(pageNum - currentPage) <= 1
            ) {
              const isCurrent = pageNum === currentPage;
              return (
                <Button
                  key={pageNum}
                  variant={isCurrent ? "default" : "outline"}
                  disabled={isLoading}
                  onClick={() => onPageChange(pageNum)}
                  className={`h-8 w-8 text-xs font-semibold rounded-md shadow-2xs transition-all cursor-pointer ${
                    isCurrent
                      ? "bg-[#1877F2] hover:bg-[#166FE5] text-white border-[#1877F2]"
                      : "border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400"
                  }`}
                >
                  {pageNum}
                </Button>
              );
            }
            if (
              (pageNum === 2 && currentPage > 3) ||
              (pageNum === totalPages - 1 && currentPage < totalPages - 2)
            ) {
              return (
                <span key={pageNum} className="px-1 text-xs text-[#65676B]">
                  ...
                </span>
              );
            }
            return null;
          })}

          <Button
            variant="outline"
            size="icon"
            disabled={currentPage >= totalPages || totalPages === 0 || isLoading}
            onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
            className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
});
