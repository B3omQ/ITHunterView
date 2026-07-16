import { FileText, Eye, Trash2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { Cv } from '@/types/cv.types';

interface CvCardProps {
  cv: Cv;
  onDelete: (id: string) => void;
  isDeleting?: boolean;
  isActive?: boolean;
  onSelect?: (cv: Cv) => void;
  onMatchJobs?: (cv: Cv) => void;
}

export function CvCard({ cv, onDelete, isDeleting, isActive, onSelect, onMatchJobs }: CvCardProps) {
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
          ? "border-primary bg-primary/5 shadow-md ring-1 ring-primary" 
          : "border-border bg-card hover:shadow-md hover:border-primary/50"
      )}
    >
      <div className="flex items-start gap-4">
        <div className={cn(
          "flex h-12 w-12 shrink-0 items-center justify-center rounded-lg text-primary transition-colors",
          isActive ? "bg-primary/20" : "bg-primary/10"
        )}>
          <FileText className="h-6 w-6" />
        </div>
        <div className="flex flex-col overflow-hidden justify-center py-1">
          <h4 className="truncate text-base font-semibold text-foreground group-hover:text-primary transition-colors" title={cv.fileName}>
            {cv.fileName}
          </h4>
          <p className="text-sm text-muted-foreground mt-0.5">
            {formattedDate} • {formattedSize}
          </p>
        </div>
      </div>

      <div className="flex items-center justify-between border-t border-border pt-4 mt-2">
        <span className="text-sm text-muted-foreground">
          {isActive ? 'Viewing' : 'Click to view'}
        </span>
        
        <div className="flex items-center gap-4">
          {onMatchJobs && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                onMatchJobs(cv);
              }}
              className="flex items-center gap-1.5 text-sm font-semibold text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300 transition-colors"
            >
              <Eye className="h-4 w-4" />
              Find Matches
            </button>
          )}

          <button
            onClick={(e) => {
              e.stopPropagation();
              onDelete(cv.id);
            }}
            disabled={isDeleting}
            className={cn(
              "flex items-center gap-1.5 text-sm font-medium text-destructive transition-colors hover:text-destructive/80",
              isDeleting && "opacity-50 cursor-not-allowed"
            )}
          >
            <Trash2 className="h-4 w-4" />
            {isDeleting ? 'Deleting...' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  );
}

