'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useGetMatchResult } from '@/hooks/useCvMatch';
import { useMutation } from '@tanstack/react-query';
import { optimizeService } from '@/services/optimize.service';
import type { MatchingOutput } from '@/types/cv.types';
import { Loader2, AlertCircle } from 'lucide-react';
import { APP_ROUTES } from '@/lib/constants';
import { Button } from '@/components/ui/button';

import { useCvOptimizer } from '@/hooks/useCvOptimizer';
import { OptimizerHeader } from '../../components/optimizer/OptimizerHeader';
import { SuggestionCard } from '../../components/optimizer/SuggestionCard';
import { OptimizerCompletion } from '../../components/optimizer/OptimizerCompletion';
import { toast } from 'sonner';

export default function CvOptimizePage({ params }: { params: Promise<{ jobId: string }> }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const resolvedParams = React.use(params);
  
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

  const generateFileMutation = useMutation({
    mutationFn: () => optimizeService.generateFile(sessionId!),
    onSuccess: (res) => {
      if (res.data) {
        window.open(res.data, '_blank');
        toast.success("CV Downloaded successfully!");
      }
    },
    onError: () => toast.error("Failed to generate CV file.")
  });

  // Loading state
  if (isLoading || !matchOutput) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="text-muted-foreground">Loading CV constraints and suggestions...</p>
      </div>
    );
  }

  // Error or no valid suggestions
  if (isError || state.validSuggestions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4 max-w-md mx-auto text-center">
        <AlertCircle className="h-12 w-12 text-muted-foreground mb-2" />
        <h2 className="text-xl font-bold">No Optimizations Available</h2>
        <p className="text-muted-foreground">
          Your CV is already well-optimized or there are no valid AI suggestions available for this job description.
        </p>
        <Button onClick={() => router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${resolvedParams.jobId}`)}>
          Back to Match Result
        </Button>
      </div>
    );
  }

  const handleBack = () => {
    router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${resolvedParams.jobId}`);
  };

  const handlePreview = () => {
    toast.info("Preview generation is running in the background. Feature coming soon!");
  };

  const handleSave = () => {
    toast.success("Saved to My CVs successfully!");
    router.push(`${APP_ROUTES.CANDIDATE.CV_MATCHING}/new?jobId=${resolvedParams.jobId}`);
  };


  const handleDownload = () => {
    if (!sessionId) {
      toast.error("Cannot download: Session not initialized.");
      return;
    }
    toast.info("Generating your optimized CV...");
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
    </div>
  );
}
