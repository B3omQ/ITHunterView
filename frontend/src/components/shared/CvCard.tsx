import { useState } from 'react';
import { FileText, Eye, Trash2, Star, Loader2 } from 'lucide-react';
import { useIsMutating } from '@tanstack/react-query';
import { cn } from '@/lib/utils';
import type { Cv } from '@/types/cv.types';

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { useSetPrimaryCv } from '@/hooks/useCv';
import { useTranslations } from 'next-intl';

interface CvCardProps {
  cv: Cv;
  onDelete: (id: string) => void;
  isDeleting?: boolean;
  isActive?: boolean;
  onSelect?: (cv: Cv) => void;
}

export function CvCard({ cv, onDelete, isDeleting, isActive, onSelect }: CvCardProps) {
  const [showConfirm, setShowConfirm] = useState(false);
  const { mutate: setPrimary, isPending: isSettingPrimary } = useSetPrimaryCv();
  const isAnySettingPrimary = useIsMutating({ mutationKey: ['set-primary-cv'] }) > 0;
  const t = useTranslations("CandidateResumes");

  // Format date: "Jun 10, 2026"
  const formattedDate = new Date(cv.createdAt).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  // Format size: "184 KB"
  const formattedSize = cv.fileSize
    ? `${Math.round(cv.fileSize / 1024)} KB`
    : t('unknownSize');

  return (
    <div
      onClick={() => onSelect?.(cv)}
      className={cn(
        "flex flex-col gap-3 rounded-xl border p-4 shadow-sm transition-all cursor-pointer group",
        isActive 
          ? "border-primary bg-primary/5 shadow-md ring-1 ring-primary" 
          : "border-border bg-card hover:border-primary/50"
      )}
    >
      <div className="flex items-center gap-3">
        <div className={cn(
          "flex h-11 w-11 shrink-0 items-center justify-center rounded-lg text-primary transition-colors border border-border",
          isActive ? "bg-primary/20" : "bg-primary/10"
        )}>
          <FileText className="h-5 w-5" />
        </div>
        <div className="flex flex-col overflow-hidden justify-center py-1 flex-1">
          <div className="flex items-center justify-between gap-2">
            <h4 className="truncate text-base font-semibold text-foreground group-hover:text-primary transition-colors" title={cv.fileName}>
              {cv.fileName}
            </h4>

          </div>
          <div className="flex items-center gap-2 mt-0.5">
            <p className="text-sm text-muted-foreground">
              {formattedDate} • {formattedSize}
            </p>
            {cv.isPrimary && (
              <span className="text-[10px] bg-primary/10 text-primary px-1.5 py-0.5 rounded-full font-medium">
                {t('primary')}
              </span>
            )}
            {cv.parseStatus === 'PROCESSING' && (
              <span className="text-[10px] bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded-full font-medium flex items-center gap-1">
                <Loader2 className="h-2 w-2 animate-spin" />
                {t('analyzingAI')}
              </span>
            )}
            {cv.parseStatus === 'FAILED' && (
              <span className="text-[10px] bg-red-100 text-red-700 px-1.5 py-0.5 rounded-full font-medium" title={cv.parseError || undefined}>
                {t('analysisFailed')}
              </span>
            )}
          </div>
        </div>
      </div>

      <div className="flex flex-wrap items-center justify-between border-t border-border/50 pt-2.5 mt-1">
        <span className="text-sm text-muted-foreground">
          {isActive ? t('viewing') : t('clickToView')}
        </span>
        
        <div className="flex items-center gap-2">
          
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              if (!cv.isPrimary) {
                setPrimary(cv.id);
              }
            }}
            disabled={isDeleting || isAnySettingPrimary || cv.isPrimary}
            className={cn(
              "gap-1.5 h-8 transition-colors",
              cv.isPrimary 
                ? "text-yellow-500 hover:text-yellow-600 hover:bg-yellow-50 opacity-100" 
                : "text-muted-foreground hover:text-yellow-500 hover:bg-yellow-50",
              (isDeleting || isAnySettingPrimary) && "opacity-50 cursor-not-allowed"
            )}
            title={cv.isPrimary ? t('thisIsPrimary') : t('setAsPrimary')}
          >
            {isSettingPrimary ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              <Star className={cn("h-3.5 w-3.5", cv.isPrimary && "fill-current")} />
            )}
            <span className="hidden sm:inline">
              {isSettingPrimary ? t('setting') : cv.isPrimary ? t('primary') : t('setPrimary')}
            </span>
          </Button>

          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              setShowConfirm(true);
            }}
            disabled={isDeleting}
            className={cn(
              "text-muted-foreground hover:text-destructive hover:bg-destructive/10 gap-1.5 h-8",
              isDeleting && "opacity-50 cursor-not-allowed"
            )}
          >
            <Trash2 className="h-3.5 w-3.5" />
            {isDeleting ? t('deleting') : t('delete')}
          </Button>

          <Dialog open={showConfirm} onOpenChange={setShowConfirm}>
            <DialogContent onClick={(e) => e.stopPropagation()}>
              <DialogHeader>
                <DialogTitle>{t('deleteResume')}</DialogTitle>
                <DialogDescription>
                  {t('deleteResumeConfirm', { fileName: cv.fileName })}
                </DialogDescription>
              </DialogHeader>
              <DialogFooter className="mt-4 flex sm:justify-end gap-2">
                <Button variant="outline" onClick={(e) => {
                  e.stopPropagation();
                  setShowConfirm(false);
                }}>
                  {t('cancel', { defaultValue: 'Cancel' })}
                </Button>
                <Button variant="destructive" onClick={(e) => {
                  e.stopPropagation();
                  onDelete(cv.id);
                  setShowConfirm(false);
                }} disabled={isDeleting}>
                  {isDeleting ? t('deleting') : t('delete')}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
      </div>
    </div>
  );
}

