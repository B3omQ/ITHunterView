"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Search, Plus, X, CheckCircle, RotateCcw } from "lucide-react";
import { useMajorTree, useRestoreMajor } from "@/hooks/useMajor";
import type { MajorDto } from "@/types/master-data.types";
import { MajorModal } from "../components/MajorModal";
import { MajorDeleteDialog } from "../components/MajorDeleteDialog";
import { MajorsTable } from "../components/MajorsTable";

export default function MajorsPage() {
  const [majorSearch, setMajorSearch] = useState("");
  const [debouncedMajorSearch, setDebouncedMajorSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedMajorSearch(majorSearch), 300);
    return () => clearTimeout(timer);
  }, [majorSearch]);

  const [majorPage, setMajorPage] = useState(1);
  const majorPageSize = 10;

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

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Specializations (Majors)</h1>
          <p className="text-sm text-muted-foreground mt-1">Manage the hierarchy of IT specializations.</p>
        </div>
        <button
          onClick={handleOpenMajorCreate}
          className="inline-flex items-center gap-1.5 px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors"
        >
          <Plus size={16} />
          <span>Add new specialization</span>
        </button>
      </div>

      <div className="space-y-4">
        <div className="bg-card border border-border rounded-2xl p-4 shadow-2xs flex gap-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search specializations by name or code..."
              value={majorSearch}
              onChange={(e) => setMajorSearch(e.target.value)}
              className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
            />
            {majorSearch && (
              <button onClick={() => setMajorSearch("")} className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground">
                <X size={16} />
              </button>
            )}
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-2xs">
          <MajorsTable
            majors={majorsData?.data?.items || []}
            isLoading={isMajorsLoading}
            isError={isMajorsError}
            totalItems={majorsData?.data?.total || 0}
            totalPages={majorsData?.data?.totalPages || 0}
            currentPage={majorPage}
            pageSize={majorPageSize}
            onPageChange={setMajorPage}
            onEdit={handleOpenMajorEdit}
            onDelete={handleMajorDeleteClick}
            onRetry={refetchMajors}
          />
        </div>
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
                <button onClick={() => { toast.undoAction?.(); setToast(null); }} className="ml-3 inline-flex items-center gap-1 text-xs font-bold underline hover:no-underline hover:opacity-90">
                  <RotateCcw size={12} /><span>Undo</span>
                </button>
              )}
            </div>
            <button onClick={() => setToast(null)} className="text-muted-foreground hover:text-foreground shrink-0 p-0.5 rounded-lg hover:bg-black/5"><X size={14} /></button>
          </div>
        </div>
      )}
    </div>
  );
}
