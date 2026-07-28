'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Search,
  Plus,
  X,
  CheckCircle,
  UploadCloud,
  Boxes,
  RotateCcw,
} from 'lucide-react';
import { useSfiaSkills, useDeleteSfiaSkill } from '@/hooks/useSfiaSkill';
import type { SfiaSkillDto } from '@/types/master-data.types';
import { SfiaSkillsTable } from '../components/SfiaSkillsTable';
import { SfiaSkillModal } from '../components/SfiaSkillModal';
import { ImportSfiaSkillModal } from '../components/ImportSfiaSkillModal';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

export default function SfiaSkillsPage() {
  const [skillSearch, setSkillSearch] = useState('');
  const [debouncedSkillSearch, setDebouncedSkillSearch] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSkillSearch(skillSearch), 350);
    return () => clearTimeout(timer);
  }, [skillSearch]);

  const [toast, setToast] = useState<{
    message: string;
    type: 'success' | 'error';
  } | null>(null);

  const showToast = useCallback(
    (message: string, type: 'success' | 'error' = 'success') => {
      setToast({ message, type });
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
    data: sfiaSkillsData,
    isLoading,
    isError,
    refetch,
  } = useSfiaSkills(debouncedSkillSearch);

  const deleteMutation = useDeleteSfiaSkill();

  const [isSkillModalOpen, setIsSkillModalOpen] = useState(false);
  const [skillModalMode, setSkillModalMode] = useState<'create' | 'edit'>('create');
  const [selectedSkill, setSelectedSkill] = useState<SfiaSkillDto | null>(null);

  const [isImportModalOpen, setIsImportModalOpen] = useState(false);

  const handleOpenCreate = useCallback(() => {
    setSkillModalMode('create');
    setSelectedSkill(null);
    setIsSkillModalOpen(true);
  }, []);

  const handleOpenEdit = useCallback((skill: SfiaSkillDto) => {
    setSkillModalMode('edit');
    setSelectedSkill(skill);
    setIsSkillModalOpen(true);
  }, []);

  const handleDelete = useCallback(
    (skill: SfiaSkillDto) => {
      if (confirm(`Are you sure you want to delete the SFIA Skill "${skill.skillCode}"?`)) {
        deleteMutation.mutate(skill.id, {
          onSuccess: (res) => {
            if (res.success) {
              showToast('SFIA Skill deleted successfully', 'success');
            } else {
              showToast(res.message || 'Failed to delete skill', 'error');
            }
          },
          onError: (err: any) => {
            showToast(
              err.response?.data?.message || 'Failed to delete skill. It might be in use.',
              'error'
            );
          },
        });
      }
    },
    [deleteMutation, showToast]
  );

  const handleResetFilters = () => {
    setSkillSearch('');
  };

  const isFilterActive = skillSearch !== '';

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Boxes className="text-[#1877F2] shrink-0 h-8 w-8" />
              SFIA 9 Skills Framework
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Manage standard SFIA 9 skills taxonomy and responsibility level mappings across target roles.
            </p>
          </div>

          <div className="flex items-center gap-3 w-full sm:w-auto">
            <Button
              variant="outline"
              onClick={() => setIsImportModalOpen(true)}
              className="border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-200 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 font-medium h-10 px-4 rounded-lg cursor-pointer gap-2 flex-1 sm:flex-none"
            >
              <UploadCloud className="h-4 w-4 text-[#1877F2]" />
              Import CSV
            </Button>
            <Button
              onClick={handleOpenCreate}
              className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer flex-1 sm:flex-none"
            >
              <Plus className="h-4 w-4" />
              Add SFIA Skill
            </Button>
          </div>
        </div>

        {/* TẦNG 1: TOOLBAR (Search & Actions) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-80 md:w-96">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
              <Input
                value={skillSearch}
                onChange={(e) => setSkillSearch(e.target.value)}
                placeholder="Search by code, name, category, or subcategory..."
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

          <div className="text-xs font-semibold text-[#65676B] dark:text-zinc-400">
            Total: <span className="text-[#050505] dark:text-zinc-100 font-bold">{sfiaSkillsData?.data?.length || 0}</span> SFIA skills
          </div>
        </div>

        {/* TẦNG 2 & 3: MAIN TABLE CONTAINER */}
        <SfiaSkillsTable
          skills={sfiaSkillsData?.data || []}
          isLoading={isLoading}
          isError={isError}
          onEdit={handleOpenEdit}
          onDelete={handleDelete}
          onRetry={refetch}
        />

        {/* Modals */}
        <SfiaSkillModal
          isOpen={isSkillModalOpen}
          onClose={() => setIsSkillModalOpen(false)}
          mode={skillModalMode}
          initialData={selectedSkill}
          onSuccess={(msg) => showToast(msg, 'success')}
        />

        <ImportSfiaSkillModal
          isOpen={isImportModalOpen}
          onClose={() => setIsImportModalOpen(false)}
          onSuccess={(msg) => showToast(msg, 'success')}
        />

        {/* Toast */}
        {toast && (
          <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
            <div
              className={`flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg border text-sm font-medium ${
                toast.type === 'success'
                  ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800'
                  : 'bg-rose-50 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border-rose-200 dark:border-rose-800'
              }`}
            >
              <CheckCircle className="h-4 w-4 shrink-0" />
              <div className="flex-1">
                <span>{toast.message}</span>
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
