'use client';

import React, { Suspense } from 'react';
import { useRouter } from 'next/navigation';
import { APP_ROUTES } from '@/lib/constants';
import { Button } from '@/components/ui/button';
import { Loader2, Sparkles, ArrowRight, Info, History, Coins, Zap } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

import { ResultOverviewCard } from '../components/ResultOverviewCard';
import { RequirementBreakdown } from '../components/RequirementBreakdown';
import { CriticalGapsPanel } from '../components/CriticalGapsPanel';

import { CvSelectionPanel } from '../components/CvSelectionPanel';
import { JdSelectionPanel } from '../components/JdSelectionPanel';
import { MatchingLoadingState } from '../components/MatchingLoadingState';
import { useCvMatchingForm } from '@/hooks/useCvMatchingForm';
import { useTranslations } from 'next-intl';

function CvMatchingContent() {
  const router = useRouter();
  const t = useTranslations('CandidateCVMatching');
  const { state, queries, setters, handlers } = useCvMatchingForm();

  return (
    <div className="w-full pb-8">
      {/* 1. Tiêu đề chính */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-8 gap-4">
        <div className="flex flex-col space-y-2 text-center md:text-left">
          <h1 className="text-3xl font-extrabold tracking-tight flex items-center justify-center md:justify-start gap-2">
            <Sparkles className="h-8 w-8 text-primary animate-pulse" />
            {t('newTitle')}
          </h1>
          <p className="text-muted-foreground text-sm max-w-2xl">
            {t('newDesc')}
          </p>
        </div>
        
        <Button 
          variant="outline" 
          onClick={() => router.push(APP_ROUTES.CANDIDATE.CV_MATCHING)}
          className="gap-2 self-start sm:self-auto"
        >
          <History className="h-4 w-4" />
          {t('viewHistory')}
        </Button>
      </div>

      {state.step === 'select' && (
        <div className="space-y-8">
          {state.resultError && (
            <div role="alert" className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {state.resultError}
            </div>
          )}
          {/* Feature Cost & Wallet Balance Banner */}
          <div className="p-4 rounded-xl bg-gradient-to-r from-purple-500/10 via-amber-500/10 to-transparent border border-purple-500/20 shadow-sm flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-500/20 text-amber-500 shadow-inner">
                {state.hasActiveSub ? <Zap className="h-5 w-5 text-purple-600 dark:text-purple-400 fill-purple-600/20" /> : <Coins className="h-5 w-5 text-amber-500 fill-amber-500/20" />}
              </div>
              <div className="flex flex-col">
                <div className="flex items-center gap-1.5 flex-wrap">
                  <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground">{t('serviceFee')}</span>
                  {state.hasActiveSub ? (
                    <>
                      <Badge className="bg-purple-600 text-white text-[10px] font-bold px-2 py-0.5 shadow-sm">
                        {t('freeSub', { subName: state.activeSubName ?? '' })}
                      </Badge>
                      <span className="text-xs font-semibold text-purple-600 dark:text-purple-400">
                        {state.isSubUnlimited ? t('unlimitedMatches') : t('remainingMatches', { remaining: state.subRemaining, limit: state.matchLimit })}
                      </span>
                    </>
                  ) : (
                    <span className="text-sm font-black text-amber-600 dark:text-amber-400">
                      {t('coinPerMatch', { coin: (state.cvMatchCost ?? 1000).toLocaleString() })}
                    </span>
                  )}
                </div>
                {!!state.activeSubName && !state.hasActiveSub && (
                  <span className="text-xs text-rose-500 mt-0.5 font-medium">
                    {t('subExpired', { subName: state.activeSubName ?? '' })}
                  </span>
                )}
                {!state.hasActiveSub && (
                  <span className="text-xs text-muted-foreground mt-0.5 font-medium">
                    {t('currentBalance')} <strong className={(state.balance ?? 0) < (state.cvMatchCost ?? 1000) ? "text-rose-500 font-bold" : "text-emerald-600 font-bold"}>{(state.balance ?? 0).toLocaleString()} Coin</strong>
                  </span>
                )}
              </div>
            </div>

            {!state.hasActiveSub && (state.balance ?? 0) < (state.cvMatchCost ?? 1000) && (
              <Button
                type="button"
                size="sm"
                onClick={() => router.push('/candidate/top-up')}
                className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-3 py-1.5 text-xs rounded-lg shadow-md hover:shadow-amber-500/25 transition-all shrink-0"
              >
                {t('topUpCoin')}
              </Button>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <CvSelectionPanel 
              cvTab={state.cvTab}
              setCvTab={setters.setCvTab}
              cvFile={state.cvFile}
              cvFileName={state.cvFileName}
              cvText={state.cvText}
              setCvText={setters.setCvText}
              selectedCvId={state.selectedCvId}
              setSelectedCvId={setters.setSelectedCvId}
              isUploading={state.isUploading}
              myCvs={queries.myCvs}
              isLoadingCvs={queries.isLoadingCvs}
              handleFileChange={handlers.handleFileChange}
              handleDragOver={handlers.handleDragOver}
              handleDrop={handlers.handleDrop}
              handleRemoveFile={handlers.handleRemoveFile}
            />
            <JdSelectionPanel 
              jdTab={state.jdTab}
              setJdTab={setters.setJdTab}
              jdText={state.jdText}
              setJdText={setters.setJdText}
              selectedJobId={state.selectedJobId}
              setSelectedJobId={setters.setSelectedJobId}
              savedJobs={queries.savedJobs}
              isLoadingJobs={queries.isLoadingJobs}
            />
          </div>

          {/* Action Button */}
          <div className="flex justify-center pt-4">
            {state.retryJobId && (
              <Button
                type="button"
                variant="outline"
                size="lg"
                onClick={handlers.handleRetry}
                disabled={state.isRetrying}
                className="mr-3 gap-2 font-semibold"
              >
                {state.isRetrying ? <Loader2 className="h-5 w-5 animate-spin" /> : null}
                Retry failed match
              </Button>
            )}
            <Button 
              size="lg" 
              onClick={handlers.handleStartAnalysis} 
              disabled={state.isSubmitDisabled || (!state.hasActiveSub && (state.balance ?? 0) < (state.cvMatchCost ?? 1000))}
              className={`px-8 font-semibold text-base transition-all gap-2 ${
                !state.hasActiveSub && (state.balance ?? 0) < (state.cvMatchCost ?? 1000)
                  ? "bg-rose-500 hover:bg-rose-600 text-white opacity-90 cursor-not-allowed"
                  : ""
              }`}
            >
              {state.isUploading ? (
                <>
                  <Loader2 className="h-5 w-5 animate-spin" />
                  {t('uploadingResume')}
                </>
              ) : !state.hasActiveSub && (state.balance ?? 0) < (state.cvMatchCost ?? 1000) ? (
                <>{t('notEnoughCoinBtn', { balance: state.balance?.toLocaleString(), cost: state.cvMatchCost?.toLocaleString() })}</>
              ) : (
                <>
                  {state.hasActiveSub ? t('startAnalysisFree') : t('startAnalysisCoin', { cost: (state.cvMatchCost ?? 1000).toLocaleString() })}
                  <ArrowRight className="h-5 w-5" />
                </>
              )}
            </Button>
          </div>
        </div>
      )}

      {/* 2. Giao diện Loading (Progress Steps) */}
      {state.step === 'loading' && (
        <MatchingLoadingState 
          progressPercent={state.progressPercent} 
          loadingStep={state.loadingStep} 
        />
      )}

      {/* 3. Giao diện Kết quả (Sử dụng Sub-Components) */}
      {state.step === 'result' && (
        <div className="space-y-6 animate-in fade-in duration-500">
          <div className="flex flex-col sm:flex-row justify-between items-center bg-muted/20 p-4 rounded-lg border gap-4">
            <div className="flex items-center gap-3 text-sm text-muted-foreground">
              <Info className="h-5 w-5 text-primary/70" />
              {t('matchResultInfo')}
            </div>
            <div className="flex gap-3">
              <Button variant="outline" onClick={() => setters.setStep('select')}>
                {t('analyzeAnother')}
              </Button>
            </div>
          </div>

          {state.matchReport && <ResultOverviewCard report={state.matchReport} />}

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2 space-y-6">
              {state.matchReport?.reportKind === 'structured' && (
                <RequirementBreakdown groups={state.matchReport.requirementGroups} />
              )}
            </div>
            <div className="space-y-6">
              {state.matchReport?.reportKind === 'structured' && (
                <CriticalGapsPanel
                  criticalGaps={state.matchReport.criticalGaps}
                  warningFlags={state.matchReport.warningFlags}
                />
              )}

            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default function CvMatchingPage() {
  return (
    <Suspense fallback={<div className="flex h-screen items-center justify-center"><Loader2 className="h-8 w-8 animate-spin text-primary" /></div>}>
      <CvMatchingContent />
    </Suspense>
  );
}
