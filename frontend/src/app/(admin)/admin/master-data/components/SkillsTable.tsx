'use client';

import React, { memo, useState } from 'react';
import {
  Edit2,
  Trash2,
  ChevronLeft,
  ChevronRight,
  SearchX,
  RotateCcw,
} from 'lucide-react';
import type { SkillDto } from '@/types/master-data.types';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { useTranslations } from 'next-intl';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

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
  const t = useTranslations('AdminMasterData');
  const [internalPageSize, setInternalPageSize] = useState(pageSize);

  const totalCount = totalItems || skills.length;
  const computedTotalPages = totalPages || Math.ceil(totalCount / internalPageSize) || 1;

  const startResult = totalCount > 0 ? (currentPage - 1) * internalPageSize + 1 : 0;
  const endResult = Math.min(currentPage * internalPageSize, totalCount);

  return (
    <div className="flex flex-col h-full justify-between">
      {/* TẦNG 2: MAIN TABLE CONTAINER (TABLE_STANDARD - SHADCN TABLE) */}
      <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
        <Table className="w-full text-left border-collapse table-fixed">
          {/* Table Header */}
          <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
            <TableRow className="hover:bg-transparent border-none">
              <TableHead className="w-[65%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t('colSkillName')}
              </TableHead>

              <TableHead className="w-[20%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t('colStatus')}
              </TableHead>

              <TableHead className="w-[15%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t('colActions')}
              </TableHead>
            </TableRow>
          </TableHeader>

          {/* Table Body */}
          <TableBody>
            {isLoading ? (
              // Loading Skeleton State (6 rows)
              Array.from({ length: internalPageSize || 6 }).map((_, index) => (
                <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                  <TableCell className="py-3.5 px-3">
                    <Skeleton className="h-5 w-36 bg-slate-100 dark:bg-zinc-800 rounded-md mb-1.5" />
                    <div className="flex gap-1">
                      <Skeleton className="h-4 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                      <Skeleton className="h-4 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </div>
                  </TableCell>
                  <TableCell className="py-3.5 px-3 text-center">
                    <Skeleton className="h-5 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
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
                      Failed to load skills
                    </p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      An error occurred while fetching skills library. Please try again.
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
            ) : skills.length === 0 ? (
              // Empty State
              <TableRow>
                <TableCell colSpan={3} className="h-72 text-center">
                  <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                    <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                      <SearchX className="h-6 w-6" />
                    </div>
                    <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                      No matching skills found
                    </p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      No skills found in the selected category or search filter.
                    </p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              // Actual Data Rows
              skills.map((skill) => (
                <TableRow
                  key={skill.id}
                  className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                >
                  {/* Skill & Aliases */}
                  <TableCell className="py-3.5 px-3 align-middle">
                    <div className="flex flex-col gap-1">
                      <span className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors">
                        {skill.name}
                      </span>
                      {skill.aliases && skill.aliases.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-0.5">
                          {skill.aliases.slice(0, 4).map((alias, idx) => (
                            <span
                              key={idx}
                              className="inline-flex items-center px-2 py-0.5 rounded-md text-[10px] font-medium bg-slate-100 dark:bg-zinc-800 text-[#65676B] dark:text-zinc-300 border border-[#CED0D4]/60 dark:border-zinc-700"
                            >
                              {alias}
                            </span>
                          ))}
                          {skill.aliases.length > 4 && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded-md text-[10px] font-semibold bg-[#E7F3FF] dark:bg-blue-950/60 text-[#1877F2] dark:text-blue-400 border border-[#1877F2]/30">
                              +{skill.aliases.length - 4} more
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                  </TableCell>

                  {/* Status (Switch + Label) */}
                  <TableCell className="py-3.5 px-3 align-middle text-center">
                    <div className="flex items-center justify-center gap-2">
                      <Switch
                        checked={skill.status === 'ACTIVE'}
                        onCheckedChange={() => onStatusToggle(skill)}
                      />
                      <span className="text-xs font-semibold text-[#050505] dark:text-zinc-300">
                        {skill.status === 'ACTIVE' ? t('statusActive') : t('statusInactive')}
                      </span>
                    </div>
                  </TableCell>

                  {/* Actions (Icon-only buttons) */}
                  <TableCell className="py-3.5 px-2 align-middle text-center">
                    <div className="flex items-center justify-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onEdit(skill)}
                        className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                        title={t('editSkill')}
                      >
                        <Edit2 className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onDelete(skill)}
                        className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                        title={t('deleteSkill')}
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
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-4 px-1">
        <div className="flex items-center space-x-3 text-sm text-[#65676B] dark:text-zinc-400">
          <div>
            Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{totalCount}</span> skills
          </div>
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

          {Array.from({ length: computedTotalPages }).map((_, index) => {
            const pageNum = index + 1;
            if (
              computedTotalPages <= 5 ||
              pageNum === 1 ||
              pageNum === computedTotalPages ||
              Math.abs(pageNum - currentPage) <= 1
            ) {
              const isCurrent = pageNum === currentPage;
              return (
                <Button
                  key={pageNum}
                  variant={isCurrent ? 'default' : 'outline'}
                  disabled={isLoading}
                  onClick={() => onPageChange(pageNum)}
                  className={`h-8 w-8 text-xs font-semibold rounded-md shadow-2xs transition-all cursor-pointer ${
                    isCurrent
                      ? 'bg-[#1877F2] hover:bg-[#166FE5] text-white border-[#1877F2]'
                      : 'border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400'
                  }`}
                >
                  {pageNum}
                </Button>
              );
            }
            if (
              (pageNum === 2 && currentPage > 3) ||
              (pageNum === computedTotalPages - 1 && currentPage < computedTotalPages - 2)
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
            disabled={currentPage >= computedTotalPages || isLoading}
            onClick={() => onPageChange(Math.min(computedTotalPages, currentPage + 1))}
            className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
});
