"use client";

import React, { useState, useEffect, useMemo, useCallback } from "react";
import {
  Search,
  Plus,
  X,
  Database,
  Layers,
  CheckCircle,
  RotateCcw,
} from "lucide-react";
import {
  useSkills,
  useSkillCategories,
  useUpdateSkillStatus,
} from "@/hooks/useSkill";
import { useMajorTree, useRestoreMajor } from "@/hooks/useMajor";
import type {
  SkillDto,
  MajorDto,
  SkillStatus,
} from "@/types/master-data.types";
import { SkillModal } from "./components/SkillModal";
import { MajorModal } from "./components/MajorModal";
import { SkillDeleteDialog } from "./components/SkillDeleteDialog";
import { MajorDeleteDialog } from "./components/MajorDeleteDialog";
import { SkillForceStatusDialog } from "./components/SkillForceStatusDialog";
import { SkillsTable } from "./components/SkillsTable";
import { MajorsTable } from "./components/MajorsTable";

export default function MasterDataPage() {
  const [activeTab, setActiveTab] = useState<"skills" | "majors">("skills");

  // Search State with local Debounce
  const [skillSearch, setSkillSearch] = useState("");
  const [debouncedSkillSearch, setDebouncedSkillSearch] = useState("");
  const [majorSearch, setMajorSearch] = useState("");
  const [debouncedMajorSearch, setDebouncedMajorSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSkillSearch(skillSearch), 300);
    return () => clearTimeout(timer);
  }, [skillSearch]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedMajorSearch(majorSearch), 300);
    return () => clearTimeout(timer);
  }, [majorSearch]);

  // Skill Filters & Pagination State
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(
    null,
  );
  const [selectedStatus, setSelectedStatus] = useState<SkillStatus | null>(
    null,
  );
  const [skillPage, setSkillPage] = useState(1);
  const skillPageSize = 10;

  // Major Pagination State
  const [majorPage, setMajorPage] = useState(1);
  const majorPageSize = 10;

  // Toast State
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

  // Fetch Data - Skills
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

  const { data: categoriesData, isLoading: isCategoriesLoading } =
    useSkillCategories();

  // Fetch Data - Majors
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

  // Mutations
  const updateSkillStatusMutation = useUpdateSkillStatus();

  const restoreMajorMutation = useRestoreMajor();

  // Modals / Dialogs State - Skills
  const [isSkillModalOpen, setIsSkillModalOpen] = useState(false);
  const [skillModalMode, setSkillModalMode] = useState<"create" | "edit">(
    "create",
  );
  const [selectedSkill, setSelectedSkill] = useState<SkillDto | null>(null);
  const [isSkillDeleteOpen, setIsSkillDeleteOpen] = useState(false);
  const [skillToDelete, setSkillToDelete] = useState<SkillDto | null>(null);

  // Force Change Status Warning Dialog State
  const [isForceStatusOpen, setIsForceStatusOpen] = useState(false);
  const [forceStatusData, setForceStatusData] = useState<{
    id: number;
    status: SkillStatus;
    message: string;
  } | null>(null);

  // Modals / Dialogs State - Majors
  const [isMajorModalOpen, setIsMajorModalOpen] = useState(false);
  const [majorModalMode, setMajorModalMode] = useState<"create" | "edit">(
    "create",
  );
  const [selectedMajor, setSelectedMajor] = useState<MajorDto | null>(null);
  const [isMajorDeleteOpen, setIsMajorDeleteOpen] = useState(false);
  const [majorToDelete, setMajorToDelete] = useState<MajorDto | null>(null);

  // Reset pagination when filter/search changes
  useEffect(() => {
    setSkillPage(1);
  }, [debouncedSkillSearch, selectedCategoryId, selectedStatus]);

  useEffect(() => {
    setMajorPage(1);
  }, [debouncedMajorSearch]);

  // Skill Form Actions
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
    const newStatus: SkillStatus =
      skill.status === "ACTIVE" ? "DEACTIVE" : "ACTIVE";

    updateSkillStatusMutation.mutate(
      {
        id: skill.id,
        dto: {
          status: newStatus,
          force: false,
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast(
              `Skill status updated to ${newStatus} successfully!`,
              "success",
            );
          }
        },
        onError: (err: any) => {
          const apiMessage = err.response?.data?.message;
          if (
            apiMessage &&
            (apiMessage.toLowerCase().includes("used") ||
              apiMessage.toLowerCase().includes("deactivate"))
          ) {
            // Mở Dialog ép buộc đổi trạng thái
            setForceStatusData({
              id: skill.id,
              status: newStatus,
              message: apiMessage,
            });
            setIsForceStatusOpen(true);
          } else {
            showToast(apiMessage || "Error updating skill status.", "error");
          }
        },
      },
    );
  }, [updateSkillStatusMutation, showToast]);

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
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">
            Master Data Management
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage the Skills Library and Major List in the system.
          </p>
        </div>
        <button
          onClick={
            activeTab === "skills"
              ? handleOpenSkillCreate
              : handleOpenMajorCreate
          }
          className="inline-flex items-center gap-1.5 px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors"
        >
          <Plus size={16} />
          <span>Add new {activeTab === "skills" ? "skill" : "major"}</span>
        </button>
      </div>

      {/* Tabs Menu */}
      <div className="flex border-b border-border">
        <button
          onClick={() => setActiveTab("skills")}
          className={`flex items-center gap-2 px-6 py-3 border-b-2 text-sm font-medium transition-all ${
            activeTab === "skills"
              ? "border-primary text-primary bg-primary/5 rounded-t-lg"
              : "border-transparent text-muted-foreground hover:text-foreground hover:bg-muted/30"
          }`}
        >
          <Layers size={16} />
          <span>Skills Library</span>
        </button>
        <button
          onClick={() => setActiveTab("majors")}
          className={`flex items-center gap-2 px-6 py-3 border-b-2 text-sm font-medium transition-all ${
            activeTab === "majors"
              ? "border-primary text-primary bg-primary/5 rounded-t-lg"
              : "border-transparent text-muted-foreground hover:text-foreground hover:bg-muted/30"
          }`}
        >
          <Database size={16} />
          <span>Major List</span>
        </button>
      </div>

      {/* TAB CONTENT: SKILLS */}
      {activeTab === "skills" && (
        <div className="space-y-4">
          {/* Filters Bar */}
          <div className="bg-card border border-border rounded-2xl p-4 space-y-4 shadow-2xs">
            <div className="flex flex-col md:flex-row gap-4 items-stretch md:items-center">
              {/* Search */}
              <div className="relative flex-1">
                <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Search skills by name..."
                  value={skillSearch}
                  onChange={(e) => setSkillSearch(e.target.value)}
                  className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
                />
                {skillSearch && (
                  <button
                    onClick={() => setSkillSearch("")}
                    className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground"
                  >
                    <X size={16} />
                  </button>
                )}
              </div>

              {/* Status Select */}
              <div className="w-full md:w-48">
                <select
                  value={selectedStatus || ""}
                  onChange={(e) =>
                    setSelectedStatus((e.target.value as SkillStatus) || null)
                  }
                  className="w-full px-3 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                >
                  <option value="">All statuses</option>
                  <option value="ACTIVE">Active</option>
                  <option value="DEACTIVE">Disabled</option>
                </select>
              </div>
            </div>

            {/* Category Pills */}
            <div className="space-y-2">
              <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block">
                Categories
              </span>
              <div className="flex flex-wrap gap-2 max-h-32 overflow-y-auto pr-2">
                <button
                  onClick={() => setSelectedCategoryId(null)}
                  className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
                    selectedCategoryId === null
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted text-muted-foreground hover:bg-muted/80 hover:text-foreground"
                  }`}
                >
                  All
                </button>
                {categoriesData?.data?.map((cat) => (
                  <button
                    key={cat.id}
                    onClick={() => setSelectedCategoryId(cat.id)}
                    className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
                      selectedCategoryId === cat.id
                        ? "bg-primary text-primary-foreground"
                        : "bg-muted text-muted-foreground hover:bg-muted/80 hover:text-foreground"
                    }`}
                  >
                    {cat.name}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Table Container */}
          <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-2xs">
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
      )}

      {/* TAB CONTENT: MAJORS */}
      {activeTab === "majors" && (
        <div className="space-y-4">
          {/* Filters Bar */}
          <div className="bg-card border border-border rounded-2xl p-4 shadow-2xs flex gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search majors by name or code..."
                value={majorSearch}
                onChange={(e) => setMajorSearch(e.target.value)}
                className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
              />
              {majorSearch && (
                <button
                  onClick={() => setMajorSearch("")}
                  className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground"
                >
                  <X size={16} />
                </button>
              )}
            </div>
          </div>

          {/* Table Container */}
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
      )}

      {/* Modals & Dialogs */}
      <SkillModal
        isOpen={isSkillModalOpen}
        onClose={() => setIsSkillModalOpen(false)}
        mode={skillModalMode}
        initialData={selectedSkill}
        categories={categoriesData?.data || []}
        onSuccess={(msg) => {
          showToast(msg, "success");
          refetchSkills();
        }}
      />

      <SkillDeleteDialog
        isOpen={isSkillDeleteOpen}
        onClose={() => setIsSkillDeleteOpen(false)}
        skillToDelete={skillToDelete}
        onSuccess={(msg) => {
          showToast(msg, "success");
          refetchSkills();
        }}
        onError={(msg) => showToast(msg, "error")}
      />

      <SkillForceStatusDialog
        isOpen={isForceStatusOpen}
        onClose={() => {
          setIsForceStatusOpen(false);
          setForceStatusData(null);
          refetchSkills();
        }}
        forceStatusData={forceStatusData}
        onSuccess={(msg) => {
          showToast(msg, "success");
          refetchSkills();
        }}
        onError={(msg) => showToast(msg, "error")}
      />

      <MajorModal
        isOpen={isMajorModalOpen}
        onClose={() => setIsMajorModalOpen(false)}
        mode={majorModalMode}
        initialData={selectedMajor}
        onSuccess={(msg) => {
          showToast(msg, "success");
          refetchMajors();
        }}
      />

      <MajorDeleteDialog
        isOpen={isMajorDeleteOpen}
        onClose={() => setIsMajorDeleteOpen(false)}
        majorToDelete={majorToDelete}
        onSuccess={(msg, id, name) => {
          showToast(msg, "warning", () => {
            restoreMajorMutation.mutate(id, {
              onSuccess: (restoreRes) => {
                if (restoreRes.success) {
                  showToast(
                    `Major "${name}" restored successfully!`,
                    "success",
                  );
                  refetchMajors();
                }
              },
              onError: (restoreErr: any) => {
                showToast(
                  restoreErr.response?.data?.message ||
                    "Failed to restore major.",
                  "error",
                );
              },
            });
          });
          refetchMajors();
        }}
        onError={(msg) => showToast(msg, "error")}
      />

      {/* TOAST SYSTEM W/ UNDO */}
      {toast && (
        <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
          <div
            className={`flex items-center gap-3 px-4 py-3 rounded-2xl shadow-lg border text-sm font-medium ${
              toast.type === "success"
                ? "bg-emerald-500/10 text-emerald-500 border-emerald-500/25"
                : toast.type === "warning"
                  ? "bg-amber-500/10 text-amber-500 border-amber-500/25"
                  : "bg-destructive/10 text-destructive border-destructive/25"
            }`}
          >
            <CheckCircle size={18} className="shrink-0" />
            <div className="flex-1">
              <span>{toast.message}</span>
              {toast.undoAction && (
                <button
                  onClick={() => {
                    toast.undoAction?.();
                    setToast(null);
                  }}
                  className="ml-3 inline-flex items-center gap-1 text-xs font-bold underline hover:no-underline hover:opacity-90"
                >
                  <RotateCcw size={12} />
                  <span>Undo</span>
                </button>
              )}
            </div>
            <button
              onClick={() => setToast(null)}
              className="text-muted-foreground hover:text-foreground shrink-0 p-0.5 rounded-lg hover:bg-black/5"
            >
              <X size={14} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
