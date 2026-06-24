import { FileText, Eye, Trash2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { Cv } from '@/types/cv.types';

interface CvCardProps {
  cv: Cv;
  onDelete: (id: string) => void;
  isDeleting?: boolean;
  isActive?: boolean;
  onSelect?: (cv: Cv) => void;
}

export function CvCard({ cv, onDelete, isDeleting, isActive, onSelect }: CvCardProps) {
  // Format date: "Jun 10, 2026"
  const formattedDate = new Date(cv.createdAt).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  // Format size: "184 KB"
  const formattedSize = cv.fileSize
    ? `${Math.round(cv.fileSize / 1024)} KB`
    : 'Unknown size';

  return (
    <div
      onClick={() => onSelect?.(cv)}
      className={cn(
        "flex flex-col gap-4 rounded-xl border p-5 shadow-sm transition-all cursor-pointer",
        isActive 
          ? "border-blue-500 bg-blue-50/20 shadow-md ring-1 ring-blue-500" 
          : "border-slate-200 bg-white hover:shadow-md hover:border-slate-300"
      )}
    >
      <div className="flex items-start gap-4">
        <div className={cn(
          "flex h-10 w-10 shrink-0 items-center justify-center rounded-lg text-blue-600 transition-colors",
          isActive ? "bg-blue-100" : "bg-blue-50"
        )}>
          <FileText className="h-5 w-5" />
        </div>
        <div className="flex flex-col overflow-hidden">
          <h4 className="truncate text-sm font-semibold text-slate-900" title={cv.fileName}>
            {cv.fileName}
          </h4>
          <p className="text-xs text-slate-500">
            {formattedDate} • {formattedSize}
          </p>
        </div>
      </div>

      <div className="flex items-center justify-between border-t border-slate-100 pt-3">
        <span className="text-xs text-slate-400">
          {isActive ? 'Viewing' : 'Click to view'}
        </span>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete(cv.id);
          }}
          disabled={isDeleting}
          className={cn(
            "flex items-center gap-1.5 text-xs font-medium text-red-500 transition-colors hover:text-red-600",
            isDeleting && "opacity-50 cursor-not-allowed"
          )}
        >
          <Trash2 className="h-3.5 w-3.5" />
          {isDeleting ? 'Deleting...' : 'Delete'}
        </button>
      </div>
    </div>
  );
}

