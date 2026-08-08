"use client";

import React, { useState, useEffect } from "react";
import { Loader2, XCircle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useCreateSkillCategory, useUpdateSkillCategory } from "@/hooks/useSkill";
import type { SkillCategoryDto } from "@/types/master-data.types";
import { useTranslations } from 'next-intl';

interface CategoryModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: "create" | "edit";
  initialData: SkillCategoryDto | null;
  onSuccess: (message: string) => void;
}

export function CategoryModal({
  isOpen,
  onClose,
  mode,
  initialData,
  onSuccess,
}: CategoryModalProps) {
  const t = useTranslations('AdminMasterData');
  const [name, setName] = useState("");
  const [error, setError] = useState("");

  const createMutation = useCreateSkillCategory();
  const updateMutation = useUpdateSkillCategory();

  useEffect(() => {
    if (isOpen) {
      if (mode === "edit" && initialData) {
        setName(initialData.name);
      } else {
        setName("");
      }
      setError("");
    }
  }, [isOpen, mode, initialData]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!name.trim()) {
      setError("Category name cannot be empty.");
      return;
    }

    if (mode === "create") {
      createMutation.mutate(
        { name: name.trim() },
        {
          onSuccess: (res) => {
            if (res.success) {
              onSuccess("Category created successfully!");
              onClose();
            } else {
              setError(res.message || "An error occurred.");
            }
          },
          onError: (err: any) => {
            setError(err.response?.data?.message || "Failed to create category.");
          },
        }
      );
    } else if (mode === "edit" && initialData) {
      updateMutation.mutate(
        { id: initialData.id, dto: { name: name.trim() } },
        {
          onSuccess: (res) => {
            if (res.success) {
              onSuccess("Category updated successfully!");
              onClose();
            } else {
              setError(res.message || "An error occurred.");
            }
          },
          onError: (err: any) => {
            setError(err.response?.data?.message || "Failed to update category.");
          },
        }
      );
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {mode === "create" ? t('addCategory') : t('editCategory')}
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          {error && (
            <div className="p-3 bg-destructive/10 text-destructive text-xs font-medium rounded-xl border border-destructive/20 flex items-center gap-2">
              <XCircle size={16} className="shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-bold text-foreground">Category Name</label>
            <input
              type="text"
              placeholder="e.g. Programming Languages..."
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60"
              required
            />
          </div>

          <div className="flex justify-end gap-3 pt-3 border-t border-border/80">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
            >
              {t('cancelBtn')}
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
            >
              {isPending && <Loader2 size={14} className="animate-spin" />}
              <span>{t('saveBtn')}</span>
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
