'use client';

import React, { useState, useMemo } from 'react';
import Link from 'next/link';
import { usePrompts } from '@/hooks/use-prompts';
import { APP_ROUTES } from '@/lib/constants';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { format } from 'date-fns';
import {
  Search,
  X,
  RotateCcw,
  RefreshCcw,
  MessageSquare,
  Settings2,
  ChevronLeft,
  ChevronRight,
  SearchX,
} from 'lucide-react';

export default function PromptsPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');

  // Fetch prompts using TanStack Query hook complying with kinh-mantra.md
  const { data, isLoading, isError, refetch, isFetching } = usePrompts(page, pageSize);

  const promptsList = data?.data?.items || [];
  const totalCount = data?.data?.totalCount || 0;
  const totalPages = data?.data?.totalPages || Math.ceil(totalCount / pageSize) || 1;

  // Filter prompts locally if search is typed
  const filteredPrompts = useMemo(() => {
    if (!search.trim()) return promptsList;
    const q = search.toLowerCase().trim();
    return promptsList.filter(
      (prompt) =>
        prompt.promptKey.toLowerCase().includes(q) ||
        (prompt.description && prompt.description.toLowerCase().includes(q))
    );
  }, [promptsList, search]);

  const handleResetFilters = () => {
    setSearch('');
    setPage(1);
  };

  const isFilterActive = search !== '';
  const startResult = totalCount > 0 ? (page - 1) * pageSize + 1 : 0;
  const endResult = Math.min(page * pageSize, totalCount);

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <MessageSquare className="text-[#1877F2] shrink-0 h-8 w-8" />
              Prompt Management
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Manage system prompts, AI configurations, and switch active versions across LLM services.
            </p>
          </div>
        </div>

        {/* TẦNG 1: TOOLBAR (Search Bar, Reset Filters, Refresh) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-72 md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search by prompt key, description..."
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {search && (
                <button
                  onClick={() => setSearch('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1 cursor-pointer"
                  title="Clear search"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>

            {/* Clear Filters Button */}
            {isFilterActive && (
              <Button
                onClick={handleResetFilters}
                variant="ghost"
                className="h-10 px-3 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 font-medium transition-colors cursor-pointer"
              >
                <RotateCcw className="h-3.5 w-3.5 mr-1.5" /> Clear Filters
              </Button>
            )}
          </div>

          {/* Refresh Button */}
          <div className="flex items-center gap-2 w-full sm:w-auto">
            <Button
              variant="outline"
              onClick={() => refetch()}
              disabled={isLoading || isFetching}
              className="h-10 border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 transition-colors cursor-pointer w-full sm:w-auto"
            >
              <RefreshCcw className={`mr-2 h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
          </div>
        </div>

        {/* TẦNG 2: MAIN TABLE CONTAINER (TABLE_STANDARD - SHADCN TABLE) */}
        <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
          <Table className="w-full text-left border-collapse table-fixed">
            {/* Table Header */}
            <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
              <TableRow className="hover:bg-transparent border-none">
                <TableHead className="w-[45%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  PROMPT KEY & DESCRIPTION
                </TableHead>

                <TableHead className="w-[20%] py-3 px-3 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  ACTIVE VERSION
                </TableHead>

                <TableHead className="w-[20%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  LAST UPDATED
                </TableHead>

                <TableHead className="w-[15%] py-3 px-3 text-right text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
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
                      <div className="space-y-1">
                        <Skeleton className="h-5 w-1/2 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        <Skeleton className="h-3 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                      </div>
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-center">
                      <Skeleton className="h-6 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-28 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3 text-right">
                      <Skeleton className="h-8 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md ml-auto" />
                    </TableCell>
                  </TableRow>
                ))
              ) : isError ? (
                // Error State
                <TableRow>
                  <TableCell colSpan={4} className="h-64 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">
                        Failed to load system prompts
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        An error occurred while fetching prompt data. Please try again.
                      </p>
                    </div>
                  </TableCell>
                </TableRow>
              ) : filteredPrompts.length === 0 ? (
                // Empty State
                <TableRow>
                  <TableCell colSpan={4} className="h-72 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                        <SearchX className="h-6 w-6" />
                      </div>
                      <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                        No system prompts found
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        {isFilterActive
                          ? 'No prompts match the current search query. Try clearing your search.'
                          : 'No system prompts available.'}
                      </p>
                      {isFilterActive && (
                        <Button
                          onClick={handleResetFilters}
                          variant="outline"
                          className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                        >
                          <RotateCcw className="h-4 w-4 mr-2" /> Clear Search
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ) : (
                // Actual Data Rows
                filteredPrompts.map((prompt) => (
                  <TableRow
                    key={prompt.id}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Prompt Key & Description */}
                    <TableCell className="py-3.5 px-3 align-middle">
                      <div className="flex flex-col gap-0.5">
                        <span className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors">
                          {prompt.promptKey}
                        </span>
                        {prompt.description && (
                          <span className="text-xs text-[#65676B] dark:text-zinc-400 line-clamp-1">
                            {prompt.description}
                          </span>
                        )}
                      </div>
                    </TableCell>

                    {/* Active Version */}
                    <TableCell className="py-3.5 px-3 align-middle text-center">
                      {prompt.activeVersionTag ? (
                        <Badge className="bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
                          <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 mr-1.5 shrink-0" />
                          {prompt.activeVersionTag}
                        </Badge>
                      ) : (
                        <Badge className="bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700 rounded-full px-2.5 py-0.5 text-xs font-medium">
                          No Active Version
                        </Badge>
                      )}
                    </TableCell>

                    {/* Last Updated */}
                    <TableCell className="py-3.5 px-3 align-middle text-sm text-[#65676B] dark:text-zinc-300 font-medium">
                      {prompt.updatedAt
                        ? format(new Date(prompt.updatedAt), 'MMM dd, yyyy HH:mm')
                        : prompt.createdAt
                        ? format(new Date(prompt.createdAt), 'MMM dd, yyyy HH:mm')
                        : 'N/A'}
                    </TableCell>

                    {/* Actions */}
                    <TableCell className="py-3.5 px-3 align-middle text-right">
                      <Link href={`${APP_ROUTES.STAFF.PROMPTS}/${prompt.id}`}>
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-8 border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] hover:border-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400 cursor-pointer font-medium"
                        >
                          <Settings2 className="w-3.5 h-3.5 mr-1.5 text-[#1877F2]" />
                          Manage
                        </Button>
                      </Link>
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
              Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{totalCount}</span> system prompts
            </div>
            <Select
              value={String(pageSize)}
              onValueChange={(val) => {
                if (val) setPageSize(Number(val));
                setPage(1);
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
          </div>

          {/* Page Buttons */}
          <div className="flex items-center space-x-1.5">
            <Button
              variant="outline"
              size="icon"
              disabled={page === 1 || isLoading}
              onClick={() => setPage((prev) => Math.max(1, prev - 1))}
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
                Math.abs(pageNum - page) <= 1
              ) {
                const isCurrent = pageNum === page;
                return (
                  <Button
                    key={pageNum}
                    variant={isCurrent ? 'default' : 'outline'}
                    disabled={isLoading}
                    onClick={() => setPage(pageNum)}
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
                (pageNum === 2 && page > 3) ||
                (pageNum === totalPages - 1 && page < totalPages - 2)
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
              disabled={page >= totalPages || isLoading}
              onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
              className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
