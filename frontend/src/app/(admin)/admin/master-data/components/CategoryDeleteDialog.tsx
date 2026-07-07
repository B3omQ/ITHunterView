"use client";

import React from "react";
import { Loader2, AlertTriangle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useDeleteSkillCategory } from "@/hooks/useSkill";
import type { SkillCategoryDto } from "@/types/master-data.types";

interface CategoryDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  categoryToDelete: SkillCategoryDto | null;
  onSuccess: (message: string) => void;
  onError: (message: string) => void;
}

export function CategoryDeleteDialog({
  isOpen,
  onClose,
  categoryToDelete,
  onSuccess,
  onError,
}: CategoryDeleteDialogProps) {
  const deleteMutation = useDeleteSkillCategory();

  const handleConfirm = () => {
    if (!categoryToDelete) return;

    deleteMutation.mutate(categoryToDelete.id, {
      onSuccess: (res) => {
        if (res.success) {
          onSuccess("Category deleted successfully!");
          onClose();
        } else {
          onError(res.message || "Failed to delete category.");
          onClose();
        }
      },
      onError: (err: any) => {
        onError(err.response?.data?.message || "Failed to delete category.");
        onClose();
      },
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-destructive">
            <AlertTriangle size={20} />
            <span>Confirm Deletion</span>
          </DialogTitle>
        </DialogHeader>

        <div className="py-4 text-sm text-muted-foreground">
          <p>
            Are you sure you want to delete the category{" "}
            <strong className="text-foreground">
              "{categoryToDelete?.name}"
            </strong>
            ?
          </p>
          <p className="mt-2">
            Skills belonging to this category will become "Uncategorized".
          </p>
        </div>

        <div className="flex justify-end gap-3 pt-3 border-t border-border/80">
          <button
            onClick={onClose}
            disabled={deleteMutation.isPending}
            className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={deleteMutation.isPending}
            className="px-4 py-2 bg-destructive hover:bg-destructive/90 text-destructive-foreground font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
          >
            {deleteMutation.isPending && (
              <Loader2 size={14} className="animate-spin" />
            )}
            <span>Yes, Delete</span>
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
