import { useState, useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { useUploadFile } from '@/hooks/useUpload';
import { useGetMyCvs } from '@/hooks/useCv';
import { useSavedJobs } from '@/hooks/useSavedJobs';
import { useMatchCvJd, useGetMatchResult } from '@/hooks/useCvMatch';
import type { MatchJdRequest, MatchingOutput } from '@/types/cv.types';
import { toast } from 'sonner';

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

  const [step, setStep] = useState<MatchingStep>('select');
  
  // State CV
  const [cvTab, setCvTab] = useState<string>('upload');
  const [cvText, setCvText] = useState<string>('');
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [cvUrl, setCvUrl] = useState<string>('');
  const [selectedCvId, setSelectedCvId] = useState<string>('');
  const [cvFileName, setCvFileName] = useState<string>('');

  // State JD
  const [jdTab, setJdTab] = useState<string>('paste');
  const [jdText, setJdText] = useState<string>('');
  const [selectedJobId, setSelectedJobId] = useState<string>('');

  // Queries & Mutations
  const uploadMutation = useUploadFile();
  const { data: myCvsData, isLoading: isLoadingCvs } = useGetMyCvs();
  const { data: savedJobsData, isLoading: isLoadingJobs } = useSavedJobs(1, 100);
  const matchMutation = useMatchCvJd();
  
  const [pollingJobId, setPollingJobId] = useState<string | null>(null);
  const [matchOutput, setMatchOutput] = useState<MatchingOutput | null>(null);
  
  const pollQuery = useGetMatchResult(pollingJobId);

  // States cho loading progress
  const [progressPercent, setProgressPercent] = useState(0);
  const [loadingStep, setLoadingStep] = useState(0);

  useEffect(() => {
    const urlJobId = searchParams.get('jobId');
    if (urlJobId && !pollingJobId && step === 'select') {
      setPollingJobId(urlJobId);
      setStep('loading');
    }
  }, [searchParams, pollingJobId, step]);

  useEffect(() => {
    if (pollQuery.data?.data) {
      const { status, matchDetails, errorMessage } = pollQuery.data.data;
      if (status === 'Completed' && matchDetails) {
        setPollingJobId(null);
        try {
          const parsed = JSON.parse(matchDetails) as MatchingOutput;
          setMatchOutput(parsed);
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

    try {
      const res = await uploadMutation.mutateAsync({ file, folderName: 'resumes' });
      if (res.success && res.data) {
        setCvUrl(res.data);
        toast.success('Resume uploaded successfully');
      } else {
        toast.error(res.message || 'Failed to upload resume');
      }
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Error uploading file');
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
    setCvUrl('');
    setCvFileName('');
  };

  const handleStartAnalysis = async () => {
    const hasCV = (cvTab === 'upload' && cvUrl) || (cvTab === 'paste' && cvText.trim()) || (cvTab === 'saved' && selectedCvId);
    const hasJD = (jdTab === 'paste' && jdText.trim()) || (jdTab === 'saved' && selectedJobId);

    if (!hasCV) {
      toast.error('Please upload, paste, or select a resume first');
      return;
    }
    if (!hasJD) {
      toast.error('Please paste a job description or select a saved job first');
      return;
    }
    
    if (cvTab === 'paste' && cvText.trim().length < 100) {
      toast.error('Resume text is too short. Please provide at least 100 characters.');
      return;
    }
    
    if (jdTab === 'paste' && jdText.trim().length < 100) {
      toast.error('Job description is too short. Please provide at least 100 characters.');
      return;
    }

    const payload: MatchJdRequest = {};
    
    if (cvTab === 'upload') payload.cvUrl = cvUrl;
    else if (cvTab === 'paste') payload.cvText = cvText;
    else if (cvTab === 'saved') payload.cvId = selectedCvId;
    
    if (jdTab === 'paste') payload.rawJdText = jdText;
    else if (jdTab === 'saved') payload.jobId = selectedJobId;

    setStep('loading');
    
    try {
      const res = await matchMutation.mutateAsync(payload);
      if (res.data) {
        setPollingJobId(res.data);
      }
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Error matching CV and JD');
      setStep('select');
    }
  };

  const isSubmitDisabled = () => {
    const hasCV = (cvTab === 'upload' && cvUrl) || (cvTab === 'paste' && cvText.trim()) || (cvTab === 'saved' && selectedCvId);
    const hasJD = (jdTab === 'paste' && jdText.trim()) || (jdTab === 'saved' && selectedJobId);
    return !hasCV || !hasJD || uploadMutation.isPending;
  };

  return {
    state: {
      step,
      cvTab,
      cvText,
      cvFile,
      cvUrl,
      selectedCvId,
      cvFileName,
      jdTab,
      jdText,
      selectedJobId,
      progressPercent,
      loadingStep,
      matchOutput,
      isUploading: uploadMutation.isPending,
      isSubmitDisabled: isSubmitDisabled()
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
