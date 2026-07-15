"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Search, Plus, X, CheckCircle, UploadCloud } from "lucide-react";
import { useSfiaSkills, useDeleteSfiaSkill } from "@/hooks/useSfiaSkill";
import type { SfiaSkillDto } from "@/types/master-data.types";
import { SfiaSkillsTable } from "../components/SfiaSkillsTable";
import { SfiaSkillModal } from "../components/SfiaSkillModal";
import { ImportSfiaSkillModal } from "../components/ImportSfiaSkillModal";

export default function SfiaSkillsPage() {
  const [skillSearch, setSkillSearch] = useState("");
  const [debouncedSkillSearch, setDebouncedSkillSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSkillSearch(skillSearch), 300);
    return () => clearTimeout(timer);
  }, [skillSearch]);

  const [skillPage, setSkillPage] = useState(1);
  const skillPageSize = 10;

  const [toast, setToast] = useState<{
    message: string;
    type: "success" | "error";
  } | null>(null);

  const showToast = useCallback((
    message: string,
    type: "success" | "error" = "success",
  ) => {
    setToast({ message, type });
  }, []);

  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 6000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  const {
    data: sfiaSkillsData,
    isLoading,
    isError,
    refetch,
  } = useSfiaSkills(skillPage, skillPageSize, debouncedSkillSearch);

  const deleteMutation = useDeleteSfiaSkill();

  const [isSkillModalOpen, setIsSkillModalOpen] = useState(false);
  const [skillModalMode, setSkillModalMode] = useState<"create" | "edit">("create");
  const [selectedSkill, setSelectedSkill] = useState<SfiaSkillDto | null>(null);

  const [isImportModalOpen, setIsImportModalOpen] = useState(false);

  useEffect(() => {
    setSkillPage(1);
  }, [debouncedSkillSearch]);

  const handleOpenCreate = useCallback(() => {
    setSkillModalMode("create");
    setSelectedSkill(null);
    setIsSkillModalOpen(true);
  }, []);

  const handleOpenEdit = useCallback((skill: SfiaSkillDto) => {
    setSkillModalMode("edit");
    setSelectedSkill(skill);
    setIsSkillModalOpen(true);
  }, []);

  const handleDelete = useCallback((skill: SfiaSkillDto) => {
    if (confirm(`Are you sure you want to delete the SFIA Skill "${skill.skillCode}"?`)) {
      deleteMutation.mutate(skill.id, {
        onSuccess: (res) => {
          if (res.success) {
            showToast("SFIA Skill deleted successfully", "success");
          } else {
            showToast(res.message || "Failed to delete skill", "error");
          }
        },
        onError: (err: any) => {
          showToast(err.response?.data?.message || "Failed to delete skill. It might be in use.", "error");
        }
      });
    }
  }, [deleteMutation, showToast]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">SFIA Skills</h1>
          <p className="text-sm text-muted-foreground mt-1">Manage standard SFIA 9 skills used for Target Roles and Learning Paths.</p>
        </div>
        <div className="flex items-center gap-3 w-full sm:w-auto">
          <button
            onClick={() => setIsImportModalOpen(true)}
            className="flex-1 sm:flex-none inline-flex items-center justify-center gap-1.5 px-4 py-2 bg-secondary hover:bg-secondary/80 text-secondary-foreground font-medium text-sm rounded-xl transition-colors"
          >
            <UploadCloud size={16} />
            <span>Import CSV</span>
          </button>
          <button
            onClick={handleOpenCreate}
            className="flex-1 sm:flex-none inline-flex items-center justify-center gap-1.5 px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors"
          >
            <Plus size={16} />
            <span>Add new</span>
          </button>
        </div>
      </div>

      <div className="bg-card border border-border rounded-2xl shadow-2xs overflow-hidden flex flex-col h-[700px]">
        <div className="p-4 border-b border-border flex flex-col sm:flex-row gap-4 items-stretch sm:items-center justify-between bg-muted/5">
          <div className="relative w-full sm:max-w-md">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search by code, name, or category..."
              value={skillSearch}
              onChange={(e) => setSkillSearch(e.target.value)}
              className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
            />
            {skillSearch && (
              <button onClick={() => setSkillSearch("")} className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground">
                <X size={16} />
              </button>
            )}
          </div>
          <div className="text-sm text-muted-foreground font-medium px-2">
            Total: {sfiaSkillsData?.data?.totalItems || 0} skills
          </div>
        </div>
        
        <div className="flex-1 overflow-hidden relative">
          <SfiaSkillsTable
            skills={sfiaSkillsData?.data?.items || []}
            isLoading={isLoading}
            isError={isError}
            totalItems={sfiaSkillsData?.data?.totalItems || 0}
            totalPages={sfiaSkillsData?.data?.totalPages || 0}
            currentPage={skillPage}
            pageSize={skillPageSize}
            onPageChange={setSkillPage}
            onEdit={handleOpenEdit}
            onDelete={handleDelete}
            onRetry={refetch}
          />
        </div>
      </div>

      <SfiaSkillModal
        isOpen={isSkillModalOpen}
        onClose={() => setIsSkillModalOpen(false)}
        mode={skillModalMode}
        initialData={selectedSkill}
        onSuccess={(msg) => showToast(msg, "success")}
      />
      
      <ImportSfiaSkillModal
        isOpen={isImportModalOpen}
        onClose={() => setIsImportModalOpen(false)}
        onSuccess={(msg) => showToast(msg, "success")}
      />

      {toast && (
        <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
          <div className={`flex items-center gap-3 px-4 py-3 rounded-2xl shadow-lg border text-sm font-medium ${toast.type === "success" ? "bg-emerald-500/10 text-emerald-500 border-emerald-500/25" : "bg-destructive/10 text-destructive border-destructive/25"}`}>
            <CheckCircle size={18} className="shrink-0" />
            <div className="flex-1">
              <span>{toast.message}</span>
            </div>
            <button onClick={() => setToast(null)} className="text-muted-foreground hover:text-foreground shrink-0 p-0.5 rounded-lg hover:bg-black/5"><X size={14} /></button>
          </div>
        </div>
      )}
    </div>
  );
}
