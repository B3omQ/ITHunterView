"use client";

import React, { memo, useState, useMemo } from "react";
import { Edit2, Trash2, ChevronLeft, ChevronRight, ChevronDown, RotateCcw, SearchX } from "lucide-react";
import type { MajorDto } from "@/types/master-data.types";
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

interface MajorsTableProps {
  majors: MajorDto[];
  isLoading: boolean;
  isError: boolean;
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  onPageSizeChange?: (pageSize: number) => void;
  onPageChange: (page: number) => void;
  onEdit: (major: MajorDto) => void;
  onDelete: (major: MajorDto) => void;
  onRetry: () => void;
  isFilterActive?: boolean;
  onResetFilters?: () => void;
}

interface FlattenedMajor {
  item: MajorDto;
  level: number;
  hasChildren: boolean;
}

export const MajorsTable = memo(function MajorsTable({
  majors,
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

  // Recursive tree flattening
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
          <Badge className="bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold shadow-none">
            Level 1 (Root)
          </Badge>
        );
      case 2:
        return (
          <Badge className="bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold shadow-none">
            Level 2 (Category)
          </Badge>
        );
      case 3:
        return (
          <Badge className="bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold shadow-none">
            Level 3 (Specialization)
          </Badge>
        );
      default:
        return null;
    }
  };

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
              <TableHead className="w-[22%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                SPECIALIZATION CODE
              </TableHead>

              <TableHead className="w-[70%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                SPECIALIZATION NAME
              </TableHead>

              <TableHead className="w-[8%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                ACTIONS
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
                    <Skeleton className="h-5 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                  </TableCell>
                  <TableCell className="py-3.5 px-3">
                    <Skeleton className="h-5 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
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
                      Failed to load specializations
                    </p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      An error occurred while fetching specializations data. Please try again.
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
            ) : flattenedMajors.length === 0 ? (
              // Empty State
              <TableRow>
                <TableCell colSpan={3} className="h-72 text-center">
                  <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                    <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                      <SearchX className="h-6 w-6" />
                    </div>
                    <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                      No specializations found
                    </p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      {isFilterActive
                        ? "No specializations match the current search query. Try clearing or adjusting your filter."
                        : "No specializations recorded yet."}
                    </p>
                    {isFilterActive && onResetFilters && (
                      <Button
                        onClick={onResetFilters}
                        variant="outline"
                        className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                      >
                        <RotateCcw className="h-4 w-4 mr-2" /> Clear All Filters
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              // Actual Data Rows
              flattenedMajors.map(({ item, level, hasChildren }) => (
                <TableRow
                  key={item.id}
                  className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                >
                  {/* Specialization Code */}
                  <TableCell className="py-3.5 px-3 align-middle font-mono text-xs">
                    <span className="font-semibold text-[#050505] dark:text-zinc-200 bg-slate-100 dark:bg-zinc-800 px-2 py-1 rounded-md border border-[#CED0D4]/60 dark:border-zinc-700">
                      {item.code}
                    </span>
                  </TableCell>

                  {/* Specialization Name */}
                  <TableCell className="py-3.5 px-3 align-middle">
                    <div
                      className="flex items-center gap-2"
                      style={{ paddingLeft: `${(level - 1) * 24}px` }}
                    >
                      {hasChildren ? (
                        <button
                          onClick={(e) => toggleExpand(item.id, e)}
                          className="p-1 rounded-md hover:bg-[#E7F3FF] dark:hover:bg-blue-950/50 text-[#65676B] dark:text-zinc-400 hover:text-[#1877F2] transition-colors cursor-pointer"
                        >
                          {expandedIds.has(item.id) ? (
                            <ChevronDown className="h-4 w-4" />
                          ) : (
                            <ChevronRight className="h-4 w-4" />
                          )}
                        </button>
                      ) : (
                        <div className="w-6" />
                      )}
                      <span className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors mr-1.5">
                        {item.name}
                      </span>
                      {getLevelBadge(level)}
                    </div>
                  </TableCell>

                  {/* Actions (Icon-only buttons) */}
                  <TableCell className="py-3.5 px-2 align-middle text-center">
                    <div className="flex items-center justify-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onEdit(item)}
                        className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                        title="Edit Specialization"
                      >
                        <Edit2 className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onDelete(item)}
                        className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                        title="Delete Specialization"
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
            Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{totalItems}</span> root specializations
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
