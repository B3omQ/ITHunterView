'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { JobApplicationService } from '@/services/job-application.service';
import { cvService } from '@/services/cv.service';
import { uploadService } from '@/services/upload.service';
import { toast } from 'sonner';
import type { Cv } from '@/types/cv.types';
import { FileText, Upload, CheckCircle, Loader2, X } from 'lucide-react';

interface ApplyJobModalProps {
  isOpen: boolean;
  onClose: () => void;
  jobId: string;
  jobTitle: string;
  onSuccess: () => void;
}

export function ApplyJobModal({ isOpen, onClose, jobId, jobTitle, onSuccess }: ApplyJobModalProps) {
  const router = useRouter();
  const [coverLetter, setCoverLetter] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isUploading, setIsUploading] = useState(false);

  // Existing CVs
  const [myCvs, setMyCvs] = useState<Cv[]>([]);
  const [selectedCvId, setSelectedCvId] = useState<string | null>(null);
  const [isLoadingCvs, setIsLoadingCvs] = useState(false);

  // Optional new file upload
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Load existing CVs when modal opens
  useEffect(() => {
    if (!isOpen) return;
    setIsLoadingCvs(true);
    cvService
      .getMyCvs()
      .then((res) => {
        const list = res.data ?? [];
        setMyCvs(list);
        // Auto-select primary CV, fallback to first
        const primary = list.find((c) => c.isPrimary) ?? list[0] ?? null;
        setSelectedCvId(primary?.id ?? null);
      })
      .catch(() => setMyCvs([]))
      .finally(() => setIsLoadingCvs(false));
  }, [isOpen]);

  const handleClose = () => {
    setCoverLetter('');
    setUploadFile(null);
    onClose();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 10 * 1024 * 1024) {
      toast.error('File must be smaller than 10 MB.');
      return;
    }
    const allowed = [
      'application/pdf',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    ];
    if (!allowed.includes(file.type)) {
      toast.error('Only PDF, DOC, or DOCX files are allowed.');
      return;
    }
    setUploadFile(file);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      let cvId: string | undefined;

      if (uploadFile) {
        // User chose to upload a new file → upload → save to DB → get cvId
        setIsUploading(true);
        try {
          const uploadRes = await uploadService.uploadFile(uploadFile, 'cvs');
          if (!uploadRes.success || !uploadRes.data) throw new Error('File upload failed.');

          const cvRes = await cvService.createCv({
            fileUrl: uploadRes.data,
            fileName: uploadFile.name,
            fileSize: uploadFile.size,
            fileType: uploadFile.type,
            isPrimary: myCvs.length === 0,
            parsedData: '',
          });
          if (!cvRes.success || !cvRes.data) throw new Error('Failed to save CV record.');
          cvId = cvRes.data.id;
        } finally {
          setIsUploading(false);
        }
      } else if (selectedCvId) {
        // Use the pre-selected existing CV
        cvId = selectedCvId;
      }
      // If neither → submit without CV (cvId stays undefined)

      await JobApplicationService.applyForJob({ jobId, cvId, coverLetter });
      toast.success('Application submitted successfully!');
      onSuccess();
      handleClose();
    } catch (error: any) {
      const status = error.response?.status;
      const serverMessage = error.response?.data?.message;
      if (status === 401) {
        toast.error('Session expired. Please log in again.');
        onClose();
        const currentPath = typeof window !== 'undefined'
          ? window.location.pathname + window.location.search
          : '/jobs';
        router.push(`/login?redirect=${encodeURIComponent(currentPath)}`);
      } else if (status === 403) {
        toast.error('Only candidates can apply for jobs.');
      } else if (status === 409) {
        toast.info(serverMessage || 'You have already applied for this job.');
        onClose();
      } else {
        toast.error(serverMessage || error.message || 'Failed to apply. Please try again.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatSize = (bytes: number) => {
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const busy = isSubmitting || isUploading;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="sm:max-w-[520px]">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle className="text-lg font-semibold">Apply for {jobTitle}</DialogTitle>
            <DialogDescription>
              Review your CV and optionally add a cover letter.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-5">
            {/* ── CV Section ─────────────────────────────────────────── */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <Label className="text-sm font-medium">CV / Resume</Label>
                <span className="text-xs text-muted-foreground">Optional</span>
              </div>

              {isLoadingCvs ? (
                <div className="flex items-center gap-2 text-sm text-muted-foreground py-2">
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Loading your CVs…
                </div>
              ) : (
                <div className="space-y-2">
                  {/* Existing CVs list */}
                  {myCvs.length > 0 && !uploadFile && (
                    <div className="space-y-1.5 max-h-44 overflow-y-auto pr-1">
                      {myCvs.map((cv) => (
                        <button
                          key={cv.id}
                          type="button"
                          onClick={() => setSelectedCvId(cv.id)}
                          className={`w-full flex items-center gap-3 rounded-lg border px-3 py-2.5 text-left transition-all ${
                            selectedCvId === cv.id
                              ? 'border-primary bg-primary/5 ring-1 ring-primary'
                              : 'border-border hover:border-primary/40 hover:bg-muted/40'
                          }`}
                        >
                          <FileText
                            className={`w-5 h-5 shrink-0 ${
                              selectedCvId === cv.id ? 'text-primary' : 'text-muted-foreground'
                            }`}
                          />
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium truncate">{cv.fileName}</p>
                            <p className="text-xs text-muted-foreground">
                              {cv.fileSize ? formatSize(cv.fileSize) : '—'}
                              {cv.isPrimary && (
                                <span className="ml-2 text-primary font-medium">· Primary</span>
                              )}
                            </p>
                          </div>
                          {selectedCvId === cv.id && (
                            <CheckCircle className="w-4 h-4 text-primary shrink-0" />
                          )}
                        </button>
                      ))}
                    </div>
                  )}

                  {/* Uploaded new file preview */}
                  {uploadFile && (
                    <div className="flex items-center gap-3 rounded-lg border border-primary bg-primary/5 px-3 py-2.5">
                      <FileText className="w-5 h-5 text-primary shrink-0" />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">{uploadFile.name}</p>
                        <p className="text-xs text-muted-foreground">{formatSize(uploadFile.size)} · New upload</p>
                      </div>
                      <button
                        type="button"
                        onClick={() => {
                          setUploadFile(null);
                          if (fileInputRef.current) fileInputRef.current.value = '';
                        }}
                        className="p-1 rounded hover:bg-destructive/10 text-muted-foreground hover:text-destructive transition-colors"
                        title="Remove file"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>
                  )}

                  {/* Upload button / zone */}
                  <input
                    ref={fileInputRef}
                    id="cv-file-input"
                    type="file"
                    accept=".pdf,.doc,.docx"
                    className="hidden"
                    onChange={handleFileChange}
                  />
                  <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    className="w-full flex items-center justify-center gap-2 rounded-lg border border-dashed border-border hover:border-primary/60 bg-muted/20 hover:bg-muted/40 py-3 text-sm text-muted-foreground hover:text-foreground transition-all group"
                  >
                    <Upload className="w-4 h-4 group-hover:text-primary transition-colors" />
                    {uploadFile
                      ? 'Replace with another file'
                      : myCvs.length > 0
                      ? 'Or upload a different CV'
                      : 'Upload CV (PDF, DOC, DOCX · max 10 MB)'}
                  </button>

                  {!uploadFile && !selectedCvId && myCvs.length === 0 && (
                    <p className="text-xs text-muted-foreground">
                      No CV attached — you can still submit without one.
                    </p>
                  )}
                </div>
              )}
            </div>

            {/* ── Cover Letter ───────────────────────────────────────── */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="coverLetter" className="text-sm font-medium">Cover Letter</Label>
                <span className="text-xs text-muted-foreground">Optional</span>
              </div>
              <Textarea
                id="coverLetter"
                placeholder="Why are you a great fit for this role?"
                value={coverLetter}
                onChange={(e) => setCoverLetter(e.target.value)}
                rows={4}
                className="resize-none"
              />
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose} disabled={busy}>
              Cancel
            </Button>
            <Button type="submit" disabled={busy}>
              {busy ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  {isUploading ? 'Uploading CV…' : 'Submitting…'}
                </>
              ) : (
                'Submit Application'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
