'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useGetMatchResult } from '@/hooks/useCvMatch';
import { useMutation } from '@tanstack/react-query';
import { optimizeService } from '@/services/optimize.service';
import type { MatchingOutput } from '@/types/cv.types';
import { Loader2, AlertCircle, FileText } from 'lucide-react';
import { APP_ROUTES } from '@/lib/constants';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog';

import { useCvOptimizer } from '@/hooks/useCvOptimizer';
import { OptimizerHeader } from '../../components/optimizer/OptimizerHeader';
import { SuggestionCard } from '../../components/optimizer/SuggestionCard';
import { OptimizerCompletion } from '../../components/optimizer/OptimizerCompletion';
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';

export default function CvOptimizePage({ params }: { params: Promise<{ jobId: string }> }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const resolvedParams = React.use(params);
  const t = useTranslations("CandidateCVMatching");
  
  const { data: pollData, isLoading, isError } = useGetMatchResult(resolvedParams.jobId);
  const [matchOutput, setMatchOutput] = useState<MatchingOutput | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);

  const createSessionMutation = useMutation({
    mutationFn: (payload: { cvUrl?: string; cvId?: string }) => 
      optimizeService.createSession(resolvedParams.jobId, payload),
    onSuccess: (res) => {
      if (res.data) setSessionId(res.data);
    },
    onError: (err) => {
      console.error("Failed to create optimize session", err);
      toast.error("Could not initialize optimization session. Proceeding with frontend-only mode.");
    }
  });

  useEffect(() => {
    // Create session on mount
    const cvUrl = searchParams.get('cvUrl');
    const cvId = searchParams.get('cvId');
    if (cvUrl || cvId) {
      createSessionMutation.mutate({ cvUrl: cvUrl || undefined, cvId: cvId || undefined });
    }
  }, [searchParams]);

  useEffect(() => {
    if (pollData?.data?.matchDetails) {
      try {
        const parsed = JSON.parse(pollData.data.matchDetails) as MatchingOutput;
        setMatchOutput(parsed);
      } catch (err) {
        console.error("Failed to parse match details", err);
      }
    }
  }, [pollData]);

  const { state, handlers } = useCvOptimizer(matchOutput, sessionId);

  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const [previewImageBase64, setPreviewImageBase64] = useState<string | null>(null);

  const generateFileMutation = useMutation({
    mutationFn: () => optimizeService.generateFile(sessionId!),
    onSuccess: (res) => {
      if (res.data) {
        window.open(res.data, '_blank');
        toast.success(t("cvDownloaded"));
      }
    },
    onError: () => toast.error(t("failedGenerateCv"))
  });

  const getPreviewMutation = useMutation({
    mutationFn: () => optimizeService.getPreview(sessionId!),
    onSuccess: (res) => {
      if (res.data) {
        setPreviewImageBase64(res.data);
        setIsPreviewOpen(true);
      } else {
        toast.info(t("previewPdfOnly"));
      }
    },
    onError: () => toast.error(t("failedLoadPreview"))
  });

  // Loading state
  if (isLoading || !matchOutput) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="text-muted-foreground">{t("loadingOptimizer")}</p>
      </div>
    );
  }

  // Error or no valid suggestions
  if (isError || state.validSuggestions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4 max-w-md mx-auto text-center">
        <AlertCircle className="h-12 w-12 text-muted-foreground mb-2" />
        <h2 className="text-xl font-bold">{t("noOptimizationsTitle")}</h2>
        <p className="text-muted-foreground">
          {t("noOptimizationsDesc")}
        </p>
        <Button onClick={() => router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${resolvedParams.jobId}`)}>
          {t("backToMatchResult")}
        </Button>
      </div>
    );
  }

  const handleBack = () => {
    router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${resolvedParams.jobId}`);
  };

  const handlePreview = () => {
    if (!sessionId) {
      toast.error(t("sessionNotInit"));
      return;
    }
    getPreviewMutation.mutate();
  };

  const handleSave = () => {
    toast.success(t("savedToMyCv"));
    router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${resolvedParams.jobId}`);
  };


  const handleDownload = () => {
    if (!sessionId) {
      toast.error(t("cannotDownload"));
      return;
    }
    toast.info(t("generatingOptimizedCv"));
    generateFileMutation.mutate();
  };

  return (
    <div className="min-h-screen bg-muted/10 pb-12">
      <OptimizerHeader 
        currentStep={state.currentIndex + 1}
        totalSteps={state.validSuggestions.length}
        currentScore={state.currentScore}
        progressPercent={state.progressPercent}
        onBack={handleBack}
      />
      
      <main className="max-w-4xl mx-auto px-4 pt-8">
        {!state.isComplete ? (
          <div className="py-8">
            <SuggestionCard 
              key={`sugg-${state.currentIndex}`} // force re-mount on index change for animations
              suggestion={state.currentSuggestion}
              onAccept={handlers.handleAccept}
              onSkip={handlers.handleSkip}
            />
          </div>
        ) : (
          <OptimizerCompletion 
            acceptedChanges={state.acceptedChanges}
            finalScore={state.currentScore}
            onPreview={handlePreview}
            onSave={handleSave}
            onDownload={handleDownload}
            onBack={handleBack}
          />
        )}
      </main>

      <Dialog open={isPreviewOpen} onOpenChange={setIsPreviewOpen}>
        <DialogContent className="max-w-4xl max-h-[90vh] flex flex-col">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <FileText className="w-5 h-5" /> {t("cvPreviewTitle")}
            </DialogTitle>
            <DialogDescription>
              {t("cvPreviewDesc")}
            </DialogDescription>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto bg-muted p-4 rounded border mt-4 flex justify-center">
            {getPreviewMutation.isPending ? (
              <div className="flex flex-col items-center justify-center p-12 text-muted-foreground gap-4">
                <Loader2 className="h-8 w-8 animate-spin" />
                <p>{t("generatingPreviewImg")}</p>
              </div>
            ) : previewImageBase64 ? (
              <img src={previewImageBase64} alt="CV Preview" className="max-w-full h-auto border shadow-sm bg-white" />
            ) : null}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
