"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Search, Plus, X, CheckCircle, RotateCcw, Edit2, Trash2 } from "lucide-react";
import { useSkills, useSkillCategories, useUpdateSkillStatus } from "@/hooks/useSkill";
import type { SkillDto, SkillStatus, SkillCategoryDto } from "@/types/master-data.types";
import { SkillModal } from "../components/SkillModal";
import { CategoryModal } from "../components/CategoryModal";
import { SkillDeleteDialog } from "../components/SkillDeleteDialog";
import { CategoryDeleteDialog } from "../components/CategoryDeleteDialog";
import { SkillForceStatusDialog } from "../components/SkillForceStatusDialog";
import { SkillsTable } from "../components/SkillsTable";

export default function SkillsPage() {
  const [skillSearch, setSkillSearch] = useState("");
  const [debouncedSkillSearch, setDebouncedSkillSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSkillSearch(skillSearch), 300);
    return () => clearTimeout(timer);
  }, [skillSearch]);

  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<SkillStatus | null>(null);
  const [skillPage, setSkillPage] = useState(1);
  const skillPageSize = 10;

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
    data: skillsData,
    isLoading: isSkillsLoading,
    isError: isSkillsError,
    refetch: refetchSkills,
  } = useSkills({
    page: skillPage,
    pageSize: skillPageSize,
    search: debouncedSkillSearch,
    categoryId: selectedCategoryId || undefined,
    status: selectedStatus || undefined,
  });

  const { data: categoriesData, isLoading: isCategoriesLoading } = useSkillCategories();
  const updateSkillStatusMutation = useUpdateSkillStatus();

  const [isCategoryModalOpen, setIsCategoryModalOpen] = useState(false);
  const [categoryModalMode, setCategoryModalMode] = useState<"create" | "edit">("create");
  const [selectedCategory, setSelectedCategory] = useState<SkillCategoryDto | null>(null);
  const [isCategoryDeleteOpen, setIsCategoryDeleteOpen] = useState(false);
  const [categoryToDelete, setCategoryToDelete] = useState<SkillCategoryDto | null>(null);

  const [isSkillModalOpen, setIsSkillModalOpen] = useState(false);
  const [skillModalMode, setSkillModalMode] = useState<"create" | "edit">("create");
  const [selectedSkill, setSelectedSkill] = useState<SkillDto | null>(null);
  const [isSkillDeleteOpen, setIsSkillDeleteOpen] = useState(false);
  const [skillToDelete, setSkillToDelete] = useState<SkillDto | null>(null);

  const [isForceStatusOpen, setIsForceStatusOpen] = useState(false);
  const [forceStatusData, setForceStatusData] = useState<{ id: number; status: SkillStatus; message: string; } | null>(null);

  useEffect(() => {
    setSkillPage(1);
  }, [debouncedSkillSearch, selectedCategoryId, selectedStatus]);

  const handleOpenCategoryCreate = useCallback(() => {
    setCategoryModalMode("create");
    setSelectedCategory(null);
    setIsCategoryModalOpen(true);
  }, []);

  const handleOpenCategoryEdit = useCallback((category: SkillCategoryDto) => {
    setCategoryModalMode("edit");
    setSelectedCategory(category);
    setIsCategoryModalOpen(true);
  }, []);

  const handleCategoryDeleteClick = useCallback((category: SkillCategoryDto) => {
    setCategoryToDelete(category);
    setIsCategoryDeleteOpen(true);
  }, []);

  const handleOpenSkillCreate = useCallback(() => {
    setSkillModalMode("create");
    setSelectedSkill(null);
    setIsSkillModalOpen(true);
  }, []);

  const handleOpenSkillEdit = useCallback((skill: SkillDto) => {
    setSkillModalMode("edit");
    setSelectedSkill(skill);
    setIsSkillModalOpen(true);
  }, []);

  const handleSkillDeleteClick = useCallback((skill: SkillDto) => {
    setSkillToDelete(skill);
    setIsSkillDeleteOpen(true);
  }, []);

  const handleSkillStatusToggle = useCallback((skill: SkillDto) => {
    const newStatus: SkillStatus = skill.status === "ACTIVE" ? "DEACTIVE" : "ACTIVE";
    updateSkillStatusMutation.mutate(
      { id: skill.id, dto: { status: newStatus, force: false } },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast(`Skill status updated to ${newStatus} successfully!`, "success");
          }
        },
        onError: (err: any) => {
          const apiMessage = err.response?.data?.message;
          if (apiMessage && (apiMessage.toLowerCase().includes("used") || apiMessage.toLowerCase().includes("deactivate"))) {
            setForceStatusData({ id: skill.id, status: newStatus, message: apiMessage });
            setIsForceStatusOpen(true);
          } else {
            showToast(apiMessage || "Error updating skill status.", "error");
          }
        },
      }
    );
  }, [updateSkillStatusMutation, showToast]);

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Skills Library</h1>
          <p className="text-sm text-muted-foreground mt-1">Manage the core skills and categories in the system.</p>
        </div>
        <button
          onClick={handleOpenSkillCreate}
          className="inline-flex items-center gap-1.5 px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors"
        >
          <Plus size={16} />
          <span>Add new skill</span>
        </button>
      </div>

      <div className="flex flex-col lg:flex-row gap-6 items-start">
        {/* Left Sidebar (Master) */}
        <div className="w-full lg:w-[30%] bg-card border border-border rounded-2xl shadow-2xs overflow-hidden flex flex-col h-[700px]">
          <div className="p-4 border-b border-border flex justify-between items-center bg-muted/10">
            <h2 className="text-sm font-semibold text-foreground uppercase tracking-wider">Skill Categories</h2>
            <button 
              onClick={handleOpenCategoryCreate}
              className="p-1.5 bg-primary/10 text-primary hover:bg-primary hover:text-primary-foreground rounded-lg transition-colors"
              title="Add Category"
            >
              <Plus size={16} />
            </button>
          </div>
          
          <div className="flex-1 overflow-y-auto p-2 space-y-1">
            <button
              onClick={() => setSelectedCategoryId(null)}
              className={`w-full flex items-center justify-between px-3 py-2.5 rounded-xl text-sm transition-all group ${
                selectedCategoryId === null
                  ? "bg-primary/10 text-primary font-semibold border-l-4 border-primary"
                  : "text-muted-foreground hover:bg-muted/50 hover:text-foreground border-l-4 border-transparent"
              }`}
            >
              <span>All Skills</span>
            </button>
            
            {isCategoriesLoading ? (
              <div className="p-4 text-center text-xs text-muted-foreground animate-pulse">Loading categories...</div>
            ) : (
              categoriesData?.data?.map((cat) => (
                <div
                  key={cat.id}
                  className={`group w-full flex items-center justify-between px-3 py-2.5 rounded-xl text-sm transition-all cursor-pointer ${
                    selectedCategoryId === cat.id
                      ? "bg-primary/10 text-primary font-semibold border-l-4 border-primary"
                      : "text-muted-foreground hover:bg-muted/50 hover:text-foreground border-l-4 border-transparent"
                  }`}
                  onClick={() => setSelectedCategoryId(cat.id)}
                >
                  <span className="truncate pr-2">{cat.name}</span>
                  <div className="flex items-center opacity-0 group-hover:opacity-100 transition-opacity gap-1">
                    <button 
                      onClick={(e) => { e.stopPropagation(); handleOpenCategoryEdit(cat); }}
                      className="p-1 text-muted-foreground hover:text-primary rounded"
                      title="Edit"
                    >
                      <Edit2 size={14} />
                    </button>
                    <button 
                      onClick={(e) => { e.stopPropagation(); handleCategoryDeleteClick(cat); }}
                      className="p-1 text-muted-foreground hover:text-destructive rounded"
                      title="Delete"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Main Content (Detail) */}
        <div className="w-full lg:w-[70%] space-y-4">
          <div className="bg-card border border-border rounded-2xl p-4 shadow-2xs flex flex-col sm:flex-row gap-4 items-stretch sm:items-center justify-between">
            <div>
              <h2 className="text-lg font-semibold text-foreground">
                {selectedCategoryId === null 
                  ? "All Skills" 
                  : `Skills in ${categoriesData?.data?.find(c => c.id === selectedCategoryId)?.name || "Category"}`}
              </h2>
              <p className="text-xs text-muted-foreground mt-0.5">Manage standard skills and aliases.</p>
            </div>
            
            <div className="flex flex-col sm:flex-row gap-3 items-center">
              <div className="relative w-full sm:w-64">
                <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Search skills..."
                  value={skillSearch}
                  onChange={(e) => setSkillSearch(e.target.value)}
                  className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
                />
                {skillSearch && (
                  <button onClick={() => setSkillSearch("")} className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground">
                    <X size={16} />
                  </button>
                )}
              </div>
              <button
                onClick={handleOpenSkillCreate}
                className="inline-flex items-center gap-1.5 px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors shrink-0"
              >
                <Plus size={16} />
                <span>Add Skill</span>
              </button>
            </div>
          </div>

          <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-2xs min-h-[590px]">
            <SkillsTable
              skills={skillsData?.data?.items || []}
              isLoading={isSkillsLoading}
              isError={isSkillsError}
              totalItems={skillsData?.data?.total || 0}
              totalPages={skillsData?.data?.totalPages || 0}
              currentPage={skillPage}
              pageSize={skillPageSize}
              onPageChange={setSkillPage}
              onEdit={handleOpenSkillEdit}
              onDelete={handleSkillDeleteClick}
              onStatusToggle={handleSkillStatusToggle}
              onRetry={refetchSkills}
            />
          </div>
        </div>
      </div>

      <SkillModal
        isOpen={isSkillModalOpen}
        onClose={() => setIsSkillModalOpen(false)}
        mode={skillModalMode}
        initialData={selectedSkill}
        categories={categoriesData?.data || []}
        onSuccess={(msg) => { showToast(msg, "success"); refetchSkills(); }}
      />
      <SkillDeleteDialog
        isOpen={isSkillDeleteOpen}
        onClose={() => setIsSkillDeleteOpen(false)}
        skillToDelete={skillToDelete}
        onSuccess={(msg) => { showToast(msg, "success"); refetchSkills(); }}
        onError={(msg) => showToast(msg, "error")}
      />
      <SkillForceStatusDialog
        isOpen={isForceStatusOpen}
        onClose={() => { setIsForceStatusOpen(false); setForceStatusData(null); refetchSkills(); }}
        forceStatusData={forceStatusData}
        onSuccess={(msg) => { showToast(msg, "success"); refetchSkills(); }}
        onError={(msg) => showToast(msg, "error")}
      />
      <CategoryModal
        isOpen={isCategoryModalOpen}
        onClose={() => setIsCategoryModalOpen(false)}
        mode={categoryModalMode}
        initialData={selectedCategory}
        onSuccess={(msg) => { showToast(msg, "success"); refetchSkills(); }}
      />
      <CategoryDeleteDialog
        isOpen={isCategoryDeleteOpen}
        onClose={() => setIsCategoryDeleteOpen(false)}
        categoryToDelete={categoryToDelete}
        onSuccess={(msg) => {
          showToast(msg, "success");
          if (selectedCategoryId === categoryToDelete?.id) setSelectedCategoryId(null);
          refetchSkills();
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
