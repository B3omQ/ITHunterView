import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { CheckCircle2, Loader2 } from 'lucide-react';
import {
  MATCHING_PROGRESS_STEPS,
  type MatchingProgressView,
} from '@/lib/matching-progress';
import { useTranslations } from "next-intl";

interface MatchingLoadingStateProps {
  progress: MatchingProgressView;
}

export function MatchingLoadingState({ progress }: MatchingLoadingStateProps) {
  const t = useTranslations("CandidateCVMatching");
  const isComplete = progress.stage === 'completed';

  return (
    <Card className="max-w-xl mx-auto w-full mt-12 border-muted shadow-none">
      <CardHeader className="space-y-1 text-center">
        <CardTitle className="text-xl font-bold flex items-center justify-center gap-2">
          {isComplete ? (
            <CheckCircle2 className="h-5 w-5 text-emerald-600" />
          ) : (
            <Loader2 className="h-5 w-5 animate-spin text-primary" />
          )}
          {isComplete ? t('progressCompleted') : t('analyzingSuitability')}
        </CardTitle>
        <CardDescription className="mx-auto max-w-md leading-relaxed">
          {t("loadingDesc")}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        {(progress.isSubmitting || progress.isWaitingForRetry) && (
          <div className="rounded-md border bg-muted/30 px-3 py-2 text-sm text-muted-foreground">
            {progress.isSubmitting ? t('submittingRequest') : t('waitingForRetry')}
          </div>
        )}

        <div className="space-y-2">
          <div className="flex justify-between text-sm font-semibold">
            <span>{t("progressLabel")}</span>
            <span>{progress.progressPercent}%</span>
          </div>
          <Progress
            value={progress.progressPercent}
            className="w-full"
            aria-label={t('progressLabel')}
          />
        </div>

        <div className="space-y-3 bg-muted/40 p-4 rounded-lg border">
          {MATCHING_PROGRESS_STEPS.map((translationKey, idx) => {
            const isDone = idx < progress.completedStepCount;
            const isCurrent = idx === progress.currentStepIndex;
            return (
              <div key={translationKey} className="flex items-start gap-2.5 text-sm transition-opacity duration-300">
                {isDone ? (
                  <CheckCircle2 className="h-4.5 w-4.5 text-emerald-600 shrink-0 mt-0.5" />
                ) : isCurrent ? (
                  <Loader2 className="h-4.5 w-4.5 text-primary animate-spin shrink-0 mt-0.5" />
                ) : (
                  <div className="h-4.5 w-4.5 rounded-full border border-muted-foreground/30 shrink-0 flex items-center justify-center text-[10px] mt-0.5 text-muted-foreground/60">
                    {idx + 1}
                  </div>
                )}
                <span className={isDone ? 'text-emerald-700 font-medium' : isCurrent ? 'text-foreground font-semibold' : 'text-muted-foreground'}>
                  {t(translationKey)}
                </span>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
