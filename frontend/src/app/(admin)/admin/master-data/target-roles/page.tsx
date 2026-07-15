"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Search, Plus, X, CheckCircle, RotateCcw } from "lucide-react";
import { usePagedTargetRoles } from "@/hooks/useTargetRole";
import type { TargetRoleTemplateDto } from "@/types/master-data.types";
import { TargetRoleModal } from "../components/TargetRoleModal";
import { TargetRoleDeleteDialog } from "../components/TargetRoleDeleteDialog";
import { TargetRolesTable } from "../components/TargetRolesTable";

export default function TargetRolesPage() {
  const [roleSearch, setRoleSearch] = useState("");
  const [debouncedRoleSearch, setDebouncedRoleSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedRoleSearch(roleSearch), 300);
    return () => clearTimeout(timer);
  }, [roleSearch]);

  const [rolePage, setRolePage] = useState(1);
  const rolePageSize = 10;

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

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Target Roles (Templates)</h1>
          <p className="text-sm text-muted-foreground mt-1">Manage standard target role templates.</p>
        </div>
        <button
          onClick={handleOpenRoleCreate}
          className="inline-flex items-center gap-1.5 px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors"
        >
          <Plus size={16} />
          <span>Add new target role</span>
        </button>
      </div>

      <div className="space-y-4">
        <div className="bg-card border border-border rounded-2xl p-4 shadow-2xs flex gap-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search target roles by name..."
              value={roleSearch}
              onChange={(e) => setRoleSearch(e.target.value)}
              className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
            />
            {roleSearch && (
              <button onClick={() => setRoleSearch("")} className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground">
                <X size={16} />
              </button>
            )}
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-2xs">
          <TargetRolesTable
            roles={rolesData?.data?.items || []}
            isLoading={isRolesLoading}
            isError={isRolesError}
            totalItems={rolesData?.data?.total || 0}
            totalPages={rolesData?.data?.totalPages || 0}
            currentPage={rolePage}
            pageSize={rolePageSize}
            onPageChange={setRolePage}
            onEdit={handleOpenRoleEdit}
            onDelete={handleRoleDeleteClick}
            onRetry={refetchRoles}
          />
        </div>
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
