'use client';

import { useState, useRef, useCallback } from 'react';
import { useGetMyCvs, useCreateCv, useDeleteCv } from '@/hooks/useCv';
import { useUploadFile } from '@/hooks/useUpload';
import { CvCard } from '@/components/shared/CvCard';
import { CloudUpload } from 'lucide-react';
import { cn } from '@/lib/utils';

export default function ResumesPage() {
  const { data: cvsResponse, isLoading: isLoadingCvs } = useGetMyCvs();
  const { mutateAsync: uploadFile } = useUploadFile();
  const { mutateAsync: createCv } = useCreateCv();
  const { mutate: deleteCv, isPending: isDeleting } = useDeleteCv();

  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const cvs = cvsResponse?.data || [];

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

      {/* Upload Zone */}
      <div
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
        onDrop={onDrop}
        onClick={() => fileInputRef.current?.click()}
        className={cn(
          "flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed bg-slate-50/50 p-10 text-center transition-colors hover:bg-slate-50",
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
        <div className="mb-4 rounded-full bg-blue-50 p-3 text-blue-600">
          <CloudUpload className="h-6 w-6" />
        </div>
        <p className="mb-1 text-sm font-semibold text-slate-900">
          {isUploading ? 'Uploading...' : 'Drag & drop your resume here'}
        </p>
        <p className="text-xs text-slate-500">
          or click to browse · PDF, DOC, DOCX · Max 5 MB
        </p>
      </div>

      {/* Resumes Grid */}
      {isLoadingCvs ? (
        <div className="text-center text-sm text-slate-500 py-10">Loading resumes...</div>
      ) : cvs.length === 0 ? (
        <div className="text-center text-sm text-slate-500 py-10 border border-dashed rounded-xl">
          No resumes uploaded yet.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {cvs.map((cv) => (
            <CvCard 
              key={cv.id} 
              cv={cv} 
              onDelete={(id) => deleteCv(id)} 
              isDeleting={isDeleting} 
            />
          ))}
        </div>
      )}
    </div>
  );
}
