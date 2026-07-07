'use client';

import React, { useState, useEffect } from 'react';
import { useUploadFile } from '@/hooks/useUpload';
import { useGetMyCvs } from '@/hooks/useCv';
import { useSavedJobs } from '@/hooks/useSavedJobs';
import { useMatchCvJd, useGetMatchResult } from '@/hooks/useCvMatch';
import type { MatchJdRequest, MatchingOutput } from '@/types/cv.types';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Progress } from '@/components/ui/progress';
import { ScrollArea } from '@/components/ui/scroll-area';
import { toast } from 'sonner';
import { 
  UploadCloud, 
  FileText, 
  CheckCircle2, 
  AlertTriangle, 
  Loader2, 
  Sparkles, 
  ArrowRight, 
  Trash2, 
  Briefcase, 
  Check, 
  X,
  FileCheck,
  ChevronRight,
  TrendingUp,
  Info
} from 'lucide-react';

import { ResultOverviewCard } from './components/ResultOverviewCard';
import { RequirementBreakdown } from './components/RequirementBreakdown';
import { CriticalGapsPanel } from './components/CriticalGapsPanel';
import { ImprovementSuggestions } from './components/ImprovementSuggestions';
import { PenaltyWarningPanel } from './components/PenaltyWarningPanel';

type Step = 'select' | 'loading' | 'result';

export default function CvMatchingPage() {
  const [step, setStep] = useState<Step>('select');
  
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

  // LLM Config đã được cấu hình trong appsettings (Backend)

  // Queries & Mutations
  const uploadMutation = useUploadFile();
  const { data: myCvsData, isLoading: isLoadingCvs } = useGetMyCvs();
  const { data: savedJobsData, isLoading: isLoadingJobs } = useSavedJobs(1, 100);
  const matchMutation = useMatchCvJd();
  
  const [pollingJobId, setPollingJobId] = useState<string | null>(null);
  const [matchOutput, setMatchOutput] = useState<MatchingOutput | null>(null);
  
  const pollQuery = useGetMatchResult(pollingJobId);

  useEffect(() => {
    if (pollQuery.data?.data) {
      const { status, matchDetails, errorMessage } = pollQuery.data.data;
      if (status === 'Completed' && matchDetails) {
        setPollingJobId(null);
        try {
          const parsed = JSON.parse(matchDetails) as MatchingOutput;
          setMatchOutput(parsed);
          setProgressPercent(100);
          setLoadingStep(6);
          setTimeout(() => setStep('result'), 600);
        } catch (err) {
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

  // States cho loading progress
  const [progressPercent, setProgressPercent] = useState(0);
  const [loadingStep, setLoadingStep] = useState(0);
  const loadingSteps = [
    'Reading and normalizing CV data...',
    'Extracting key skills and experiences...',
    'Analyzing Job Description requirements...',
    'Executing vector search and similarity matching...',
    'Evaluating match relevance via AI Judge...',
    'Applying credibility and penalty scoring...',
    'Generating final feedback report...'
  ];

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
            Math.floor((nextPercent / 100) * loadingSteps.length),
            loadingSteps.length - 1
          );
          setLoadingStep(currentStep);
          return Math.min(nextPercent, 98);
        });
      }, 500);
    }
    return () => clearInterval(interval);
  }, [step]);

  // Xử lý Upload File
  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;
    const file = files[0];
    
    // Giới hạn 5MB
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

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (!files || files.length === 0) return;
    const file = files[0];

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

  // Reset CV File
  const handleRemoveFile = () => {
    setCvFile(null);
    setCvUrl('');
    setCvFileName('');
  };

  // Bắt đầu phân tích
  const handleStartAnalysis = async () => {
    // Validate dữ liệu
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

  // Kiểm tra nút Submit có sẵn sàng hay không
  const isSubmitDisabled = () => {
    const hasCV = (cvTab === 'upload' && cvUrl) || (cvTab === 'paste' && cvText.trim()) || (cvTab === 'saved' && selectedCvId);
    const hasJD = (jdTab === 'paste' && jdText.trim()) || (jdTab === 'saved' && selectedJobId);
    return !hasCV || !hasJD || uploadMutation.isPending;
  };

  // Lấy các CV đã lưu dạng dropdown items
  const myCvs = myCvsData?.data || [];
  // Lấy các Saved Jobs
  const savedJobs = savedJobsData?.data || [];

  return (
    <div className="max-w-5xl mx-auto w-full py-8 px-4">
      {/* 1. Tiêu đề chính */}
      <div className="flex flex-col space-y-2 mb-8 text-center md:text-left">
        <h1 className="text-3xl font-extrabold tracking-tight flex items-center justify-center md:justify-start gap-2">
          <Sparkles className="h-8 w-8 text-primary animate-pulse" />
          AI CV-JD Matching
        </h1>
        <p className="text-muted-foreground text-sm max-w-2xl">
          Evaluate the fit between your resume and job requirements using standard vector search and LLM scoring methodologies.
        </p>
      </div>

      {step === 'select' && (
        <div className="space-y-8">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            {/* Cột Trái - CV Selection */}
            <div className="flex flex-col space-y-3">
              <Label className="text-base font-semibold">Select Your Resume (CV)</Label>
              <Tabs value={cvTab} onValueChange={setCvTab} className="w-full">
                <TabsList className="grid w-full grid-cols-3">
                  <TabsTrigger value="upload">Upload File</TabsTrigger>
                  <TabsTrigger value="paste">Paste Text</TabsTrigger>
                  <TabsTrigger value="saved">My Saved</TabsTrigger>
                </TabsList>

                {/* Tab: Upload File */}
                <TabsContent value="upload" className="mt-4">
                  {!cvFile ? (
                    <div 
                      onDragOver={handleDragOver}
                      onDrop={handleDrop}
                      className="border-2 border-dashed border-input rounded-lg p-8 text-center hover:bg-muted/40 transition cursor-pointer flex flex-col items-center justify-center min-h-[220px]"
                    >
                      <input 
                        type="file" 
                        id="cv-upload-input" 
                        className="hidden" 
                        accept=".pdf,.docx,.txt"
                        onChange={handleFileChange}
                      />
                      <label htmlFor="cv-upload-input" className="cursor-pointer flex flex-col items-center justify-center w-full h-full">
                        <UploadCloud className="h-10 w-10 text-muted-foreground mb-4" />
                        <span className="text-sm font-medium">Drag & drop your file here, or <span className="text-primary underline">browse</span></span>
                        <span className="text-xs text-muted-foreground mt-2">Supports PDF, DOCX, TXT up to 5MB</span>
                      </label>
                    </div>
                  ) : (
                    <div className="border border-input rounded-lg p-6 flex items-center justify-between bg-muted/20 min-h-[120px]">
                      <div className="flex items-center space-x-4">
                        <div className="p-3 bg-primary/10 rounded-md text-primary">
                          <FileText className="h-6 w-6" />
                        </div>
                        <div className="flex flex-col max-w-[280px]">
                          <span className="font-medium text-sm truncate">{cvFileName}</span>
                          {uploadMutation.isPending ? (
                            <span className="text-xs text-muted-foreground flex items-center gap-1.5 mt-1">
                              <Loader2 className="h-3 w-3 animate-spin text-primary" />
                              Uploading to Cloudinary...
                            </span>
                          ) : (
                            <span className="text-xs text-emerald-600 font-medium flex items-center gap-1.5 mt-1">
                              <Check className="h-3.5 w-3.5" />
                              Uploaded successfully
                            </span>
                          )}
                        </div>
                      </div>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={handleRemoveFile} 
                        disabled={uploadMutation.isPending}
                        className="text-muted-foreground hover:text-destructive"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  )}
                </TabsContent>

                {/* Tab: Paste Text */}
                <TabsContent value="paste" className="mt-4">
                  <Textarea
                    placeholder="Paste the raw text of your resume here..."
                    className="min-h-[220px] font-sans resize-none"
                    value={cvText}
                    onChange={(e) => setCvText(e.target.value)}
                  />
                </TabsContent>

                {/* Tab: My Saved CVs */}
                <TabsContent value="saved" className="mt-4">
                  <div className="space-y-4">
                    <Label className="text-xs text-muted-foreground">Choose a resume saved in your profile</Label>
                    {isLoadingCvs ? (
                      <div className="flex items-center justify-center p-8 border rounded-lg bg-muted/10">
                        <Loader2 className="h-6 w-6 animate-spin text-primary" />
                      </div>
                    ) : myCvs.length === 0 ? (
                      <div className="text-center p-8 border border-dashed rounded-lg">
                        <p className="text-sm text-muted-foreground">No saved resumes found.</p>
                      </div>
                    ) : (
                      <Select value={selectedCvId} onValueChange={(val) => setSelectedCvId(val || '')}>
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder="Select a resume" />
                        </SelectTrigger>
                        <SelectContent>
                          {myCvs.map((cv) => (
                            <SelectItem key={cv.id} value={cv.id}>
                              {cv.fileName || `Resume - ${new Date(cv.createdAt).toLocaleDateString()}`}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  </div>
                </TabsContent>
              </Tabs>
            </div>

            {/* Cột Phải - JD Selection */}
            <div className="flex flex-col space-y-3">
              <Label className="text-base font-semibold">Select Job Description (JD)</Label>
              <Tabs value={jdTab} onValueChange={setJdTab} className="w-full">
                <TabsList className="grid w-full grid-cols-2">
                  <TabsTrigger value="paste">Paste JD Text</TabsTrigger>
                  <TabsTrigger value="saved">From Saved Jobs</TabsTrigger>
                </TabsList>

                {/* Tab: Paste JD Text */}
                <TabsContent value="paste" className="mt-4">
                  <Textarea
                    placeholder="Paste the Job Description requirements here..."
                    className="min-h-[220px] font-sans resize-none"
                    value={jdText}
                    onChange={(e) => setJdText(e.target.value)}
                  />
                </TabsContent>

                {/* Tab: Saved Jobs */}
                <TabsContent value="saved" className="mt-4">
                  <div className="space-y-4">
                    <Label className="text-xs text-muted-foreground">Select one of your bookmarked job postings</Label>
                    {isLoadingJobs ? (
                      <div className="flex items-center justify-center p-8 border rounded-lg bg-muted/10">
                        <Loader2 className="h-6 w-6 animate-spin text-primary" />
                      </div>
                    ) : savedJobs.length === 0 ? (
                      <div className="text-center p-8 border border-dashed rounded-lg">
                        <p className="text-sm text-muted-foreground">No saved jobs found.</p>
                      </div>
                    ) : (
                      <Select value={selectedJobId} onValueChange={(val) => setSelectedJobId(val || '')}>
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder="Select a job" />
                        </SelectTrigger>
                        <SelectContent>
                          {savedJobs.map((job) => (
                            <SelectItem key={job.jobId} value={job.jobId}>
                              {job.title} - {job.companyName}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  </div>
                </TabsContent>
              </Tabs>
            </div>
          </div>



          {/* Action Button */}
          <div className="flex justify-center pt-4">
            <Button 
              size="lg" 
              onClick={handleStartAnalysis} 
              disabled={isSubmitDisabled()}
              className="px-8 font-semibold text-base transition-all gap-2"
            >
              {uploadMutation.isPending ? (
                <>
                  <Loader2 className="h-5 w-5 animate-spin" />
                  Uploading Resume...
                </>
              ) : (
                <>
                  Start Analysis
                  <ArrowRight className="h-5 w-5" />
                </>
              )}
            </Button>
          </div>
        </div>
      )}

      {/* 2. Giao diện Loading (Progress Steps) */}
      {step === 'loading' && (
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
              {loadingSteps.map((stepMsg, idx) => {
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
      )}

      {/* 3. Giao diện Kết quả (Sử dụng Sub-Components) */}
      {step === 'result' && (
        <div className="space-y-6 animate-in fade-in duration-500">
          <div className="flex flex-col sm:flex-row justify-between items-center bg-muted/20 p-4 rounded-lg border gap-4">
            <div className="flex items-center gap-3 text-sm text-muted-foreground">
              <Info className="h-5 w-5 text-primary/70" />
              This match result is generated by our AI using your provided CV and Job Description.
            </div>
            <div className="flex gap-3">
              <Button variant="outline" onClick={() => setStep('select')}>
                Analyze Another
              </Button>
            </div>
          </div>

          {matchOutput?.jdFit && (
            <>
              <ResultOverviewCard jdFit={matchOutput.jdFit} />
              <PenaltyWarningPanel penalties={matchOutput.jdFit.penalties} />
            </>
          )}

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2 space-y-6">
              {matchOutput?.jdFit && (
                <RequirementBreakdown scores={matchOutput.jdFit.requirementScores} />
              )}
              {matchOutput?.improvements && matchOutput.improvements.length > 0 && (
                <ImprovementSuggestions improvements={matchOutput.improvements} />
              )}
            </div>
            <div className="space-y-6">
              {matchOutput?.jdFit && (
                <CriticalGapsPanel 
                  criticalGaps={matchOutput.jdFit.criticalGaps} 
                  penalties={matchOutput.jdFit.penalties} 
                />
              )}
              <Card className="bg-muted/30 border-muted">
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm font-bold flex items-center gap-2">
                    <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                    How is this calculated?
                  </CardTitle>
                </CardHeader>
                <CardContent className="text-xs text-muted-foreground leading-relaxed space-y-2">
                  <p>Our AI evaluates your CV against the JD using a 4-tier processing algorithm:</p>
                  <ul className="list-disc pl-4 space-y-1">
                    <li>Extracts requirements into <span className="font-semibold text-foreground">Must-have</span> (70%) and <span className="font-semibold text-foreground">Nice-to-have</span> (30%).</li>
                    <li>Performs embedding matching to locate relevant experience.</li>
                    <li>Analyzes evidence quality and assigns a score from 0.0 to 1.0.</li>
                    <li>Applies penalties for weak evidence or missing critical skills.</li>
                  </ul>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
