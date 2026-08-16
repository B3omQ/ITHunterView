'use client';

import { useState, useRef, useCallback, useEffect } from 'react';
import { useGetMyCvs, useCreateCv, useDeleteCv } from '@/hooks/useCv';
import { useUploadFile } from '@/hooks/useUpload';
import { CvCard } from '@/components/shared/CvCard';
import { CloudUpload, ExternalLink, FileText } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { Cv } from '@/types/cv.types';
import { useTranslations } from 'next-intl';

export default function ResumesPage() {
  const { data: cvsResponse, isLoading: isLoadingCvs } = useGetMyCvs();
  const { mutateAsync: uploadFile } = useUploadFile();
  const { mutateAsync: createCv } = useCreateCv();
  const { mutate: deleteCv, isPending: isDeleting } = useDeleteCv();
  const t = useTranslations("CandidateResumes");

  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  
  const [selectedCv, setSelectedCv] = useState<Cv | null>(null);

  const cvs = cvsResponse?.data || [];

  // Set default selected CV when data is loaded
  useEffect(() => {
    const list = cvsResponse?.data || [];
    if (list.length > 0) {
      if (!selectedCv || !list.some(c => c.id === selectedCv.id)) {
        setSelectedCv(list[0]);
      }
    } else {
      setSelectedCv(null);
    }
  }, [cvsResponse?.data, selectedCv]);

  const getEmbedUrl = (url: string) => {
    if (!url) return '';
    const cleanUrl = url.split('?')[0].toLowerCase();
    const isDoc = cleanUrl.endsWith('.doc') || cleanUrl.endsWith('.docx');
    
    if (isDoc) {
      return `https://docs.google.com/gview?url=${encodeURIComponent(url)}&embedded=true`;
    }
    
    return url;
  };

  const handleFile = async (file: File) => {
    if (cvs.length >= 10) {
      alert(t('maxCvLimitReached'));
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      alert(t('fileTooLarge'));
      return;
    }

    try {
      setIsUploading(true);
      // 1. Upload to storage
      const uploadRes = await uploadFile({ file, folderName: 'cv' });
      if (!uploadRes?.success || !uploadRes.data) {
        throw new Error(uploadRes?.message || 'Upload failed');
      }

      // Handle duplicate file names
      let finalFileName = file.name;
      let counter = 1;
      
      const lastDotIndex = file.name.lastIndexOf('.');
      const baseName = lastDotIndex === -1 ? file.name : file.name.substring(0, lastDotIndex);
      const extension = lastDotIndex === -1 ? '' : file.name.substring(lastDotIndex);

      while (cvs.some(cv => cv.fileName === finalFileName)) {
        finalFileName = `${baseName} (${counter})${extension}`;
        counter++;
      }

      // 2. Create CV record
      await createCv({
        fileUrl: uploadRes.data,
        fileName: finalFileName,
        fileSize: file.size,
        fileType: file.type || 'application/pdf',
        isPrimary: cvs.length === 0, // First CV is primary by default
      });

    } catch (error) {
      console.error('Failed to upload CV:', error);
      alert(t('uploadFailed'));
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const onDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const onDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const onDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleFile(e.dataTransfer.files[0]);
    }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      handleFile(e.target.files[0]);
    }
  };

  return (
    <div className="w-full pb-8 flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold tracking-tight text-foreground">{t('myResumes')}</h1>
        <p className="text-base text-muted-foreground">
          {t('myResumesDesc')}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12 items-start">
        {/* Left Side: Upload & List (5 cols on lg) */}
        <div className="flex flex-col gap-6 lg:col-span-5">
          {/* Upload Zone */}
          <div
            onDragOver={(e) => {
              if (cvs.length >= 10) {
                e.preventDefault();
                return;
              }
              onDragOver(e);
            }}
            onDragLeave={onDragLeave}
            onDrop={(e) => {
              if (cvs.length >= 10) {
                e.preventDefault();
                alert(t('maxCvLimitReached'));
                return;
              }
              onDrop(e);
            }}
            onClick={() => {
              if (cvs.length >= 10) {
                alert(t('maxCvLimitReached'));
                return;
              }
              fileInputRef.current?.click();
            }}
            className={cn(
              "flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed bg-muted/30 p-8 text-center transition-colors hover:bg-muted/50",
              isDragging ? "border-primary bg-primary/5" : "border-border",
              isUploading && "pointer-events-none opacity-60",
              cvs.length >= 10 && "opacity-60 cursor-not-allowed border-muted-foreground/30 hover:bg-muted/30"
            )}
          >
            <input
              type="file"
              className="hidden"
              accept=".pdf,.doc,.docx"
              ref={fileInputRef}
              onChange={handleFileInput}
            />
            <div className="mb-4 rounded-full bg-primary/10 p-3 text-primary">
              <CloudUpload className="h-6 w-6" />
            </div>
            <p className="mb-1 text-sm font-semibold text-foreground">
              {isUploading ? t('uploading') : t('dragDrop')}
            </p>
            <p className="text-xs text-muted-foreground">
              {t('uploadHint')}
            </p>
          </div>

          {/* Resumes List */}
          <div className="flex flex-col gap-4">
            <h3 className="text-base font-semibold text-foreground">Uploaded CVs ({cvs.length})</h3>
            {isLoadingCvs ? (
              <div className="text-center text-sm text-muted-foreground py-10">Loading resumes...</div>
            ) : cvs.length === 0 ? (
              <div className="text-center text-sm text-muted-foreground py-10 border border-dashed border-border rounded-xl">
                No resumes uploaded yet.
              </div>
            ) : (
              <div className="flex flex-col gap-3 max-h-[500px] overflow-y-auto pr-1">
                {cvs.map((cv) => (
                  <CvCard 
                    key={cv.id} 
                    cv={cv} 
                    isActive={selectedCv?.id === cv.id}
                    onSelect={(c) => setSelectedCv(c)}
                    onDelete={(id) => deleteCv(id)} 
                    isDeleting={isDeleting}
                  />
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Right Side: Live Viewer (7 cols on lg) */}
        <div className="lg:col-span-7 w-full">
          {selectedCv ? (
            <div className="flex flex-col h-[650px] border border-border rounded-xl bg-card overflow-hidden shadow-sm">
              <div className="flex items-center justify-between border-b border-border bg-muted/20 px-5 py-4">
                <div className="flex flex-col min-w-0">
                  <span className="text-base font-semibold text-foreground truncate" title={selectedCv.fileName}>
                    {selectedCv.fileName}
                  </span>
                  <span className="text-sm text-muted-foreground mt-0.5">
                    {t('livePreview')}
                  </span>
                </div>
                <a
                  href={selectedCv.fileUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 rounded-lg border border-border bg-card px-4 py-2 text-sm font-semibold text-foreground hover:bg-muted/50 transition-colors"
                >
                  <ExternalLink className="h-4 w-4" />
                  <span>{t('openNewTab')}</span>
                </a>
              </div>
              <div className="flex-1 w-full h-full min-h-0 bg-muted/10">
                <iframe
                  src={getEmbedUrl(selectedCv.fileUrl)}
                  className="w-full h-full border-0"
                  title={selectedCv.fileName}
                />
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center h-[650px] border border-dashed border-border rounded-xl bg-muted/10 text-muted-foreground gap-3">
              <FileText className="h-12 w-12 opacity-50 animate-pulse" />
              <p className="text-base font-medium">{t('selectResume')}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

