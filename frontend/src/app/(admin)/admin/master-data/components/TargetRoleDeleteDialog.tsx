import React from "react";
import { AlertTriangle, Trash2, X } from "lucide-react";
import { TargetRoleTemplateDto } from "@/types/master-data.types";
import { useDeleteTargetRole } from "@/hooks/useTargetRole";

interface TargetRoleDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  roleToDelete: TargetRoleTemplateDto | null;
  onSuccess: (message: string) => void;
  onError: (message: string) => void;
}

export function TargetRoleDeleteDialog({
  isOpen,
  onClose,
  roleToDelete,
  onSuccess,
  onError,
}: TargetRoleDeleteDialogProps) {
  const deleteMutation = useDeleteTargetRole();

  if (!isOpen || !roleToDelete) return null;

  const handleDelete = () => {
    deleteMutation.mutate(roleToDelete.id, {
      onSuccess: () => {
        onSuccess(`Role "${roleToDelete.roleName}" has been deleted.`);
        onClose();
      },
      onError: (error: any) => {
        onError(error.response?.data?.message || "Failed to delete target role.");
        onClose();
      },
    });
  };

  const isPending = deleteMutation.isPending;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm transition-opacity"
        onClick={!isPending ? onClose : undefined}
      />
      
      <div className="relative bg-card rounded-2xl shadow-xl w-full max-w-md flex flex-col overflow-hidden animate-in zoom-in-95 duration-200 border border-border">
        <div className="flex items-center justify-between px-5 py-4 border-b border-border bg-destructive/5">
          <div className="flex items-center gap-2 text-destructive">
            <AlertTriangle size={18} />
            <h2 className="text-base font-bold">Delete Target Role</h2>
          </div>
          <button
            onClick={onClose}
            disabled={isPending}
            className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors disabled:opacity-50"
          >
            <X size={18} />
          </button>
        </div>

        <div className="p-6 space-y-4">
          <p className="text-sm text-foreground">
            Are you sure you want to delete the target role <span className="font-bold">"{roleToDelete.roleName}"</span>?
          </p>
          <p className="text-xs text-muted-foreground">
            This action cannot be undone. All required skills mapped to this role will also be removed.
            This may affect candidate learning paths that were generated using this template.
          </p>
        </div>

        <div className="flex items-center justify-end gap-3 px-5 py-4 border-t border-border bg-muted/10">
          <button
            onClick={onClose}
            disabled={isPending}
            className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground bg-transparent hover:bg-muted rounded-xl transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            onClick={handleDelete}
            disabled={isPending}
            className="inline-flex items-center gap-2 px-5 py-2 text-sm font-semibold text-destructive-foreground bg-destructive hover:bg-destructive/90 rounded-xl shadow-xs transition-colors disabled:opacity-50"
          >
            {isPending ? (
              <div className="h-4 w-4 rounded-full border-2 border-destructive-foreground/30 border-t-destructive-foreground animate-spin" />
            ) : (
              <Trash2 size={16} />
            )}
            <span>Delete Role</span>
          </button>
        </div>
      </div>
    </div>
  );
}
