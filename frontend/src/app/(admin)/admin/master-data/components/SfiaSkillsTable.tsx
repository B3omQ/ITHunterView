'use client';

import React from 'react';
import { Edit2, Trash2, SearchX, RotateCcw } from 'lucide-react';
import { SfiaSkillDto } from '@/types/master-data.types';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

interface SfiaSkillsTableProps {
  skills: SfiaSkillDto[];
  isLoading: boolean;
  isError: boolean;
  onEdit: (skill: SfiaSkillDto) => void;
  onDelete: (skill: SfiaSkillDto) => void;
  onRetry: () => void;
}

export function SfiaSkillsTable({
  skills,
  isLoading,
  isError,
  onEdit,
  onDelete,
  onRetry,
}: SfiaSkillsTableProps) {
  // Group skills by category -> subcategory -> skills
  const groupedData = React.useMemo(() => {
    const categories: Record<string, Record<string, SfiaSkillDto[]>> = {};

    if (!Array.isArray(skills)) return categories;

    skills.forEach((skill) => {
      if (!categories[skill.category]) {
        categories[skill.category] = {};
      }
      const subcat = skill.subcategory || 'Other';
      if (!categories[skill.category][subcat]) {
        categories[skill.category][subcat] = [];
      }
      categories[skill.category][subcat].push(skill);
    });

    return categories;
  }, [skills]);

  return (
    <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
      <Table className="w-full text-left border-collapse table-fixed">
        {/* Table Header */}
        <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
          <TableRow className="hover:bg-transparent border-none">
            <TableHead className="w-[18%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
              CATEGORY
            </TableHead>
            <TableHead className="w-[18%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
              SUBCATEGORY
            </TableHead>
            <TableHead className="w-[32%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
              SKILL &amp; CODE
            </TableHead>
            <TableHead className="w-[24%] py-3 px-1 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
              LEVELS (1 - 7)
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
            Array.from({ length: 6 }).map((_, index) => (
              <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                <TableCell className="py-3.5 px-3">
                  <Skeleton className="h-5 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                </TableCell>
                <TableCell className="py-3.5 px-3">
                  <Skeleton className="h-5 w-24 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                </TableCell>
                <TableCell className="py-3.5 px-3">
                  <Skeleton className="h-5 w-40 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                </TableCell>
                <TableCell className="py-3.5 px-1 text-center">
                  <Skeleton className="h-6 w-32 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                </TableCell>
                <TableCell className="py-3.5 px-2 text-center">
                  <Skeleton className="h-8 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
                </TableCell>
              </TableRow>
            ))
          ) : isError ? (
            // Error State
            <TableRow>
              <TableCell colSpan={5} className="h-64 text-center">
                <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                  <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">
                    Failed to load SFIA skills
                  </p>
                  <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                    An error occurred while fetching SFIA 9 skills. Please try again.
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
          ) : !skills || skills.length === 0 ? (
            // Empty State
            <TableRow>
              <TableCell colSpan={5} className="h-72 text-center">
                <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                  <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                    <SearchX className="h-6 w-6" />
                  </div>
                  <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                    No SFIA skills found
                  </p>
                  <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                    Try adjusting your search criteria or add a new SFIA skill.
                  </p>
                </div>
              </TableCell>
            </TableRow>
          ) : (
            // Grouped Data Rows
            Object.entries(groupedData).map(([category, subcategories]) => {
              const totalCategoryRows = Object.values(subcategories).reduce(
                (sum, items) => sum + items.length,
                0
              );
              let isFirstInCategory = true;

              return (
                <React.Fragment key={category}>
                  {Object.entries(subcategories).map(([subcategory, subcatSkills]) => {
                    const totalSubcategoryRows = subcatSkills.length;
                    let isFirstInSubcategory = true;

                    return subcatSkills.map((skill) => {
                      const availableLevels = skill.availableLevels
                        ? skill.availableLevels.split(',').map(Number)
                        : [];

                      const rowElement = (
                        <TableRow
                          key={skill.id}
                          className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                        >
                          {/* Category Cell */}
                          {isFirstInCategory && (
                            <TableCell
                              className="py-3.5 px-3 font-bold text-xs text-[#1877F2] dark:text-blue-400 align-top border-r border-[#CED0D4]/60 dark:border-zinc-800/60 bg-slate-50/50 dark:bg-zinc-950/50"
                              rowSpan={totalCategoryRows}
                            >
                              {category}
                            </TableCell>
                          )}

                          {/* Subcategory Cell */}
                          {isFirstInSubcategory && (
                            <TableCell
                              className="py-3.5 px-3 text-xs text-[#65676B] dark:text-zinc-300 align-top border-r border-[#CED0D4]/60 dark:border-zinc-800/60 font-medium"
                              rowSpan={totalSubcategoryRows}
                            >
                              {subcategory === 'Other' ? (
                                <span className="italic opacity-50">None</span>
                              ) : (
                                subcategory
                              )}
                            </TableCell>
                          )}

                          {/* Skill Name & Code */}
                          <TableCell className="py-3.5 px-3 align-middle">
                            <div className="flex items-center gap-2">
                              <span className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors">
                                {skill.skillName}
                              </span>
                              <span className="font-mono text-xs bg-slate-100 dark:bg-zinc-800 text-[#050505] dark:text-zinc-200 px-1.5 py-0.5 rounded border border-[#CED0D4]/60 dark:border-zinc-700 shrink-0">
                                {skill.skillCode}
                              </span>
                            </div>
                          </TableCell>

                          {/* Levels (1-7) */}
                          <TableCell className="py-3.5 px-1 align-middle text-center">
                            <div className="flex items-center justify-center gap-1">
                              {[1, 2, 3, 4, 5, 6, 7].map((level) => {
                                const isAvailable = availableLevels.includes(level);
                                return (
                                  <span
                                    key={level}
                                    className={`h-6 w-6 rounded-full flex items-center justify-center text-xs font-bold transition-all ${
                                      isAvailable
                                        ? 'bg-[#E7F3FF] dark:bg-blue-950/60 text-[#1877F2] dark:text-blue-400 border border-[#1877F2]/30 shadow-2xs'
                                        : 'text-[#65676B]/30'
                                    }`}
                                  >
                                    {isAvailable ? level : '•'}
                                  </span>
                                );
                              })}
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
                                title="Edit SFIA Skill"
                              >
                                <Edit2 className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => onDelete(skill)}
                                className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                                title="Delete SFIA Skill"
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      );

                      isFirstInCategory = false;
                      isFirstInSubcategory = false;

                      return rowElement;
                    });
                  })}
                </React.Fragment>
              );
            })
          )}
        </TableBody>
      </Table>
    </div>
  );
}
