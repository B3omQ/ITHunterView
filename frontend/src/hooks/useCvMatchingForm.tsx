import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { useGetMyCvs } from '@/hooks/useCv';
import { useSavedJobs } from '@/hooks/useSavedJobs';
import { useMatchCvJd, useGetMatchResult, useRetryMatch } from '@/hooks/useCvMatch';
import { useWalletBalance } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';
import type { CvAnalysisResult, MatchJdRequest, MatchReport, MatchingResultDto } from '@/types/cv.types';
import { toast } from 'sonner';
import api from '@/services/api-client';
import {
  createMatchingIdempotencyKey,
  getMatchingErrorMessage,
  isAmbiguousMatchingError,
  matchingRequestFingerprint,
  type MatchingAttempt,
} from '@/lib/matching-idempotency';
import {
  getMatchingFailureMessage,
  shouldOfferMatchingRetry,
} from '@/lib/matching-failure';
import { normalizeCompletedMatchReport } from '@/lib/matching-report';
import { getMatchingProgress } from '@/lib/matching-progress';

export type MatchingStep = 'select' | 'loading' | 'result';

export function useCvMatchingForm() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const initialJobId = searchParams.get('jobId');
  const initialPrefillJobId = searchParams.get('prefillJobId');
  const { data: walletRes } = useWalletBalance();
  const { data: coinConfigRes } = usePublicCoinConfig();

  const balance = walletRes?.data?.balance ?? 0;
  const activeSubName = walletRes?.data?.activeSubscriptionName;
  const cvMatchCost = coinConfigRes?.data?.featureCosts?.cvJdMatching ?? 1000;
  
  const matchLimit = walletRes?.data?.cvMatchLimit ?? 0;
  const matchUsed = walletRes?.data?.cvMatchUsed ?? 0;
  const isSubUnlimited = matchLimit === -1;
  const subRemaining = isSubUnlimited ? -1 : Math.max(0, matchLimit - matchUsed);
  const hasActiveSub = !!activeSubName && (isSubUnlimited || subRemaining > 0);

  const [step, setStep] = useState<MatchingStep>(initialJobId ? 'loading' : 'select');

  // State CV
  const [cvTab, setCvTab] = useState<string>('upload');
  const [cvText, setCvText] = useState<string>('');
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [selectedCvId, setSelectedCvId] = useState<string>('');
  const [cvFileName, setCvFileName] = useState<string>('');

  // State JD
  const [jdTab, setJdTab] = useState<string>(initialPrefillJobId ? 'saved' : 'paste');
  const [jdText, setJdText] = useState<string>('');
  const [selectedJobId, setSelectedJobId] = useState<string>(initialPrefillJobId ?? '');

  // Queries & Mutations
  const [isExtracting, setIsExtracting] = useState(false);
  const { data: myCvsData, isLoading: isLoadingCvs } = useGetMyCvs();
  const { data: savedJobsData, isLoading: isLoadingJobs } = useSavedJobs(1, 100);
  const matchMutation = useMatchCvJd();
  const retryMutation = useRetryMatch();

  const [pollingJobId, setPollingJobId] = useState<string | null>(initialJobId);
  const [currentJobId, setCurrentJobId] = useState<string | null>(initialJobId);
  const [matchReport, setMatchReport] = useState<MatchReport | null>(null);
  const [cvAnalysis, setCvAnalysis] = useState<CvAnalysisResult | null>(null);
  const [matchedCvId, setMatchedCvId] = useState<string | null>(null);
  const [retryJobId, setRetryJobId] = useState<string | null>(null);
  const [resultError, setResultError] = useState<string | null>(null);
  const submitAttemptRef = useRef<MatchingAttempt | null>(null);
  const retryAttemptRef = useRef<MatchingAttempt | null>(null);
  const submitInFlightRef = useRef(false);
  const retryInFlightRef = useRef(false);

  const clearPendingAttempts = () => {
    submitAttemptRef.current = null;
    retryAttemptRef.current = null;
  };

  const pollQuery = useGetMatchResult(pollingJobId);

  const defaultCvId = myCvsData?.data?.find((cv) => cv.isPrimary)?.id
    ?? myCvsData?.data?.[0]?.id
    ?? '';
  const effectiveSelectedCvId = selectedCvId || defaultCvId;

  const resolvePollingResult = useCallback((result: MatchingResultDto, jobId: string) => {
    if (result.status === 'Completed') {
      setPollingJobId(null);
      setRetryJobId(null);
      setResultError(null);
      if (!result.report) {
        console.error('Completed matching response is missing its typed report', {
          jobId,
          status: result.status,
          reportKind: result.reportKind ?? 'missing',
          matchMethod: result.matchMethod ?? 'missing',
        });
      }
      setMatchReport(normalizeCompletedMatchReport(result));
      setCvAnalysis(result.cvAnalysis ?? null);
      setMatchedCvId(result.cvId || null);
      setTimeout(() => setStep('result'), 600);
      return;
    }

    if (result.status === 'Failed') {
      setPollingJobId(null);
      setRetryJobId(shouldOfferMatchingRetry(result) ? jobId : null);
      const message = getMatchingFailureMessage(result.errorCode, result.errorMessage || 'Matching failed.');
      setResultError(message);
      toast.error(message);
      setStep('select');
    }
  }, []);

  useEffect(() => {
    const result = pollQuery.data?.data;
    if (result && pollingJobId) {
      const jobId = pollingJobId;
      const timer = setTimeout(() => resolvePollingResult(result, jobId), 0);
      return () => clearTimeout(timer);
    }
  }, [pollQuery.data?.data, pollingJobId, resolvePollingResult]);

  const polledResult = pollQuery.data?.data;
  const matchingProgress = getMatchingProgress(
    polledResult?.status,
    polledResult?.processingStage,
    step === 'loading' && !pollingJobId,
  );

  // Xử lý Upload File
  const processUpload = async (file: File) => {
    if (file.size > 5 * 1024 * 1024) {
      toast.error('File size exceeds the limit of 5MB');
      return;
    }
    clearPendingAttempts();
    setCvFile(file);
    setCvFileName(file.name);
    setIsExtracting(true);

    try {
      const formData = new FormData();
      formData.append('file', file);

      const res = await api.post('/api/cvs/extract-text', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      if (res.data?.success && res.data?.data) {
        setCvText(res.data.data);
        toast.success('Resume text extracted successfully');
      } else {
        toast.error(res.data?.message || 'Failed to parse resume');
      }
    } catch (err: unknown) {
      toast.error(getMatchingErrorMessage(err, 'Error extracting file text'));
    } finally {
      setIsExtracting(false);
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;
    await processUpload(files[0]);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (!files || files.length === 0) return;
    await processUpload(files[0]);
  };

  const handleRemoveFile = () => {
    clearPendingAttempts();
    setCvFile(null);
    setCvText('');
    setCvFileName('');
  };

  const handleStartAnalysis = async () => {
    // Nếu upload thành công (JSON được lưu trong cvText), cho phép qua vòng gửi xe
    const hasCV = (cvTab === 'upload' && cvText.trim()) || (cvTab === 'paste' && cvText.trim()) || (cvTab === 'saved' && effectiveSelectedCvId);
    const hasJD = (jdTab === 'paste' && jdText.trim()) || (jdTab === 'saved' && selectedJobId);

    if (!hasCV) {
      toast.error('Please upload, paste, or select a resume first');
      return;
    }
    if (!hasJD) {
      toast.error('Please paste a job description or select a saved job first');
      return;
    }

    if ((cvTab === 'paste' || cvTab === 'upload') && cvText.trim().length < 100) {
      toast.error('Resume text is too short. Please provide at least 100 characters or upload a valid CV.');
      return;
    }

    if (jdTab === 'paste' && jdText.trim().length < 100) {
      toast.error('Job description is too short. Please provide at least 100 characters.');
      return;
    }

    const payload: MatchJdRequest = {};

    if (cvTab === 'upload' || cvTab === 'paste') {
      payload.cvText = cvText;
      if (cvTab === 'upload' && cvFileName) {
        payload.cvFileName = cvFileName;
      }
    } else if (cvTab === 'saved') {
      payload.cvId = effectiveSelectedCvId;
    }

    if (jdTab === 'paste') payload.rawJdText = jdText;
    else if (jdTab === 'saved') payload.jobId = selectedJobId;

    if (!hasActiveSub && balance < cvMatchCost) {
      toast.error(
        <div className="flex flex-col gap-1.5">
          <span className="font-semibold text-rose-600 dark:text-rose-400">Số dư Coin không đủ!</span>
          <span className="text-xs text-muted-foreground">
            Bạn có {balance.toLocaleString()} Coin nhưng tính năng này yêu cầu {cvMatchCost.toLocaleString()} Coin.
          </span>
          <div className="flex items-center gap-2 mt-1">
            <button 
              onClick={() => router.push('/candidate/top-up')}
              className="px-3 py-1 bg-amber-500 hover:bg-amber-600 text-white font-medium text-xs rounded-lg shadow-sm transition"
            >
              Nạp ngay
            </button>
            <button 
              onClick={() => router.push('/candidate/pricing')}
              className="px-3 py-1 bg-purple-600 hover:bg-purple-700 text-white font-medium text-xs rounded-lg shadow-sm transition"
            >
              Xem Gói cước
            </button>
          </div>
        </div>,
        { duration: 6000 }
      );
      return;
    }

    if (submitInFlightRef.current) return;

    const fingerprint = matchingRequestFingerprint(payload);
    if (submitAttemptRef.current?.fingerprint !== fingerprint) {
      submitAttemptRef.current = {
        key: createMatchingIdempotencyKey('submit'),
        fingerprint,
      };
    }

    submitInFlightRef.current = true;
    setResultError(null);
    setMatchReport(null);
    setCvAnalysis(null);
    setStep('loading');

    try {
      const res = await matchMutation.mutateAsync({
        data: payload,
        idempotencyKey: submitAttemptRef.current.key,
      });
      if (res.success && res.data && res.data !== '00000000-0000-0000-0000-000000000000') {
        submitAttemptRef.current = null;
        toast.success(
          hasActiveSub 
            ? `Bắt đầu phân tích CV-JD (Miễn phí từ gói ${activeSubName}${isSubUnlimited ? "" : `, còn ${Math.max(0, subRemaining - 1)} lượt`})!`
            : `Đã sử dụng ${cvMatchCost.toLocaleString()} Coin để phân tích độ tương thích CV-JD!`
        );
        setPollingJobId(res.data);
        setCurrentJobId(res.data);
      } else {
        submitAttemptRef.current = null;
        toast.error(res.message || 'Không thể gửi yêu cầu phân tích. Vui lòng thử lại sau.');
        setStep('select');
      }
    } catch (err: unknown) {
      if (!isAmbiguousMatchingError(err)) submitAttemptRef.current = null;
      toast.error(getMatchingErrorMessage(err, 'Có lỗi xảy ra khi phân tích CV và JD.'));
      setStep('select');
    } finally {
      submitInFlightRef.current = false;
    }
  };

  const isSubmitDisabled = () => {
    const hasCV = (cvTab === 'upload' && cvText.trim()) || (cvTab === 'paste' && cvText.trim()) || (cvTab === 'saved' && effectiveSelectedCvId);
    const hasJD = (jdTab === 'paste' && jdText.trim()) || (jdTab === 'saved' && selectedJobId);

    return !hasCV || !hasJD || isExtracting;
  };

  const handleRetry = async () => {
    if (!retryJobId) return;
    if (retryInFlightRef.current) return;

    const fingerprint = `retry:${retryJobId}`;
    if (retryAttemptRef.current?.fingerprint !== fingerprint) {
      retryAttemptRef.current = {
        key: createMatchingIdempotencyKey('retry'),
        fingerprint,
      };
    }

    retryInFlightRef.current = true;
    setStep('loading');
    try {
      const res = await retryMutation.mutateAsync({
        jobId: retryJobId,
        idempotencyKey: retryAttemptRef.current.key,
      });
      if (res.success && res.data) {
        retryAttemptRef.current = null;
        setResultError(null);
        setRetryJobId(null);
        setPollingJobId(res.data);
        setCurrentJobId(res.data);
      } else {
        retryAttemptRef.current = null;
        setResultError(res.message || 'Retry could not be accepted.');
        toast.error(res.message || 'Retry could not be accepted.');
        setStep('select');
      }
    } catch (err: unknown) {
      if (!isAmbiguousMatchingError(err)) retryAttemptRef.current = null;
      const message = getMatchingErrorMessage(err, 'Retry could not be accepted.');
      setResultError(message);
      toast.error(message);
      setStep('select');
    } finally {
      retryInFlightRef.current = false;
    }
  };

  const updateCvTab = (value: string) => {
    clearPendingAttempts();
    setCvTab(value);
  };
  const updateCvText = (value: string) => {
    clearPendingAttempts();
    setCvText(value);
  };
  const updateSelectedCvId = (value: string) => {
    clearPendingAttempts();
    setSelectedCvId(value);
  };
  const updateJdTab = (value: string) => {
    clearPendingAttempts();
    setJdTab(value);
  };
  const updateJdText = (value: string) => {
    clearPendingAttempts();
    setJdText(value);
  };
  const updateSelectedJobId = (value: string) => {
    clearPendingAttempts();
    setSelectedJobId(value);
  };

  return {
    state: {
      step,
      cvTab,
      cvText,
      cvFile,
      selectedCvId: effectiveSelectedCvId,
      cvFileName,
      jdTab,
      jdText,
      selectedJobId,
      currentJobId,
      matchingProgress,
      matchReport,
      cvAnalysis,
      matchedCvId,
      retryJobId,
      resultError,
      isRetrying: retryMutation.isPending,
      isUploading: isExtracting,
      isSubmitDisabled: isSubmitDisabled(),
      balance,
      activeSubName,
      cvMatchCost,
      matchLimit,
      matchUsed,
      isSubUnlimited,
      subRemaining,
      hasActiveSub
    },
    queries: {
      myCvs: myCvsData?.data || [],
      isLoadingCvs,
      savedJobs: savedJobsData?.data || [],
      isLoadingJobs
    },
    setters: {
      setStep,
      setCvTab: updateCvTab,
      setCvText: updateCvText,
      setSelectedCvId: updateSelectedCvId,
      setJdTab: updateJdTab,
      setJdText: updateJdText,
      setSelectedJobId: updateSelectedJobId
    },
    handlers: {
      handleFileChange,
      handleDragOver,
      handleDrop,
      handleRemoveFile,
      handleStartAnalysis,
      handleRetry
    }
  };
}
