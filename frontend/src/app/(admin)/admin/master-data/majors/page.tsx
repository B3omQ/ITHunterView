"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Search, Plus, X, CheckCircle, RotateCcw, FolderTree } from "lucide-react";
import { useMajorTree, useRestoreMajor } from "@/hooks/useMajor";
import type { MajorDto } from "@/types/master-data.types";
import { MajorModal } from "../components/MajorModal";
import { MajorDeleteDialog } from "../components/MajorDeleteDialog";
import { MajorsTable } from "../components/MajorsTable";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useTranslations } from 'next-intl';

export default function MajorsPage() {
  const t = useTranslations('AdminMasterData');
  const [majorSearch, setMajorSearch] = useState("");
  const [debouncedMajorSearch, setDebouncedMajorSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedMajorSearch(majorSearch), 350);
    return () => clearTimeout(timer);
  }, [majorSearch]);

  const [majorPage, setMajorPage] = useState(1);
  const [majorPageSize, setMajorPageSize] = useState(10);

  useEffect(() => {
    setMajorPage(1);
  }, [debouncedMajorSearch]);

  const [toast, setToast] = useState<{
    message: string;
    type: "success" | "error" | "warning";
    undoAction?: () => void;
  } | null>(null);

  const showToast = useCallback((
    message: string,
    type: "success" | "error" | "warning" = "success",
    undoAction?: () => void,
  ) => {
    setToast({ message, type, undoAction });
  }, []);

  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 6000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  const {
    data: majorsData,
    isLoading: isMajorsLoading,
    isError: isMajorsError,
    refetch: refetchMajors,
  } = useMajorTree({
    page: majorPage,
    pageSize: majorPageSize,
    search: debouncedMajorSearch,
  });

  const restoreMajorMutation = useRestoreMajor();

  const [isMajorModalOpen, setIsMajorModalOpen] = useState(false);
  const [majorModalMode, setMajorModalMode] = useState<"create" | "edit">("create");
  const [selectedMajor, setSelectedMajor] = useState<MajorDto | null>(null);
  const [isMajorDeleteOpen, setIsMajorDeleteOpen] = useState(false);
  const [majorToDelete, setMajorToDelete] = useState<MajorDto | null>(null);

  const handleOpenMajorCreate = useCallback(() => {
    setMajorModalMode("create");
    setSelectedMajor(null);
    setIsMajorModalOpen(true);
  }, []);

  const handleOpenMajorEdit = useCallback((major: MajorDto) => {
    setMajorModalMode("edit");
    setSelectedMajor(major);
    setIsMajorModalOpen(true);
  }, []);

  const handleMajorDeleteClick = useCallback((major: MajorDto) => {
    setMajorToDelete(major);
    setIsMajorDeleteOpen(true);
  }, []);

  const handleResetFilters = () => {
    setMajorSearch("");
    setMajorPage(1);
  };

  const isFilterActive = majorSearch !== "";

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <FolderTree className="text-[#1877F2] shrink-0 h-8 w-8" />
              {t('majorsTitle')}
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              {t('majorsDesc')}
            </p>
          </div>

          <Button
            onClick={handleOpenMajorCreate}
            className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto"
          >
            <Plus className="h-4 w-4" />
            {t('addMajor')}
          </Button>
        </div>

        {/* TẦNG 1: TOOLBAR (Search & Reset Filters) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-80 md:w-96">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={majorSearch}
                onChange={(e) => setMajorSearch(e.target.value)}
                placeholder={t('searchPlaceholder')}
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {majorSearch && (
                <button
                  onClick={() => setMajorSearch("")}
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
                <RotateCcw className="h-3.5 w-3.5 mr-1.5" /> {t('resetFilters')}
              </Button>
            )}
          </div>
        </div>

        {/* TẦNG 2 & 3: MAJORS TABLE & PAGINATION CONTAINER */}
        <MajorsTable
          majors={majorsData?.data?.items || []}
          isLoading={isMajorsLoading}
          isError={isMajorsError}
          totalItems={majorsData?.data?.total || 0}
          totalPages={majorsData?.data?.totalPages || 0}
          currentPage={majorPage}
          pageSize={majorPageSize}
          onPageSizeChange={setMajorPageSize}
          onPageChange={setMajorPage}
          onEdit={handleOpenMajorEdit}
          onDelete={handleMajorDeleteClick}
          onRetry={refetchMajors}
          isFilterActive={isFilterActive}
          onResetFilters={handleResetFilters}
        />
      </div>

      <MajorModal
        isOpen={isMajorModalOpen}
        onClose={() => setIsMajorModalOpen(false)}
        mode={majorModalMode}
        initialData={selectedMajor}
        onSuccess={(msg) => { showToast(msg, "success"); refetchMajors(); }}
      />
      <MajorDeleteDialog
        isOpen={isMajorDeleteOpen}
        onClose={() => setIsMajorDeleteOpen(false)}
        majorToDelete={majorToDelete}
        onSuccess={(msg, id, name) => {
          showToast(msg, "warning", () => {
            restoreMajorMutation.mutate(id, {
              onSuccess: (res) => {
                if (res.success) { showToast(`Major "${name}" restored successfully!`, "success"); refetchMajors(); }
              },
              onError: (err: any) => { showToast(err.response?.data?.message || "Failed to restore major.", "error"); }
            });
          });
          refetchMajors();
        }}
        onError={(msg) => showToast(msg, "error")}
      />

      {toast && (
        <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
          <div className={`flex items-center gap-3 px-4 py-3 rounded-2xl shadow-lg border text-sm font-medium ${toast.type === "success" ? "bg-emerald-500/10 text-emerald-500 border-emerald-500/25" : toast.type === "warning" ? "bg-amber-500/10 text-amber-500 border-amber-500/25" : "bg-destructive/10 text-destructive border-destructive/25"}`}>
            <CheckCircle size={18} className="shrink-0" />
            <div className="flex-1">
              <span>{toast.message}</span>
              {toast.undoAction && (
                <button onClick={() => { toast.undoAction?.(); setToast(null); }} className="ml-3 inline-flex items-center gap-1 text-xs font-bold underline hover:no-underline hover:opacity-90 cursor-pointer">
                  <RotateCcw size={12} /><span>Undo</span>
                </button>
              )}
            </div>
            <button onClick={() => setToast(null)} className="text-muted-foreground hover:text-foreground shrink-0 p-0.5 rounded-lg hover:bg-black/5 cursor-pointer"><X size={14} /></button>
          </div>
        </div>
      )}
    </div>
  );
}
