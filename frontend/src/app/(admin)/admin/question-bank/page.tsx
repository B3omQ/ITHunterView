'use client';

import React, { useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { useQuestionBank } from '@/hooks/useQuestionBank';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Plus,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  BookOpen,
  Briefcase,
  Search,
  X,
  RotateCcw,
  SearchX,
} from 'lucide-react';
import { toast } from 'sonner';

export default function AdminQuestionBankPage() {
  const router = useRouter();
  const [search, setSearch] = useState('');

  // Custom hook complying with kinh-mantra.md (page -> hook -> service -> api-client -> backend)
  const {
    questions,
    totalCount,
    page,
    setPage,
    pageSize,
    setPageSize,
    industry,
    setIndustry,
    level,
    setLevel,
    loading,
    deleteQuestion,
  } = useQuestionBank(1, 10);

  const totalPages = Math.ceil(totalCount / pageSize) || 1;
  const startResult = totalCount > 0 ? (page - 1) * pageSize + 1 : 0;
  const endResult = Math.min(page * pageSize, totalCount);

  // Client-side search filtering over fetched question items
  const filteredQuestions = useMemo(() => {
    if (!search.trim()) return questions;
    const q = search.toLowerCase().trim();
    return questions.filter(
      (item) =>
        item.questionText.toLowerCase().includes(q) ||
        (item.industry && item.industry.toLowerCase().includes(q)) ||
        (item.level && item.level.toLowerCase().includes(q))
    );
  }, [questions, search]);

  const openCreateModal = () => {
    router.push('/admin/question-bank/create');
  };

  const openEditModal = (question: any) => {
    router.push(`/admin/question-bank/${question.id}/edit`);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this question?')) return;
    const res = await deleteQuestion(id);
    if (res.success) {
      toast.success('Question deleted successfully');
    } else {
      toast.error(res.message || 'Failed to delete question');
    }
  };

  const handleResetFilters = () => {
    setSearch('');
    setIndustry('');
    setLevel('');
    setPage(1);
  };

  const isFilterActive = search !== '' || (industry !== '' && industry !== 'ALL') || (level !== '' && level !== 'ALL');

  // Standardized muted pastel level badges
  const renderLevelBadge = (lvl: string) => {
    const l = lvl?.toUpperCase();
    switch (l) {
      case 'INTERN':
      case 'FRESHER':
      case 'INTERN_FRESHER':
        return (
          <Badge className="bg-teal-50 dark:bg-teal-950/40 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
            <span className="h-1.5 w-1.5 rounded-full bg-teal-500 mr-1.5 shrink-0" />
            {lvl}
          </Badge>
        );
      case 'JUNIOR':
        return (
          <Badge className="bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
            <span className="h-1.5 w-1.5 rounded-full bg-blue-500 mr-1.5 shrink-0" />
            Junior
          </Badge>
        );
      case 'MIDDLE':
        return (
          <Badge className="bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
            <span className="h-1.5 w-1.5 rounded-full bg-purple-500 mr-1.5 shrink-0" />
            Middle
          </Badge>
        );
      case 'SENIOR':
        return (
          <Badge className="bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
            <span className="h-1.5 w-1.5 rounded-full bg-rose-500 mr-1.5 shrink-0" />
            Senior
          </Badge>
        );
      default:
        return (
          <Badge variant="outline" className="rounded-full px-2.5 py-0.5 text-xs font-medium">
            {lvl || 'N/A'}
          </Badge>
        );
    }
  };

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <BookOpen className="text-[#1877F2] shrink-0 h-8 w-8" />
              Question Bank (Admin)
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Manage sample interview questions across all domains and difficulty levels.
            </p>
          </div>

          <Button
            onClick={openCreateModal}
            className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto"
          >
            <Plus className="h-4 w-4" />
            Add Question
          </Button>
        </div>

        {/* TẦNG 1: TOOLBAR (Search & Dropdown Filters) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-72 md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search questions, industry, level..."
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

            {/* Industry Filter */}
            <Select
              value={industry || 'ALL'}
              onValueChange={(val) => {
                if (val) setIndustry(val === 'ALL' ? '' : val);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[160px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder="Industry" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">All Industries</SelectItem>
                <SelectItem value="BA">BA</SelectItem>
                <SelectItem value="DEV">Dev</SelectItem>
                <SelectItem value="TEST">Test</SelectItem>
              </SelectContent>
            </Select>

            {/* Level Filter */}
            <Select
              value={level || 'ALL'}
              onValueChange={(val) => {
                if (val) setLevel(val === 'ALL' ? '' : val);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full sm:w-[160px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder="Level" />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">All Levels</SelectItem>
                <SelectItem value="INTERN_FRESHER">Intern / Fresher</SelectItem>
                <SelectItem value="JUNIOR">Junior</SelectItem>
                <SelectItem value="MIDDLE">Middle</SelectItem>
                <SelectItem value="SENIOR">Senior</SelectItem>
              </SelectContent>
            </Select>

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
        </div>

        {/* TẦNG 2: MAIN TABLE CONTAINER (TABLE_STANDARD - SHADCN TABLE) */}
        <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
          <Table className="w-full text-left border-collapse table-fixed">
            {/* Table Header */}
            <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
              <TableRow className="hover:bg-transparent border-none">
                <TableHead className="w-[22%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  CATEGORY & LEVEL
                </TableHead>

                <TableHead className="w-[70%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  QUESTION CONTENT
                </TableHead>

                <TableHead className="w-[8%] py-3 px-2 text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  ACTIONS
                </TableHead>
              </TableRow>
            </TableHeader>

            {/* Table Body */}
            <TableBody>
              {loading ? (
                // Loading Skeleton State (6 rows)
                Array.from({ length: pageSize || 6 }).map((_, index) => (
                  <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-4 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md mb-2" />
                      <Skeleton className="h-6 w-24 bg-slate-100 dark:bg-zinc-800 rounded-full" />
                    </TableCell>
                    <TableCell className="py-3.5 px-3">
                      <Skeleton className="h-5 w-4/5 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                    </TableCell>
                    <TableCell className="py-3.5 px-2 text-center">
                      <Skeleton className="h-8 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md mx-auto" />
                    </TableCell>
                  </TableRow>
                ))
              ) : filteredQuestions.length === 0 ? (
                // Empty State
                <TableRow>
                  <TableCell colSpan={3} className="h-72 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                      <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                        <SearchX className="h-6 w-6" />
                      </div>
                      <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                        No questions found
                      </p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                        {isFilterActive
                          ? 'No questions match the current filters. Try clearing or adjusting your filter.'
                          : 'No question items created yet.'}
                      </p>
                      {isFilterActive && (
                        <Button
                          onClick={handleResetFilters}
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
                filteredQuestions.map((q) => (
                  <TableRow
                    key={q.id}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Category & Level */}
                    <TableCell className="py-3.5 px-3 align-top">
                      <div className="flex flex-col gap-2">
                        <div className="flex items-center gap-1.5 text-xs text-[#65676B] dark:text-zinc-300 font-semibold">
                          <Briefcase className="h-3.5 w-3.5 text-[#1877F2] shrink-0" />
                          {q.industry || 'N/A'}
                        </div>
                        <div>{renderLevelBadge(q.level)}</div>
                      </div>
                    </TableCell>

                    {/* Question Content */}
                    <TableCell className="py-3.5 px-3 align-top">
                      <p className="font-medium text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors whitespace-pre-wrap">
                        {q.questionText}
                      </p>
                    </TableCell>

                    {/* Actions (Icon-only buttons) */}
                    <TableCell className="py-3.5 px-2 align-top text-center">
                      <div className="flex items-center justify-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => openEditModal(q)}
                          className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                          title="Edit Question"
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDelete(q.id)}
                          className="h-8 w-8 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40 cursor-pointer"
                          title="Delete Question"
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
              Showing <span className="font-semibold text-[#050505] dark:text-zinc-200">{startResult} - {endResult}</span> of <span className="font-semibold text-[#050505] dark:text-zinc-200">{totalCount}</span> question items
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
              disabled={page === 1 || loading}
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
                    disabled={loading}
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
              disabled={page >= totalPages || loading}
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
