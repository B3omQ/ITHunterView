import React, { useState, useEffect } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { useUploadFile } from '@/hooks/useUpload';
import { useGetMyCvs } from '@/hooks/useCv';
import { useSavedJobs } from '@/hooks/useSavedJobs';
import { useMatchCvJd, useGetMatchResult } from '@/hooks/useCvMatch';
import { useWalletBalance } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';
import type { MatchJdRequest, MatchingOutput } from '@/types/cv.types';
import { toast } from 'sonner';
import api from '@/services/api-client';

export type MatchingStep = 'select' | 'loading' | 'result';

export const MATCHING_LOADING_STEPS = [
  'Reading and normalizing CV data...',
  'Extracting key skills and experiences...',
  'Analyzing Job Description requirements...',
  'Executing vector search and similarity matching...',
  'Evaluating match relevance via AI Judge...',
  'Applying credibility and penalty scoring...',
  'Generating final feedback report...'
];

export function useCvMatchingForm() {
  const searchParams = useSearchParams();
  const router = useRouter();
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

  const [step, setStep] = useState<MatchingStep>('select');

  // State CV
  const [cvTab, setCvTab] = useState<string>('upload');
  const [cvText, setCvText] = useState<string>('');
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [selectedCvId, setSelectedCvId] = useState<string>('');
  const [cvFileName, setCvFileName] = useState<string>('');

  // State JD
  const [jdTab, setJdTab] = useState<string>('paste');
  const [jdText, setJdText] = useState<string>('');
  const [selectedJobId, setSelectedJobId] = useState<string>('');

  // Queries & Mutations
  const [isExtracting, setIsExtracting] = useState(false);
  const { data: myCvsData, isLoading: isLoadingCvs } = useGetMyCvs();
  const { data: savedJobsData, isLoading: isLoadingJobs } = useSavedJobs(1, 100);
  const matchMutation = useMatchCvJd();

  const [pollingJobId, setPollingJobId] = useState<string | null>(null);
  const [currentJobId, setCurrentJobId] = useState<string | null>(null);
  const [matchOutput, setMatchOutput] = useState<MatchingOutput | null>(null);
  const [matchedCvId, setMatchedCvId] = useState<string | null>(null);

  const pollQuery = useGetMatchResult(pollingJobId);

  // States cho loading progress
  const [progressPercent, setProgressPercent] = useState(0);
  const [loadingStep, setLoadingStep] = useState(0);

  useEffect(() => {
    const urlJobId = searchParams.get('jobId');
    if (urlJobId && !pollingJobId && step === 'select') {
      setPollingJobId(urlJobId);
      setCurrentJobId(urlJobId);
      setStep('loading');
    }

    const prefill = searchParams.get('prefillJobId');
    if (prefill && step === 'select') {
      setJdTab('saved');
      setSelectedJobId(prefill);
    }
  }, [searchParams, pollingJobId, step]);

  useEffect(() => {
    if (myCvsData?.data && myCvsData.data.length > 0 && !selectedCvId) {
      const primary = myCvsData.data.find((c: any) => c.isPrimary) || myCvsData.data[0];
      setSelectedCvId(primary.id);
    }
  }, [myCvsData, selectedCvId]);



  useEffect(() => {
    if (pollQuery.data?.data) {
      const { status, matchDetails, errorMessage } = pollQuery.data.data;
      if (status === 'Completed' && matchDetails) {
        setPollingJobId(null);
        try {
          const parsed = JSON.parse(matchDetails) as MatchingOutput;
          setMatchOutput(parsed);
          setMatchedCvId(pollQuery.data.data.cvId || null);
          setProgressPercent(100);
          setLoadingStep(MATCHING_LOADING_STEPS.length - 1);
          setTimeout(() => setStep('result'), 600);
        } catch (err) {
          console.error("Parse error details:", err);
          console.error("Raw matchDetails string:", matchDetails);
          toast.error("Failed to parse matching result.");
          setStep('select');
        }
      } else if (status === 'Failed') {
        setPollingJobId(null);
        toast.error(errorMessage || "Matching failed.");
        setStep('select');
      }
    }
  }, [pollQuery.data?.data]);

  // Giả lập Loading Progress
  useEffect(() => {
    let interval: NodeJS.Timeout;
    if (step === 'loading') {
      setProgressPercent(0);
      setLoadingStep(0);

      interval = setInterval(() => {
        setProgressPercent((prev) => {
          if (prev >= 98) {
            return 98; // Hold until API completes
          }
          const nextPercent = prev + Math.floor(Math.random() * 15) + 5;
          const currentStep = Math.min(
            Math.floor((nextPercent / 100) * MATCHING_LOADING_STEPS.length),
            MATCHING_LOADING_STEPS.length - 1
          );
          setLoadingStep(currentStep);
          return Math.min(nextPercent, 98);
        });
      }, 500);
    }
    return () => clearInterval(interval);
  }, [step]);

  // Xử lý Upload File
  const processUpload = async (file: File) => {
    if (file.size > 5 * 1024 * 1024) {
      toast.error('File size exceeds the limit of 5MB');
      return;
    }
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
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Error extracting file text');
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
    setCvFile(null);
    setCvText('');
    setCvFileName('');
  };

  const handleStartAnalysis = async () => {
    // Nếu upload thành công (JSON được lưu trong cvText), cho phép qua vòng gửi xe
    const hasCV = (cvTab === 'upload' && cvText.trim()) || (cvTab === 'paste' && cvText.trim()) || (cvTab === 'saved' && selectedCvId);
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
      payload.cvId = selectedCvId;
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

    setStep('loading');

    try {
      const res = await matchMutation.mutateAsync(payload);
      if (res.success && res.data && res.data !== '00000000-0000-0000-0000-000000000000') {
        toast.success(
          hasActiveSub 
            ? `Bắt đầu phân tích CV-JD (Miễn phí từ gói ${activeSubName}${isSubUnlimited ? "" : `, còn ${Math.max(0, subRemaining - 1)} lượt`})!`
            : `Đã sử dụng ${cvMatchCost.toLocaleString()} Coin để phân tích độ tương thích CV-JD!`
        );
        setPollingJobId(res.data);
        setCurrentJobId(res.data);
      } else {
        toast.error(res.message || 'Không thể gửi yêu cầu phân tích. Vui lòng thử lại sau.');
        setStep('select');
      }
    } catch (err: any) {
      toast.error(err.response?.data?.message || err.message || 'Có lỗi xảy ra khi phân tích CV và JD.');
      setStep('select');
    }
  };

  const isSubmitDisabled = () => {
    const hasCV = (cvTab === 'upload' && cvText.trim()) || (cvTab === 'paste' && cvText.trim()) || (cvTab === 'saved' && selectedCvId);
    const hasJD = (jdTab === 'paste' && jdText.trim()) || (jdTab === 'saved' && selectedJobId);

    let isCvReady = true;
    let isJdReady = true;

    return !hasCV || !hasJD || isExtracting || !isCvReady || !isJdReady;
  };

  return {
    state: {
      step,
      cvTab,
      cvText,
      cvFile,
      selectedCvId,
      cvFileName,
      jdTab,
      jdText,
      selectedJobId,
      currentJobId,
      progressPercent,
      loadingStep,
      matchOutput,
      matchedCvId,
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
      setCvTab,
      setCvText,
      setSelectedCvId,
      setJdTab,
      setJdText,
      setSelectedJobId
    },
    handlers: {
      handleFileChange,
      handleDragOver,
      handleDrop,
      handleRemoveFile,
      handleStartAnalysis
    }
  };
}
