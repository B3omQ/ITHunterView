'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Search,
  Plus,
  X,
  CheckCircle,
  RotateCcw,
  Edit2,
  Trash2,
  Sparkles,
} from 'lucide-react';
import { useSkills, useSkillCategories, useUpdateSkillStatus } from '@/hooks/useSkill';
import type { SkillDto, SkillStatus, SkillCategoryDto } from '@/types/master-data.types';
import { SkillModal } from '../components/SkillModal';
import { CategoryModal } from '../components/CategoryModal';
import { SkillDeleteDialog } from '../components/SkillDeleteDialog';
import { CategoryDeleteDialog } from '../components/CategoryDeleteDialog';
import { SkillForceStatusDialog } from '../components/SkillForceStatusDialog';
import { SkillsTable } from '../components/SkillsTable';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useTranslations } from 'next-intl';

export default function SkillsPage() {
  const t = useTranslations('AdminMasterData');
  const [skillSearch, setSkillSearch] = useState('');
  const [debouncedSkillSearch, setDebouncedSkillSearch] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSkillSearch(skillSearch), 350);
    return () => clearTimeout(timer);
  }, [skillSearch]);

  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<SkillStatus | null>(null);
  const [skillPage, setSkillPage] = useState(1);
  const skillPageSize = 10;

  const [toast, setToast] = useState<{
    message: string;
    type: 'success' | 'error' | 'warning';
    undoAction?: () => void;
  } | null>(null);

  const showToast = useCallback(
    (
      message: string,
      type: 'success' | 'error' | 'warning' = 'success',
      undoAction?: () => void
    ) => {
      setToast({ message, type, undoAction });
    },
    []
  );

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
  const [categoryModalMode, setCategoryModalMode] = useState<'create' | 'edit'>('create');
  const [selectedCategory, setSelectedCategory] = useState<SkillCategoryDto | null>(null);
  const [isCategoryDeleteOpen, setIsCategoryDeleteOpen] = useState(false);
  const [categoryToDelete, setCategoryToDelete] = useState<SkillCategoryDto | null>(null);

  const [isSkillModalOpen, setIsSkillModalOpen] = useState(false);
  const [skillModalMode, setSkillModalMode] = useState<'create' | 'edit'>('create');
  const [selectedSkill, setSelectedSkill] = useState<SkillDto | null>(null);
  const [isSkillDeleteOpen, setIsSkillDeleteOpen] = useState(false);
  const [skillToDelete, setSkillToDelete] = useState<SkillDto | null>(null);

  const [isForceStatusOpen, setIsForceStatusOpen] = useState(false);
  const [forceStatusData, setForceStatusData] = useState<{
    id: number;
    status: SkillStatus;
    message: string;
  } | null>(null);

  useEffect(() => {
    setSkillPage(1);
  }, [debouncedSkillSearch, selectedCategoryId, selectedStatus]);

  const handleOpenCategoryCreate = useCallback(() => {
    setCategoryModalMode('create');
    setSelectedCategory(null);
    setIsCategoryModalOpen(true);
  }, []);

  const handleOpenCategoryEdit = useCallback((category: SkillCategoryDto) => {
    setCategoryModalMode('edit');
    setSelectedCategory(category);
    setIsCategoryModalOpen(true);
  }, []);

  const handleCategoryDeleteClick = useCallback((category: SkillCategoryDto) => {
    setCategoryToDelete(category);
    setIsCategoryDeleteOpen(true);
  }, []);

  const handleOpenSkillCreate = useCallback(() => {
    setSkillModalMode('create');
    setSelectedSkill(null);
    setIsSkillModalOpen(true);
  }, []);

  const handleOpenSkillEdit = useCallback((skill: SkillDto) => {
    setSkillModalMode('edit');
    setSelectedSkill(skill);
    setIsSkillModalOpen(true);
  }, []);

  const handleSkillDeleteClick = useCallback((skill: SkillDto) => {
    setSkillToDelete(skill);
    setIsSkillDeleteOpen(true);
  }, []);

  const handleSkillStatusToggle = useCallback(
    (skill: SkillDto) => {
      const newStatus: SkillStatus = skill.status === 'ACTIVE' ? 'DEACTIVE' : 'ACTIVE';
      updateSkillStatusMutation.mutate(
        { id: skill.id, dto: { status: newStatus, force: false } },
        {
          onSuccess: (res) => {
            if (res.success) {
              showToast(`Skill status updated to ${newStatus} successfully!`, 'success');
            }
          },
          onError: (err: any) => {
            const apiMessage = err.response?.data?.message;
            if (
              apiMessage &&
              (apiMessage.toLowerCase().includes('used') ||
                apiMessage.toLowerCase().includes('deactivate'))
            ) {
              setForceStatusData({ id: skill.id, status: newStatus, message: apiMessage });
              setIsForceStatusOpen(true);
            } else {
              showToast(apiMessage || 'Error updating skill status.', 'error');
            }
          },
        }
      );
    },
    [updateSkillStatusMutation, showToast]
  );

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Sparkles className="text-[#1877F2] shrink-0 h-8 w-8" />
              {t('skillsTitle')}
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              {t('skillsDesc')}
            </p>
          </div>

          <Button
            onClick={handleOpenSkillCreate}
            className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto"
          >
            <Plus className="h-4 w-4" />
            {t('addSkill')}
          </Button>
        </div>

        {/* Layout: Master-Detail Split Container */}
        <div className="flex flex-col lg:flex-row gap-6 items-start">
          {/* Left Sidebar (Master: Skill Categories) */}
          <div className="w-full lg:w-[28%] bg-white dark:bg-zinc-900 border border-[#CED0D4] dark:border-zinc-800 rounded-lg shadow-2xs overflow-hidden flex flex-col h-[680px]">
            <div className="p-3.5 border-b border-[#CED0D4] dark:border-zinc-800 flex justify-between items-center bg-slate-50 dark:bg-zinc-950">
              <h2 className="text-xs font-bold text-[#050505] dark:text-zinc-100 uppercase tracking-wider">
                Skill Categories
              </h2>
              <Button
                onClick={handleOpenCategoryCreate}
                variant="ghost"
                size="icon-sm"
                className="h-7 w-7 text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                title={t('addCategory')}
              >
                <Plus className="h-4 w-4" />
              </Button>
            </div>

            <div className="flex-1 overflow-y-auto p-2 space-y-1">
              <button
                onClick={() => setSelectedCategoryId(null)}
                className={`w-full flex items-center justify-between px-3 py-2.5 rounded-md text-xs font-semibold transition-all cursor-pointer ${
                  selectedCategoryId === null
                    ? 'bg-[#E7F3FF] dark:bg-blue-950/50 text-[#1877F2] dark:text-blue-400 border-l-4 border-[#1877F2]'
                    : 'text-[#050505] dark:text-zinc-300 hover:bg-slate-50 dark:hover:bg-zinc-800/60 border-l-4 border-transparent'
                }`}
              >
                <span>All Skills</span>
              </button>

              {isCategoriesLoading ? (
                <div className="p-4 text-center text-xs text-[#65676B] animate-pulse">
                  Loading categories...
                </div>
              ) : (
                categoriesData?.data?.map((cat) => {
                  const isSelected = selectedCategoryId === cat.id;
                  return (
                    <div
                      key={cat.id}
                      className={`group w-full flex items-center justify-between px-3 py-2.5 rounded-md text-xs transition-all cursor-pointer ${
                        isSelected
                          ? 'bg-[#E7F3FF] dark:bg-blue-950/50 text-[#1877F2] dark:text-blue-400 font-bold border-l-4 border-[#1877F2]'
                          : 'text-[#050505] dark:text-zinc-300 hover:bg-slate-50 dark:hover:bg-zinc-800/60 border-l-4 border-transparent'
                      }`}
                      onClick={() => setSelectedCategoryId(cat.id)}
                    >
                      <span className="truncate pr-2">{cat.name}</span>
                      <div className="flex items-center opacity-0 group-hover:opacity-100 transition-opacity gap-1">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleOpenCategoryEdit(cat);
                          }}
                          className="p-1 text-[#65676B] hover:text-[#1877F2] rounded cursor-pointer"
                          title={t('editCategory')}
                        >
                          <Edit2 className="h-3.5 w-3.5" />
                        </button>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleCategoryDeleteClick(cat);
                          }}
                          className="p-1 text-[#65676B] hover:text-rose-600 rounded cursor-pointer"
                          title={t('deleteCategory')}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </div>

          {/* Right Main Content (Detail: Skills Grid Table) */}
          <div className="w-full lg:w-[72%] space-y-4">
            <div className="flex flex-col sm:flex-row gap-3 items-center justify-between">
              <div className="relative w-full sm:w-80">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
                <Input
                  type="text"
                  placeholder={t('searchPlaceholder')}
                  value={skillSearch}
                  onChange={(e) => setSkillSearch(e.target.value)}
                  className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
                />
                {skillSearch && (
                  <button
                    onClick={() => setSkillSearch('')}
                    className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1 cursor-pointer"
                    title="Clear search"
                  >
                    <X className="h-4 w-4" />
                  </button>
                )}
              </div>

              <span className="text-xs font-semibold text-[#65676B] dark:text-zinc-400">
                {selectedCategoryId === null
                  ? 'Showing all categories'
                  : `Category: ${
                      categoriesData?.data?.find((c) => c.id === selectedCategoryId)?.name ||
                      'Selected'
                    }`}
              </span>
            </div>

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

        {/* Modals */}
        <SkillModal
          isOpen={isSkillModalOpen}
          onClose={() => setIsSkillModalOpen(false)}
          mode={skillModalMode}
          initialData={selectedSkill}
          categories={categoriesData?.data || []}
          onSuccess={(msg) => {
            showToast(msg, 'success');
            refetchSkills();
          }}
        />
        <SkillDeleteDialog
          isOpen={isSkillDeleteOpen}
          onClose={() => setIsSkillDeleteOpen(false)}
          skillToDelete={skillToDelete}
          onSuccess={(msg) => {
            showToast(msg, 'success');
            refetchSkills();
          }}
          onError={(msg) => showToast(msg, 'error')}
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
            showToast(msg, 'success');
            refetchSkills();
          }}
          onError={(msg) => showToast(msg, 'error')}
        />
        <CategoryModal
          isOpen={isCategoryModalOpen}
          onClose={() => setIsCategoryModalOpen(false)}
          mode={categoryModalMode}
          initialData={selectedCategory}
          onSuccess={(msg) => {
            showToast(msg, 'success');
            refetchSkills();
          }}
        />
        <CategoryDeleteDialog
          isOpen={isCategoryDeleteOpen}
          onClose={() => setIsCategoryDeleteOpen(false)}
          categoryToDelete={categoryToDelete}
          onSuccess={(msg) => {
            showToast(msg, 'success');
            if (selectedCategoryId === categoryToDelete?.id) setSelectedCategoryId(null);
            refetchSkills();
          }}
          onError={(msg) => showToast(msg, 'error')}
        />

        {/* Toast */}
        {toast && (
          <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
            <div
              className={`flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg border text-sm font-medium ${
                toast.type === 'success'
                  ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800'
                  : toast.type === 'warning'
                  ? 'bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border-amber-200 dark:border-amber-800'
                  : 'bg-rose-50 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border-rose-200 dark:border-rose-800'
              }`}
            >
              <CheckCircle className="h-4 w-4 shrink-0" />
              <div className="flex-1">
                <span>{toast.message}</span>
                {toast.undoAction && (
                  <button
                    onClick={() => {
                      toast.undoAction?.();
                      setToast(null);
                    }}
                    className="ml-3 inline-flex items-center gap-1 text-xs font-bold underline hover:no-underline hover:opacity-90 cursor-pointer"
                  >
                    <RotateCcw className="h-3 w-3" />
                    <span>Undo</span>
                  </button>
                )}
              </div>
              <button
                onClick={() => setToast(null)}
                className="text-[#65676B] hover:text-[#050505] shrink-0 p-0.5 rounded cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
