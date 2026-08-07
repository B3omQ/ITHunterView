"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Search, Plus, X, CheckCircle, RotateCcw, Target, UploadCloud } from "lucide-react";
import { usePagedTargetRoles } from "@/hooks/useTargetRole";
import type { TargetRoleTemplateDto } from "@/types/master-data.types";
import { TargetRoleModal } from "../components/TargetRoleModal";
import { TargetRoleDeleteDialog } from "../components/TargetRoleDeleteDialog";
import { TargetRolesTable } from "../components/TargetRolesTable";
import { ImportTargetRoleModal } from "../components/ImportTargetRoleModal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useTranslations } from 'next-intl';

export default function TargetRolesPage() {
  const t = useTranslations('AdminMasterData');
  const [roleSearch, setRoleSearch] = useState("");
  const [debouncedRoleSearch, setDebouncedRoleSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedRoleSearch(roleSearch), 350);
    return () => clearTimeout(timer);
  }, [roleSearch]);

  const [rolePage, setRolePage] = useState(1);
  const [rolePageSize, setRolePageSize] = useState(10);

  useEffect(() => {
    setRolePage(1);
  }, [debouncedRoleSearch]);

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
    data: rolesData,
    isLoading: isRolesLoading,
    isError: isRolesError,
    refetch: refetchRoles,
  } = usePagedTargetRoles({
    page: rolePage,
    pageSize: rolePageSize,
    search: debouncedRoleSearch,
  });

  const [isRoleModalOpen, setIsRoleModalOpen] = useState(false);
  const [roleModalMode, setRoleModalMode] = useState<"create" | "edit">("create");
  const [selectedRole, setSelectedRole] = useState<TargetRoleTemplateDto | null>(null);
  const [isRoleDeleteOpen, setIsRoleDeleteOpen] = useState(false);
  const [roleToDelete, setRoleToDelete] = useState<TargetRoleTemplateDto | null>(null);
  const [isImportModalOpen, setIsImportModalOpen] = useState(false);

  const handleOpenRoleCreate = useCallback(() => {
    setRoleModalMode("create");
    setSelectedRole(null);
    setIsRoleModalOpen(true);
  }, []);

  const handleOpenRoleEdit = useCallback((role: TargetRoleTemplateDto) => {
    setRoleModalMode("edit");
    setSelectedRole(role);
    setIsRoleModalOpen(true);
  }, []);

  const handleRoleDeleteClick = useCallback((role: TargetRoleTemplateDto) => {
    setRoleToDelete(role);
    setIsRoleDeleteOpen(true);
  }, []);

  const handleResetFilters = () => {
    setRoleSearch("");
    setRolePage(1);
  };

  const isFilterActive = roleSearch !== "";

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Target className="text-[#1877F2] shrink-0 h-8 w-8" />
              {t('targetRolesTitle2')}
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              {t('targetRolesDesc2')}
            </p>
          </div>

          <div className="flex items-center gap-2.5 w-full sm:w-auto">
            <Button
              variant="outline"
              onClick={() => setIsImportModalOpen(true)}
              className="h-10 border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 transition-colors cursor-pointer w-full sm:w-auto gap-2"
            >
              <UploadCloud className="h-4 w-4" />
              {t('importTargetRoleBtn')}
            </Button>
            <Button
              onClick={handleOpenRoleCreate}
              className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto"
            >
              <Plus className="h-4 w-4" />
              {t('addTargetRoleBtn')}
            </Button>
          </div>
        </div>

        {/* TẦNG 1: TOOLBAR (Search & Clear Filters) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-80 md:w-96">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={roleSearch}
                onChange={(e) => setRoleSearch(e.target.value)}
                placeholder={t('targetRolesSearchPlaceholder')}
                className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
              />
              {roleSearch && (
                <button
                  onClick={() => setRoleSearch("")}
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
                <RotateCcw className="h-3.5 w-3.5 mr-1.5" /> {t('clearFilters')}
              </Button>
            )}
          </div>
        </div>

        {/* TẦNG 2 & 3: TARGET ROLES TABLE & PAGINATION CONTAINER */}
        <TargetRolesTable
          roles={rolesData?.data?.items || []}
          isLoading={isRolesLoading}
          isError={isRolesError}
          totalItems={rolesData?.data?.total || 0}
          totalPages={rolesData?.data?.totalPages || 0}
          currentPage={rolePage}
          pageSize={rolePageSize}
          onPageSizeChange={setRolePageSize}
          onPageChange={setRolePage}
          onEdit={handleOpenRoleEdit}
          onDelete={handleRoleDeleteClick}
          onRetry={refetchRoles}
          isFilterActive={isFilterActive}
          onResetFilters={handleResetFilters}
        />
      </div>

      <TargetRoleModal
        isOpen={isRoleModalOpen}
        onClose={() => setIsRoleModalOpen(false)}
        mode={roleModalMode}
        initialData={selectedRole}
        onSuccess={(msg) => { showToast(msg, "success"); refetchRoles(); }}
      />
      <TargetRoleDeleteDialog
        isOpen={isRoleDeleteOpen}
        onClose={() => setIsRoleDeleteOpen(false)}
        roleToDelete={roleToDelete}
        onSuccess={(msg) => { showToast(msg, "success"); refetchRoles(); }}
        onError={(msg) => showToast(msg, "error")}
      />
      <ImportTargetRoleModal
        isOpen={isImportModalOpen}
        onClose={() => setIsImportModalOpen(false)}
        onSuccess={(msg) => { showToast(msg, "success"); refetchRoles(); }}
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
