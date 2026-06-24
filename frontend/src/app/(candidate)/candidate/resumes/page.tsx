'use client';

import { useState, useRef, useCallback, useEffect } from 'react';
import { useGetMyCvs, useCreateCv, useDeleteCv } from '@/hooks/useCv';
import { useUploadFile } from '@/hooks/useUpload';
import { CvCard } from '@/components/shared/CvCard';
import { CloudUpload, ExternalLink, FileText } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { Cv } from '@/types/cv.types';

export default function ResumesPage() {
  const { data: cvsResponse, isLoading: isLoadingCvs } = useGetMyCvs();
  const { mutateAsync: uploadFile } = useUploadFile();
  const { mutateAsync: createCv } = useCreateCv();
  const { mutate: deleteCv, isPending: isDeleting } = useDeleteCv();

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
    if (file.size > 5 * 1024 * 1024) {
      alert('File is too large. Max 5MB allowed.');
      return;
    }

    try {
      setIsUploading(true);
      // 1. Upload to storage
      const uploadRes = await uploadFile({ file, folderName: 'cv' });
      if (!uploadRes?.success || !uploadRes.data) {
        throw new Error(uploadRes?.message || 'Upload failed');
      }

      // 2. Create CV record
      await createCv({
        fileUrl: uploadRes.data,
        fileName: file.name,
        fileSize: file.size,
        fileType: file.type || 'application/pdf',
        isPrimary: cvs.length === 0, // First CV is primary by default
        parsedData: '', // Mock empty parsed data for now
      });

    } catch (error) {
      console.error('Failed to upload CV:', error);
      alert('Failed to upload CV. Please try again.');
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
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold tracking-tight text-slate-900">My Resumes</h1>
        <p className="text-sm text-slate-500">
          Manage and optimize your resumes for ATS compatibility
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12 items-start">
        {/* Left Side: Upload & List (5 cols on lg) */}
        <div className="flex flex-col gap-6 lg:col-span-5">
          {/* Upload Zone */}
          <div
            onDragOver={onDragOver}
            onDragLeave={onDragLeave}
            onDrop={onDrop}
            onClick={() => fileInputRef.current?.click()}
            className={cn(
              "flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed bg-slate-50/50 p-6 text-center transition-colors hover:bg-slate-50",
              isDragging ? "border-blue-400 bg-blue-50" : "border-slate-200",
              isUploading && "pointer-events-none opacity-60"
            )}
          >
            <input
              type="file"
              className="hidden"
              accept=".pdf,.doc,.docx"
              ref={fileInputRef}
              onChange={handleFileInput}
            />
            <div className="mb-3 rounded-full bg-blue-50 p-2.5 text-blue-600">
              <CloudUpload className="h-5 w-5" />
            </div>
            <p className="mb-1 text-xs font-semibold text-slate-900">
              {isUploading ? 'Uploading...' : 'Drag & drop your resume here'}
            </p>
            <p className="text-[10px] text-slate-500">
              or click to browse · PDF, DOC, DOCX · Max 5 MB
            </p>
          </div>

          {/* Resumes List */}
          <div className="flex flex-col gap-4">
            <h3 className="text-sm font-semibold text-slate-900">Uploaded CVs ({cvs.length})</h3>
            {isLoadingCvs ? (
              <div className="text-center text-sm text-slate-500 py-10">Loading resumes...</div>
            ) : cvs.length === 0 ? (
              <div className="text-center text-sm text-slate-500 py-10 border border-dashed rounded-xl">
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
            <div className="flex flex-col h-[650px] border border-slate-200 rounded-xl bg-slate-50 overflow-hidden shadow-xs">
              <div className="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3">
                <div className="flex flex-col min-w-0">
                  <span className="text-sm font-semibold text-slate-900 truncate" title={selectedCv.fileName}>
                    {selectedCv.fileName}
                  </span>
                  <span className="text-xs text-slate-500">
                    Live Preview
                  </span>
                </div>
                <a
                  href={selectedCv.fileUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50 transition-colors"
                >
                  <ExternalLink className="h-3.5 w-3.5" />
                  <span>Open in new tab</span>
                </a>
              </div>
              <div className="flex-1 w-full h-full min-h-0 bg-slate-100">
                <iframe
                  src={getEmbedUrl(selectedCv.fileUrl)}
                  className="w-full h-full border-0"
                  title={selectedCv.fileName}
                />
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center h-[650px] border border-dashed border-slate-200 rounded-xl bg-slate-50 text-slate-400 gap-2">
              <FileText className="h-10 w-10 text-slate-300 animate-pulse" />
              <p className="text-sm font-medium">Select a resume from the list to view its preview</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

