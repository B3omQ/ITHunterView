import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { CheckCircle2, Loader2 } from 'lucide-react';
import { MATCHING_LOADING_STEPS } from '@/hooks/useCvMatchingForm';

interface MatchingLoadingStateProps {
  progressPercent: number;
  loadingStep: number;
}

export function MatchingLoadingState({ progressPercent, loadingStep }: MatchingLoadingStateProps) {
  return (
    <Card className="max-w-xl mx-auto w-full mt-12 border-muted">
      <CardHeader className="space-y-1 text-center">
        <CardTitle className="text-xl font-bold flex items-center justify-center gap-2">
          <Loader2 className="h-5 w-5 animate-spin text-primary" />
          Analyzing Suitability
        </CardTitle>
        <CardDescription>
          This might take around 15–30 seconds. Do not close this window.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="space-y-2">
          <div className="flex justify-between text-sm font-semibold">
            <span>Progress</span>
            <span>{progressPercent}%</span>
          </div>
          <Progress value={progressPercent} className="w-full" />
        </div>

        {/* List steps */}
        <div className="space-y-3 bg-muted/40 p-4 rounded-lg border">
          {MATCHING_LOADING_STEPS.map((stepMsg, idx) => {
            const isDone = idx < loadingStep;
            const isCurrent = idx === loadingStep;
            return (
              <div key={idx} className="flex items-start gap-2.5 text-sm transition-opacity duration-300">
                {isDone ? (
                  <CheckCircle2 className="h-4.5 w-4.5 text-emerald-600 shrink-0 mt-0.5" />
                ) : isCurrent ? (
                  <Loader2 className="h-4.5 w-4.5 text-primary animate-spin shrink-0 mt-0.5" />
                ) : (
                  <div className="h-4.5 w-4.5 rounded-full border border-muted-foreground/30 shrink-0 flex items-center justify-center text-[10px] mt-0.5 text-muted-foreground/60">
                    {idx + 1}
                  </div>
                )}
                <span className={isDone ? 'text-emerald-700/80 font-medium line-through' : isCurrent ? 'text-foreground font-semibold' : 'text-muted-foreground'}>
                  {stepMsg}
                </span>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
