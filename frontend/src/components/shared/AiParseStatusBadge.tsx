import React from 'react';
import { Loader2, CheckCircle2, AlertCircle, Sparkles } from 'lucide-react';
import { cn } from '@/lib/utils';

interface AiParseStatusBadgeProps {
  status?: 'PENDING' | 'PROCESSING' | 'SUCCESS' | 'FAILED' | string;
  error?: string | null;
  className?: string;
  forCandidate?: boolean;
}

export const AiParseStatusBadge: React.FC<AiParseStatusBadgeProps> = ({
  status = 'SUCCESS',
  error,
  className,
  forCandidate = false,
}) => {
  const normalizedStatus = (status || 'SUCCESS').toUpperCase();

  if (normalizedStatus === 'PENDING' || normalizedStatus === 'PROCESSING') {
    return (
      <div
        className={cn(
          'inline-flex items-center gap-1.5 rounded-full bg-amber-500/10 px-2.5 py-0.5 text-xs font-medium text-amber-600 dark:text-amber-400 border border-amber-500/20',
          className
        )}
        title={forCandidate ? "Hệ thống cần xử lý thêm CV của bạn để có kết quả tốt nhất" : "AI is analyzing content"}
      >
        <Loader2 className="h-3 w-3 animate-spin text-amber-500" />
        <span>{forCandidate ? "Processing..." : "AI Processing..."}</span>
      </div>
    );
  }

  if (normalizedStatus === 'FAILED') {
    return (
      <div
        className={cn(
          'inline-flex items-center gap-1.5 rounded-full bg-rose-500/10 px-2.5 py-0.5 text-xs font-medium text-rose-600 dark:text-rose-400 border border-rose-500/20 cursor-help',
          className
        )}
        title={forCandidate ? "Vui lòng thử lại hoặc upload file khác" : (error || 'Failed to parse AI content')}
      >
        <AlertCircle className="h-3 w-3 text-rose-500" />
        <span>{forCandidate ? "Action Required" : "AI Failed"}</span>
      </div>
    );
  }

  // If candidate and SUCCESS, we can just return a generic Ready badge, or hide it. Let's return a clean "Ready" badge.
  if (forCandidate) {
     return null; // The user prefers it hidden if SUCCESS, or at least no AI mention. Let's hide it for candidate if success so it's cleaner.
  }

  return (
    <div
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-0.5 text-xs font-medium text-emerald-600 dark:text-emerald-400 border border-emerald-500/20',
        className
      )}
      title="AI content parsed successfully"
    >
      <Sparkles className="h-3 w-3 text-emerald-500" />
      <span>AI Parsed</span>
    </div>
  );
};
