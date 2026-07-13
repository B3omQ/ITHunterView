"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuestionBank } from "@/hooks/useQuestionBank";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import {
  Plus,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  Loader2,
  BookOpen,
  Briefcase
} from "lucide-react";

export default function QuestionBankPage() {
  const pageSize = 10;
  const {
    questions,
    totalCount,
    page,
    setPage,
    industry,
    setIndustry,
    level,
    setLevel,
    loading,
    createQuestion,
    updateQuestion,
    deleteQuestion,
    refresh
  } = useQuestionBank(1, pageSize);

  const router = useRouter();

  const totalPages = Math.ceil(totalCount / pageSize);
  const startResult = (page - 1) * pageSize + 1;
  const endResult = Math.min(page * pageSize, totalCount);

  const openCreateModal = () => {
    router.push("/staff/question-bank/create");
  };

  const openEditModal = (question: any) => {
    router.push(`/staff/question-bank/${question.id}/edit`);
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Are you sure you want to delete this question?")) return;
    const res = await deleteQuestion(id);
    if (!res.success) {
      alert(res.message || "Failed to delete question");
    }
  };

  const renderLevelBadge = (lvl: string) => {
    const colors: Record<string, string> = {
      FRESHER: "bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800",
      INTERN: "bg-teal-100 text-teal-800 border-teal-200 dark:bg-teal-900/30 dark:text-teal-400 dark:border-teal-800",
      JUNIOR: "bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800",
      MIDDLE: "bg-purple-100 text-purple-800 border-purple-200 dark:bg-purple-900/30 dark:text-purple-400 dark:border-purple-800",
      SENIOR: "bg-rose-100 text-rose-800 border-rose-200 dark:bg-rose-900/30 dark:text-rose-400 dark:border-rose-800",
    };
    const c = colors[lvl] || "bg-zinc-100 text-zinc-800 border-zinc-200 dark:bg-zinc-800 dark:text-zinc-400 dark:border-zinc-700";
    return (
      <span className={`inline-flex px-2 py-0.5 rounded-full text-[11px] font-bold border ${c}`}>
        {lvl}
      </span>
    );
  };

  return (
    <div className="min-h-screen bg-background py-6 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
      <div className="max-w-7xl mx-auto space-y-4">

        {/* Top Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-zinc-900 dark:text-zinc-50 tracking-tight">Question Bank</h1>
            <p className="text-zinc-500 dark:text-zinc-400 mt-1.5 text-sm">Manage sample interview questions</p>
          </div>
          <Button
            onClick={openCreateModal}
            className="bg-blue-600 hover:bg-blue-700 text-white font-medium shadow-md shadow-blue-500/10 hover:shadow-blue-500/20 transition-all gap-2"
          >
            <Plus className="h-4.5 w-4.5" />
            Add Question
          </Button>
        </div>

        {/* Filters */}
        <div className="flex flex-col sm:flex-row items-center gap-4 py-2 border-b border-zinc-200 dark:border-zinc-800 mb-4">
          <div className="flex items-center gap-2 w-full sm:w-auto">
            <span className="text-sm font-medium text-zinc-500 dark:text-zinc-400 shrink-0">Industry:</span>
            <Select value={industry} onValueChange={(val) => { setIndustry(val || ""); setPage(1); }}>
              <SelectTrigger className="w-full sm:w-48 bg-white dark:bg-zinc-950 border-zinc-200 dark:border-zinc-800">
                <SelectValue placeholder="All Industries" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="ALL">All Industries</SelectItem>
                <SelectItem value="BA">BA</SelectItem>
                <SelectItem value="DEV">Dev</SelectItem>
                <SelectItem value="TEST">Test</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center gap-2 w-full sm:w-auto">
            <span className="text-sm font-medium text-zinc-500 dark:text-zinc-400 shrink-0">Level:</span>
            <select
              value={level}
              onChange={(e) => { setLevel(e.target.value); setPage(1); }}
              className="h-10 w-full sm:w-44 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
            >
              <option value="ALL">All Levels</option>
              <option value="INTERN_FRESHER">Intern/Fresher</option>
              <option value="JUNIOR">Junior</option>
              <option value="MIDDLE">Middle</option>
              <option value="SENIOR">Senior</option>
            </select>
          </div>
        </div>

        {/* Grid View */}
        <div className="relative min-h-[400px]">
          {loading && (
            <div className="absolute inset-0 bg-white/70 dark:bg-zinc-950/70 z-10 flex items-center justify-center backdrop-blur-xs rounded-2xl">
              <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
            </div>
          )}

          {questions.length > 0 ? (
            <div className="bg-white dark:bg-zinc-900 border border-zinc-200/80 dark:border-zinc-800/80 rounded-xl overflow-x-auto shadow-sm">
              <table className="w-full text-sm text-left">
                <thead className="bg-zinc-50/80 dark:bg-zinc-950/80 border-b border-zinc-200/80 dark:border-zinc-800/80 text-zinc-500 font-semibold text-xs uppercase tracking-wider">
                  <tr>
                    <th className="px-5 py-4 min-w-[120px]">Category</th>
                    <th className="px-5 py-4 w-full min-w-[300px]">Question & Answer</th>
                    <th className="px-5 py-4 text-right min-w-[120px]">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-zinc-100 dark:divide-zinc-800/50">
                  {questions.map((q) => (
                    <tr key={q.id} className="hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors group">
                      <td className="px-5 py-4 align-top">
                        <div className="flex flex-col gap-2">
                          <div className="flex items-center gap-1.5 text-xs text-zinc-600 dark:text-zinc-300 font-medium">
                            <Briefcase className="h-3.5 w-3.5 text-blue-500" />
                            {q.industry || 'N/A'}
                          </div>
                          <div>
                            {renderLevelBadge(q.level)}
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-4 align-top">
                        <div className="space-y-2">
                          <p className="font-semibold text-zinc-900 dark:text-zinc-50 text-sm whitespace-pre-wrap">
                            {q.questionText}
                          </p>
                        </div>
                      </td>
                      <td className="px-5 py-4 align-top text-right">
                        <div className="flex items-center justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => openEditModal(q)}
                            title="Edit"
                            className="h-8 w-8 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:bg-blue-900/30"
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => handleDelete(q.id)}
                            title="Delete"
                            className="h-8 w-8 text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/30"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="bg-white dark:bg-zinc-900 rounded-xl shadow-xs border border-zinc-200/80 dark:border-zinc-800/80 p-16 text-center text-zinc-500 dark:text-zinc-400">
              No questions found.
            </div>
          )}

          {/* Pagination */}
          {totalCount > 0 && (
            <div className="px-6 py-4 flex flex-col sm:flex-row items-center justify-between border-t border-zinc-200 dark:border-zinc-800 gap-4 bg-zinc-50/20 dark:bg-zinc-950/10">
              <span className="text-sm text-zinc-500 dark:text-zinc-400">
                Showing <strong className="font-semibold text-zinc-700 dark:text-zinc-300">{startResult}</strong> to{" "}
                <strong className="font-semibold text-zinc-700 dark:text-zinc-300">{endResult}</strong> of{" "}
                <strong className="font-semibold text-zinc-700 dark:text-zinc-300">{totalCount}</strong> results
              </span>

              <div className="flex items-center gap-1">
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  disabled={page === 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>

                <span className="text-sm font-medium mx-2">{page} / {totalPages}</span>

                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
