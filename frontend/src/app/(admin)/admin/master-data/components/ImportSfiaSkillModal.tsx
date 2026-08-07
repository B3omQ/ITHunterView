"use client";

import React, { useState, useRef, useEffect } from "react";
import { X, UploadCloud, AlertCircle, FileText } from "lucide-react";
import { useImportSfiaSkills } from "@/hooks/useSfiaSkill";
import { useTranslations } from 'next-intl';

interface ImportSfiaSkillModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (msg: string) => void;
}

export function ImportSfiaSkillModal({
  isOpen,
  onClose,
  onSuccess,
}: ImportSfiaSkillModalProps) {
  const t = useTranslations('AdminMasterData');
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState("");
  const fileInputRef = useRef<HTMLInputElement>(null);
  const importMutation = useImportSfiaSkills();

  useEffect(() => {
    if (isOpen) {
      setFile(null);
      setError("");
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const isPending = importMutation.isPending;

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const selectedFile = e.target.files[0];
      if (selectedFile.type !== "text/csv" && !selectedFile.name.endsWith(".csv")) {
        setError("Please upload a valid CSV file.");
        setFile(null);
        return;
      }
      setFile(selectedFile);
      setError("");
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setError("Please select a file to import.");
      return;
    }

    importMutation.mutate(file, {
      onSuccess: (res) => {
        if (res.success) {
          onSuccess(res.message || "Import completed successfully");
          onClose();
        } else {
          setError(res.message || "Failed to import skills");
        }
      },
      onError: (err: any) => {
        setError(err.response?.data?.message || "An error occurred during import");
      },
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-card w-full max-w-lg rounded-2xl shadow-xl overflow-hidden border border-border flex flex-col">
        <div className="px-6 py-4 border-b border-border flex justify-between items-center bg-muted/10 shrink-0">
          <h2 className="text-lg font-semibold text-foreground">{t('importSfiaTitle')}</h2>
          <button
            onClick={onClose}
            className="p-1.5 text-muted-foreground hover:bg-muted rounded-lg transition-colors"
          >
            <X size={20} />
          </button>
        </div>

        <div className="p-6">
          {error && (
            <div className="mb-4 p-3 bg-destructive/10 border border-destructive/20 rounded-xl flex items-start gap-2 text-destructive text-sm">
              <AlertCircle size={16} className="mt-0.5 shrink-0" />
              <p>{error}</p>
            </div>
          )}

          <div className="mb-6">
            <h3 className="text-sm font-medium mb-2">CSV Format Requirements:</h3>
            <div className="text-xs text-muted-foreground space-y-1">
              <p>1. Must include headers in the first row.</p>
              <p>2. Columns (in order): <strong>SkillCode, SkillName, Category, Subcategory, Description, AvailableLevels</strong>.</p>
              <p>3. AvailableLevels format (comma-separated): <strong>"4,5,6,7"</strong>.</p>
              <p>4. If SkillCode already exists, it will update the existing skill.</p>
            </div>
          </div>

          <form id="import-form" onSubmit={handleSubmit} className="space-y-4">
            <div 
              className={`border-2 border-dashed rounded-2xl p-8 text-center transition-colors ${
                file ? "border-primary/50 bg-primary/5" : "border-border hover:border-primary/50 hover:bg-muted/50 cursor-pointer"
              }`}
              onClick={() => !file && fileInputRef.current?.click()}
            >
              {file ? (
                <div className="flex flex-col items-center gap-2">
                  <FileText size={32} className="text-primary" />
                  <span className="text-sm font-medium text-foreground">{file.name}</span>
                  <span className="text-xs text-muted-foreground">{(file.size / 1024).toFixed(1)} KB</span>
                  <button 
                    type="button" 
                    onClick={(e) => { e.stopPropagation(); setFile(null); }}
                    className="mt-2 text-xs text-destructive hover:underline"
                  >
                    Remove file
                  </button>
                </div>
              ) : (
                <div className="flex flex-col items-center gap-2 text-muted-foreground">
                  <UploadCloud size={32} />
                  <span className="text-sm font-medium">{t('importSfiaClickToUpload')}</span>
                  <span className="text-xs opacity-70">{t('importSfiaFileHint')}</span>
                </div>
              )}
              <input
                ref={fileInputRef}
                type="file"
                accept=".csv"
                className="hidden"
                onChange={handleFileChange}
              />
            </div>
          </form>
        </div>

        <div className="px-6 py-4 border-t border-border bg-muted/10 flex justify-end gap-3 shrink-0">
          <button
            type="button"
            onClick={onClose}
            disabled={isPending}
            className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors disabled:opacity-50"
          >
            {t('cancelBtn')}
          </button>
          <button
            type="submit"
            form="import-form"
            disabled={!file || isPending}
            className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 rounded-xl shadow-xs transition-colors disabled:opacity-50"
          >
            {isPending ? (
              <div className="w-4 h-4 border-2 border-primary-foreground/30 border-t-primary-foreground rounded-full animate-spin" />
            ) : (
              <UploadCloud size={16} />
            )}
            <span>{isPending ? t('importSfiaProcessBtn') : t('importSfiaProcessBtn')}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
