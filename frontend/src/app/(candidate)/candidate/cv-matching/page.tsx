'use client';

import React, { useState, useEffect } from 'react';
import { useUploadFile } from '@/hooks/useUpload';
import { useGetMyCvs } from '@/hooks/useCv';
import { useSavedJobs } from '@/hooks/useSavedJobs';
import { useMatchCvJd } from '@/hooks/useCvMatch';
import type { CvJobMatchScoreResponse, MatchJdRequest } from '@/types/cv.types';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
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

  // Queries & Mutations
  const uploadMutation = useUploadFile();
  const { data: myCvsData, isLoading: isLoadingCvs } = useGetMyCvs();
  const { data: savedJobsData, isLoading: isLoadingJobs } = useSavedJobs(1, 100);
  const matchMutation = useMatchCvJd();
  const [matchResult, setMatchResult] = useState<CvJobMatchScoreResponse | null>(null);

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
          if (prev >= 100) {
            clearInterval(interval);
            setTimeout(() => {
              setStep('result');
            }, 600);
            return 100;
          }
          const nextPercent = prev + Math.floor(Math.random() * 15) + 5;
          const currentStep = Math.min(
            Math.floor((nextPercent / 100) * loadingSteps.length),
            loadingSteps.length - 1
          );
          setLoadingStep(currentStep);
          return Math.min(nextPercent, 100);
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
      setMatchResult(res);
      // Kết thúc loading ảo sớm nếu API trả về nhanh, hoặc đợi loading ảo chạy xong (handled by useEffect later, but for now we just let the API await finish the fake progress if we wanted, or just skip it. 
      // To keep it simple, we let the fake interval run until it hits 100.
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

      {/* 3. Giao diện Kết quả Mock */}
      {step === 'result' && (
        <div className="space-y-8 animate-in fade-in duration-500">
          {/* Header Kết quả */}
          <div className="flex flex-col md:flex-row md:items-center justify-between bg-muted/20 p-6 rounded-lg border gap-4">
            <div className="flex items-center space-x-4">
              <div className="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary border border-primary/20">
                <FileCheck className="h-8 w-8" />
              </div>
              <div>
                <h2 className="text-xl font-bold">Analysis Completed</h2>
                <p className="text-sm text-muted-foreground">
                  Job Match Score generated for: <span className="font-semibold text-foreground">Senior React Developer</span>
                </p>
              </div>
            </div>
            <div className="flex gap-3">
              <Button variant="outline" onClick={() => setStep('select')}>
                Analyze Another
              </Button>
              <Button className="gap-2">
                Improve CV
                <Sparkles className="h-4 w-4" />
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Cột trái (2/3) - Chi tiết đánh giá */}
            <div className="lg:col-span-2 space-y-6">
              <Card className="border-muted">
                <CardHeader>
                  <CardTitle className="text-lg">JD Fit Requirement Breakdown</CardTitle>
                  <CardDescription>Detailed mapping and scores of specific job requirements against your resume.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  {/* Category Group 1 */}
                  <div>
                    <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground mb-3 flex items-center gap-1.5">
                      <Briefcase className="h-3.5 w-3.5" />
                      Must-have Requirements (70% weight)
                    </h3>
                    <div className="space-y-4">
                      {/* Req 1 */}
                      <div className="border-b pb-4 last:border-0 last:pb-0">
                        <div className="flex justify-between items-start gap-2 mb-1.5">
                          <div className="space-y-0.5">
                            <span className="text-sm font-semibold flex items-center gap-1.5">
                              <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                              React.js (tech_skill_tool)
                            </span>
                            <p className="text-xs text-muted-foreground">Req: At least 3 years experience with React and state management.</p>
                          </div>
                          <span className="text-sm font-bold text-emerald-600">1.0 / 1.0</span>
                        </div>
                        <div className="bg-muted p-2 rounded text-xs text-muted-foreground">
                          Evidence: Found React.js listed in 3 professional projects with active roles and technical context (Next.js, Tailwind, Redux).
                        </div>
                      </div>

                      {/* Req 2 */}
                      <div className="border-b pb-4 last:border-0 last:pb-0">
                        <div className="flex justify-between items-start gap-2 mb-1.5">
                          <div className="space-y-0.5">
                            <span className="text-sm font-semibold flex items-center gap-1.5 text-amber-600">
                              <AlertTriangle className="h-4 w-4 text-amber-500" />
                              TypeScript (tech_skill_tool)
                            </span>
                            <p className="text-xs text-muted-foreground">Req: Experience with TypeScript in enterprise apps.</p>
                          </div>
                          <span className="text-sm font-bold text-amber-600">0.5 / 1.0</span>
                        </div>
                        <div className="bg-muted p-2 rounded text-xs text-muted-foreground">
                          Evidence: Mentions TypeScript in the skill section, but lacks explicit project contributions or development context.
                        </div>
                      </div>

                      {/* Req 3 */}
                      <div className="border-b pb-4 last:border-0 last:pb-0">
                        <div className="flex justify-between items-start gap-2 mb-1.5">
                          <div className="space-y-0.5">
                            <span className="text-sm font-semibold flex items-center gap-1.5 text-destructive">
                              <X className="h-4 w-4 text-destructive" />
                              Microfrontends architecture (tech_skill_methodology)
                            </span>
                            <p className="text-xs text-muted-foreground">Req: Experienced in building scalable microfrontends.</p>
                          </div>
                          <span className="text-sm font-bold text-destructive">0.0 / 1.0</span>
                        </div>
                        <div className="bg-muted p-2 rounded text-xs text-muted-foreground">
                          Evidence: No mentions of Microfrontends, Module Federation, or module sharing architectures.
                        </div>
                      </div>
                    </div>
                  </div>

                  <hr />

                  {/* Category Group 2 */}
                  <div>
                    <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground mb-3 flex items-center gap-1.5">
                      <TrendingUp className="h-3.5 w-3.5" />
                      Nice-to-have Requirements (30% weight)
                    </h3>
                    <div className="space-y-4">
                      {/* Req 4 */}
                      <div className="border-b pb-4 last:border-0 last:pb-0">
                        <div className="flex justify-between items-start gap-2 mb-1.5">
                          <div className="space-y-0.5">
                            <span className="text-sm font-semibold flex items-center gap-1.5">
                              <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                              Next.js (tech_skill_tool)
                            </span>
                            <p className="text-xs text-muted-foreground">Req: Experience with Next.js App Router is a strong plus.</p>
                          </div>
                          <span className="text-sm font-bold text-emerald-600">1.0 / 1.0</span>
                        </div>
                        <div className="bg-muted p-2 rounded text-xs text-muted-foreground">
                          Evidence: Built an E-commerce platform using Next.js 14 and App Router with full Server Components migration.
                        </div>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Cột phải (1/3) - Điểm số & Tóm tắt */}
            <div className="space-y-6">
              {/* Điểm JD Fit */}
              <Card className="border-primary/20 bg-primary/5">
                <CardHeader className="text-center pb-2">
                  <CardTitle className="text-base font-bold text-muted-foreground">JD Fit Score</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col items-center justify-center pb-6">
                  <div className="relative flex items-center justify-center">
                    {/* Ring score */}
                    <div className="w-28 h-28 rounded-full border-4 border-primary/20 flex flex-col items-center justify-center bg-background shadow-md">
                      <span className="text-3xl font-extrabold text-primary">{matchResult?.overallScore || 76}</span>
                      <span className="text-[10px] text-muted-foreground uppercase font-bold">out of 100</span>
                    </div>
                  </div>
                  <div className="mt-4 text-center">
                    <span className="inline-flex items-center gap-1 bg-emerald-100 text-emerald-800 text-xs px-2.5 py-1 rounded-full font-semibold">
                      <Check className="h-3 w-3" />
                      Suitable Candidate
                    </span>
                    <p className="text-xs text-muted-foreground mt-3 leading-relaxed">
                      Your profile matches the essential requirements well. Standard gaps can be mitigated easily.
                    </p>
                  </div>
                </CardContent>
              </Card>

              {/* Critical Gaps & suggestions */}
              <Card className="border-muted">
                <CardHeader>
                  <CardTitle className="text-sm font-bold flex items-center gap-1.5">
                    <AlertTriangle className="h-4.5 w-4.5 text-amber-500" />
                    Critical Gaps Detected
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="bg-destructive/5 p-3 rounded-lg border border-destructive/10 space-y-1">
                    <h4 className="text-xs font-bold text-destructive flex items-center gap-1">
                      <X className="h-3.5 w-3.5" />
                      Missing Microfrontends Skill
                    </h4>
                    <p className="text-xs text-muted-foreground">
                      This is a must-have requirement. Add any project related to microfrontends, or alternative module federation designs.
                    </p>
                  </div>

                  <div className="bg-amber-50 p-3 rounded-lg border border-amber-200/50 space-y-1">
                    <h4 className="text-xs font-bold text-amber-800 flex items-center gap-1">
                      <AlertTriangle className="h-3.5 w-3.5" />
                      Weak TypeScript Evidence
                    </h4>
                    <p className="text-xs text-muted-foreground">
                      Only listed in skills section. Add explicit action outcomes incorporating TS in project entries.
                    </p>
                  </div>
                </CardContent>
              </Card>

              {/* Quick info */}
              <div className="rounded-lg border bg-muted/40 p-4 flex gap-3 text-xs text-muted-foreground leading-normal">
                <Info className="h-5 w-5 text-muted-foreground/80 shrink-0 mt-0.5" />
                <span>
                  Matches are evaluated via a 4-tier processing algorithm including embedding matching, threshold filters and LLM checking.
                </span>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
