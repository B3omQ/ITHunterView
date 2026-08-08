'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useGetMyCvs } from '@/hooks/useCv';
import { useUploadFile } from '@/hooks/useUpload';
import { useCreateOptimizeSession, useGetOptimizeHistory, useDeleteOptimizeHistory } from '@/hooks/useOptimizeCv';
import { useWalletBalance } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';
import { optimizeService } from '@/services/optimize.service';
import { cvService } from '@/services/cv.service';
import { CvOptimizationResult } from '@/types/optimize.types';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
import { ListPagination } from '@/components/shared/ListPagination';
import { 
  Sparkles, 
  FileText, 
  Upload, 
  CheckCircle2, 
  AlertTriangle, 
  XCircle, 
  ArrowRight, 
  Loader2, 
  Layers, 
  UserCheck, 
  ListOrdered, 
  Info,
  ChevronRight,
  ShieldCheck,
  RefreshCw,
  History,
  Calendar,
  Eye,
  Trash2,
  Zap,
  Coins
} from 'lucide-react';
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';

export default function StandaloneCvOptimizePage() {
  const t = useTranslations('CandidateOptimizeCv');
  const router = useRouter();
  const { data: myCvsRes, isLoading: isLoadingCvs } = useGetMyCvs();
  const { mutateAsync: uploadFile } = useUploadFile();
  const createSessionMutation = useCreateOptimizeSession();

  const myCvs = myCvsRes?.data || [];

  // Wallet & Coin State
  const { data: walletRes } = useWalletBalance();
  const { data: coinConfigRes } = usePublicCoinConfig();

  const balance = walletRes?.data?.balance ?? 0;
  const activeSubName = walletRes?.data?.activeSubscriptionName;
  const optimizeCost = coinConfigRes?.data?.featureCosts?.cvOptimize ?? 500;
  
  const optimizeLimit = walletRes?.data?.cvOptimizeLimit ?? 0;
  const optimizeUsed = walletRes?.data?.cvOptimizeUsed ?? 0;
  const isSubUnlimited = optimizeLimit === -1;
  const subRemaining = isSubUnlimited ? -1 : Math.max(0, optimizeLimit - optimizeUsed);
  const hasActiveSub = !!activeSubName && (isSubUnlimited || subRemaining > 0);

  // Form State
  const [cvSourceTab, setCvSourceTab] = useState<'saved' | 'upload'>('saved');
  const [selectedCvId, setSelectedCvId] = useState<string>('');
  const [file, setFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  // Result State
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [analysisResult, setAnalysisResult] = useState<CvOptimizationResult | null>(null);

  // History State
  const [historyPage, setHistoryPage] = useState(1);
  const { data: historyRes, isLoading: isLoadingHistory } = useGetOptimizeHistory(historyPage, 6);
  const deleteHistoryMutation = useDeleteOptimizeHistory();
  const [isLoadingHistoryDetail, setIsLoadingHistoryDetail] = useState<string | null>(null);

  const historyData = historyRes?.data;
  const historyItems = historyData?.items || [];
  const totalPages = Math.ceil((historyData?.totalCount || 0) / 6);

  const handleViewHistoryDetail = async (sessionId: string) => {
    setIsLoadingHistoryDetail(sessionId);
    try {
      const res = await optimizeService.getSessionResult(sessionId);
      if (res.data) {
        setAnalysisResult(res.data);
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    } catch (err) {
      toast.error(t('toastLoadDetailFail'));
    } finally {
      setIsLoadingHistoryDetail(null);
    }
  };

  // File Upload Handler
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const selectedFile = e.target.files[0];
      const ext = selectedFile.name.split('.').pop()?.toLowerCase();
      if (ext !== 'pdf' && ext !== 'docx') {
        toast.error(t('toastInvalidFormat'));
        return;
      }
      setFile(selectedFile);
    }
  };

  const handleStartAnalysis = async () => {
    if (!hasActiveSub && balance < optimizeCost) {
      toast.error(t('toastNotEnoughCoin', { cost: optimizeCost.toLocaleString() }));
      return;
    }

    setIsAnalyzing(true);
    try {
      if (cvSourceTab === 'saved') {
        if (!selectedCvId) {
          toast.error(t('toastSelectSavedCv'));
          setIsAnalyzing(false);
          return;
        }
        const res = await createSessionMutation.mutateAsync({ cvId: selectedCvId });
        if (res.data) {
          setAnalysisResult(res.data);
          toast.success(t('toastAnalysisComplete'));
        }
      } else {
        if (!file) {
          toast.error(t('toastSelectFileToUpload'));
          setIsAnalyzing(false);
          return;
        }
        setIsUploading(true);
        const uploadRes = await uploadFile({ file, folderName: 'cv' });
        setIsUploading(false);
        
        if (uploadRes?.data) {
          const res = await createSessionMutation.mutateAsync({ cvUrl: uploadRes.data });
          if (res.data) {
            setAnalysisResult(res.data);
            toast.success('Phân tích CV hoàn tất!');
          }
        } else {
          toast.error(t('toastUploadFail'));
        }
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsAnalyzing(false);
    }
  };

  const formatExampleText = (text?: string): string => {
    if (!text) return '';
    let str = text.trim();

    // Handle empty value case e.g. "Summary": "" or "Summary": ""
    if (/^"?\w+"?\s*:\s*""$/.test(str)) {
      const key = str.match(/^"?(\w+)"?/)?.[1] || 'Mục';
      return `${key}: (Đang bỏ trống trong CV)`;
    }

    // Handle JSON structures (objects or arrays or colon-separated JSON)
    if ((str.startsWith('{') && str.endsWith('}')) || (str.startsWith('[') && str.endsWith(']')) || str.includes('":')) {
      try {
        let keyPrefix = '';
        let jsonPart = str;
        const firstColon = str.indexOf(':');
        if (firstColon > 0 && firstColon < 30) {
          const candidateKey = str.substring(0, firstColon).replace(/["{}]/g, '').trim();
          if (candidateKey) {
            keyPrefix = candidateKey;
            jsonPart = str.substring(firstColon + 1).trim();
          }
        }

        const parsed = JSON.parse(jsonPart.startsWith('[') || jsonPart.startsWith('{') ? jsonPart : str);
        let formatted = '';

        if (Array.isArray(parsed)) {
          formatted = parsed.map(item => {
            if (typeof item === 'object' && item !== null) {
              return Object.entries(item).map(([k, v]) => `${k}: ${v}`).join(' | ');
            }
            return String(item);
          }).join(', ');
        } else if (typeof parsed === 'object' && parsed !== null) {
          formatted = Object.entries(parsed).map(([k, v]) => {
            if (Array.isArray(v)) return `${k}: ${v.join(', ')}`;
            if (typeof v === 'object' && v !== null) {
              return `${k}: ${JSON.stringify(v).replace(/["{}]/g, '')}`;
            }
            return `${k}: ${v}`;
          }).join(' | ');
        } else {
          formatted = String(parsed);
        }

        return keyPrefix && !formatted.toLowerCase().startsWith(keyPrefix.toLowerCase())
          ? `${keyPrefix}: ${formatted}`
          : formatted;
      } catch {
        // Regex cleanup fallback: remove quotes, braces, brackets
        return str
          .replace(/^\s*"?(\w+)"?\s*:\s*""/g, '$1: (Đang bỏ trống)')
          .replace(/^\s*"?(\w+)"?\s*:\s*/g, '$1: ')
          .replace(/["\[\]{}]/g, '')
          .replace(/,\s*/g, ', ')
          .trim();
      }
    }

    // Strip wrapping quotes
    if (str.startsWith('"') && str.endsWith('"') && str.length > 2) {
      str = str.substring(1, str.length - 1);
    }

    return str;
  };

  const getSectionStatusBadge = (status: 'Good' | 'Warning' | 'Missing') => {
    switch (status) {
      case 'Good':
        return (
          <Badge className="bg-emerald-500/10 text-emerald-700 border-emerald-500/20 gap-1 font-medium">
            <CheckCircle2 className="w-3.5 h-3.5" /> {t('statusGood')}
          </Badge>
        );
      case 'Warning':
        return (
          <Badge className="bg-amber-500/10 text-amber-700 border-amber-500/20 gap-1 font-medium">
            <AlertTriangle className="w-3.5 h-3.5" /> {t('statusWarning')}
          </Badge>
        );
      case 'Missing':
        return (
          <Badge className="bg-rose-500/10 text-rose-700 border-rose-500/20 gap-1 font-medium">
            <XCircle className="w-3.5 h-3.5" /> {t('statusMissing')}
          </Badge>
        );
    }
  };

  const getPriorityBadge = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'high':
        return <Badge className="bg-rose-500/15 text-rose-700 font-bold border-rose-200">{t('priorityHigh')}</Badge>;
      case 'medium':
        return <Badge className="bg-amber-500/15 text-amber-700 font-bold border-amber-200">{t('priorityMedium')}</Badge>;
      default:
        return <Badge className="bg-blue-500/15 text-blue-700 font-bold border-blue-200">{t('priorityRecommended')}</Badge>;
    }
  };

  return (
    <div className="w-full pb-12 space-y-8 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b pb-6">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight flex items-center gap-2.5">
            <Sparkles className="h-8 w-8 text-primary animate-pulse" />
            {t('pageTitle')}
          </h1>
          <p className="text-muted-foreground mt-1 text-sm max-w-3xl">
            {t('pageDesc')}
          </p>
        </div>
        {analysisResult && (
          <Button 
            variant="outline" 
            onClick={() => setAnalysisResult(null)}
            className="gap-2 shrink-0 self-start md:self-auto"
          >
            <RefreshCw className="h-4 w-4" /> {t('evaluateAnotherBtn')}
          </Button>
        )}
      </div>

      {/* Loading State */}
      {isAnalyzing && (
        <div className="flex flex-col items-center justify-center min-h-[450px] space-y-4 bg-card rounded-2xl border p-8 text-center shadow-sm">
          <div className="relative">
            <div className="w-20 h-20 rounded-full border-4 border-primary/20 border-t-primary animate-spin flex items-center justify-center">
              <Sparkles className="w-8 h-8 text-primary" />
            </div>
          </div>
          <h2 className="text-xl font-bold tracking-tight">{t('analyzingTitle')}</h2>
          <p className="text-muted-foreground text-sm max-w-md">
            {t('analyzingDesc')}
          </p>
        </div>
      )}

      {/* Input Step: Select CV when no result yet */}
      {!isAnalyzing && !analysisResult && (
        <div className="space-y-6">
          {/* Feature Cost & Wallet Balance Banner */}
          <div className="p-4 rounded-xl bg-gradient-to-r from-purple-500/10 via-amber-500/10 to-transparent border border-purple-500/20 shadow-sm flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-500/20 text-amber-500 shadow-inner">
                {hasActiveSub ? <Zap className="h-5 w-5 text-purple-600 dark:text-purple-400 fill-purple-600/20" /> : <Coins className="h-5 w-5 text-amber-500 fill-amber-500/20" />}
              </div>
              <div>
                <h4 className="font-bold text-foreground text-sm">{t('costLabel')}</h4>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {hasActiveSub 
                    ? <span className="text-purple-600 dark:text-purple-400 font-medium">{isSubUnlimited ? t('freeSubUnlimited', { subName: activeSubName }) : t('freeSubRemaining', { subName: activeSubName, remaining: subRemaining })}</span>
                    : <span>{t('costPerTime', { cost: optimizeCost.toLocaleString() })}</span>}
                </p>
              </div>
            </div>
            
            <div className="text-right flex items-center gap-4">
              <div className="hidden sm:block">
                <p className="text-xs text-muted-foreground">{t('currentBalanceLabel')}</p>
                <p className="text-sm font-bold text-foreground">
                  <strong className={(!hasActiveSub && balance < optimizeCost) ? "text-rose-500" : "text-emerald-600"}>
                    {balance.toLocaleString()} Coin
                  </strong>
                </p>
              </div>
              <Button 
                variant={(!hasActiveSub && balance < optimizeCost) ? "default" : "outline"}
                size="sm"
                className={(!hasActiveSub && balance < optimizeCost) ? "bg-amber-500 hover:bg-amber-600 text-white" : "border-amber-500 text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-950/30"}
                onClick={() => router.push('/candidate/billing')}
              >
                {t('topUpBtn')}
              </Button>
            </div>
          </div>

          <Card className="shadow-sm border-border/80">
          <CardHeader>
            <CardTitle className="text-xl font-bold flex items-center gap-2">
              <FileText className="w-5 h-5 text-primary" /> {t('selectCvTitle')}
            </CardTitle>
            <CardDescription>
              {t('selectCvDesc')}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Tabs Choice */}
            <div className="flex gap-4 border-b pb-4">
              <Button
                type="button"
                variant={cvSourceTab === 'saved' ? 'default' : 'ghost'}
                onClick={() => setCvSourceTab('saved')}
                className="gap-2"
              >
                <FileText className="w-4 h-4" /> {t('tabSavedCv', { count: myCvs.length })}
              </Button>
              <Button
                type="button"
                variant={cvSourceTab === 'upload' ? 'default' : 'ghost'}
                onClick={() => setCvSourceTab('upload')}
                className="gap-2"
              >
                <Upload className="w-4 h-4" /> {t('tabUploadCv')}
              </Button>
            </div>

            {/* Saved CV Option */}
            {cvSourceTab === 'saved' && (
              <div className="space-y-4">
                {isLoadingCvs ? (
                  <div className="flex items-center justify-center p-8 text-muted-foreground gap-2">
                    <Loader2 className="w-5 h-5 animate-spin" /> {t('loadingCvList')}
                  </div>
                ) : myCvs.length === 0 ? (
                  <div className="p-6 text-center border border-dashed rounded-xl space-y-3">
                    <p className="text-muted-foreground text-sm">{t('noSavedCv')}</p>
                    <Button variant="outline" size="sm" onClick={() => setCvSourceTab('upload')}>
                      <Upload className="w-4 h-4 mr-1.5" /> {t('uploadFirstCvBtn')}
                    </Button>
                  </div>
                ) : (
                  <RadioGroup value={selectedCvId} onValueChange={setSelectedCvId} className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {myCvs.map((cv) => (
                      <div
                        key={cv.id}
                        className={`flex items-start space-x-3 p-4 rounded-xl border transition-all cursor-pointer hover:border-primary/50 ${
                          selectedCvId === cv.id ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'bg-card'
                        }`}
                        onClick={() => setSelectedCvId(cv.id)}
                      >
                        <RadioGroupItem value={cv.id} id={cv.id} className="mt-1" />
                        <div className="flex-1 min-w-0">
                          <Label htmlFor={cv.id} className="font-semibold text-base cursor-pointer truncate block">
                            {cv.fileName}
                          </Label>
                          <div className="flex items-center gap-2 mt-1">
                            {cv.isPrimary && (
                              <Badge variant="secondary" className="text-[10px] bg-primary/10 text-primary font-bold">
                                {t('primaryCvBadge')}
                              </Badge>
                            )}
                            <span className="text-xs text-muted-foreground uppercase">{cv.fileType}</span>
                          </div>
                        </div>
                      </div>
                    ))}
                  </RadioGroup>
                )}
              </div>
            )}

            {/* File Upload Option */}
            {cvSourceTab === 'upload' && (
              <div className="space-y-4">
                <div className="border-2 border-dashed border-border rounded-xl p-8 text-center hover:border-primary/50 transition-colors bg-muted/10">
                  <input
                    type="file"
                    id="cv-upload-input"
                    className="hidden"
                    accept=".pdf,.docx"
                    onChange={handleFileChange}
                  />
                  <label htmlFor="cv-upload-input" className="cursor-pointer flex flex-col items-center gap-3">
                    <div className="p-4 rounded-full bg-primary/10 text-primary">
                      <Upload className="w-8 h-8" />
                    </div>
                    <div>
                      <span className="font-semibold text-base text-foreground">
                        {file ? file.name : t('uploadDragDrop')}
                      </span>
                      <p className="text-xs text-muted-foreground mt-1">{t('uploadFormatSupport')}</p>
                    </div>
                  </label>
                </div>
              </div>
            )}

            {/* Action Submit */}
            <div className="flex justify-end pt-4 border-t">
              <Button
                size="lg"
                onClick={handleStartAnalysis}
                disabled={
                  isUploading ||
                  (cvSourceTab === 'saved' && !selectedCvId) ||
                  (cvSourceTab === 'upload' && !file)
                }
                className="gap-2 px-8 shadow-md shadow-primary/20 font-bold"
              >
                {isUploading ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin" /> {t('btnUploading')}
                  </>
                ) : (
                  <>
                    {t('btnAnalyzeOptimize')} <ArrowRight className="w-5 h-5" />
                  </>
                )}
              </Button>
            </div>
          </CardContent>
        </Card>
        </div>
      )}

      {/* Analysis Results View */}
      {!isAnalyzing && analysisResult && (
        <div className="space-y-8 animate-in fade-in duration-500">
          {/* Top Overview Banner */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Score Card */}
            <Card className="bg-gradient-to-br from-primary/10 via-card to-card border-primary/20 shadow-sm flex flex-col justify-between">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-bold uppercase tracking-wider text-muted-foreground">
                  {t('scoreTitle')}
                </CardTitle>
              </CardHeader>
              <CardContent className="flex items-center justify-between pt-0">
                <div>
                  <div className="text-5xl font-black text-primary tracking-tight">
                    {analysisResult.overallScore.toFixed(0)}
                    <span className="text-2xl text-muted-foreground font-normal">/100</span>
                  </div>
                  <p className="text-xs text-muted-foreground font-medium mt-1">
                    {analysisResult.overallScore >= 80 ? t('scoreExcellent') : analysisResult.overallScore >= 60 ? t('scoreGood') : t('scoreNeedsWork')}
                  </p>
                </div>
                <div className="w-16 h-16 rounded-full bg-primary/10 border border-primary/20 flex items-center justify-center text-primary">
                  <ShieldCheck className="w-8 h-8" />
                </div>
              </CardContent>
            </Card>

            {/* Summary Card */}
            <Card className="lg:col-span-2 shadow-sm">
              <CardHeader className="pb-2">
                <CardTitle className="text-base font-bold flex items-center gap-2">
                  <Info className="w-5 h-5 text-primary" /> {t('overviewTitle')}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-foreground/90 leading-relaxed">
                  {analysisResult.summary}
                </p>
                <div className="mt-4 flex items-center gap-2 text-xs text-muted-foreground font-medium">
                  <span className="inline-block w-2 h-2 rounded-full bg-emerald-500"></span>
                  {t('cvFileLabel')} <strong className="text-foreground">{analysisResult.cvFileName || t('defaultCvName')}</strong>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Section 1: Standard Sections Completeness */}
          <Card className="shadow-sm">
            <CardHeader className="border-b bg-muted/20">
              <CardTitle className="text-lg font-bold flex items-center gap-2.5">
                <Layers className="w-5 h-5 text-primary" /> {t('section1Title')}
              </CardTitle>
              <CardDescription>
                {t('section1Desc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="pt-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {analysisResult.sections.map((sec, idx) => (
                  <div key={idx} className="p-4 rounded-xl border bg-card hover:border-primary/40 transition-colors space-y-2">
                    <div className="flex items-center justify-between gap-2">
                      <span className="font-semibold text-base text-foreground flex items-center gap-2">
                        {sec.sectionName}
                      </span>
                      {getSectionStatusBadge(sec.status)}
                    </div>
                    <p className="text-xs text-muted-foreground leading-relaxed pt-1">
                      {sec.feedback}
                    </p>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Section 2: Priority Order Analysis */}
          <Card className="shadow-sm">
            <CardHeader className="border-b bg-muted/20">
              <CardTitle className="text-lg font-bold flex items-center justify-between flex-wrap gap-4">
                <div className="flex items-center gap-2.5">
                  <ListOrdered className="w-5 h-5 text-primary" /> {t('section2Title')}
                </div>
                <Badge variant="outline" className="bg-primary/10 text-primary border-primary/30 font-semibold px-3 py-1">
                  <UserCheck className="w-3.5 h-3.5 mr-1" /> {t('targetAudience', { level: analysisResult.priorityOrder.candidateLevel })}
                </Badge>
              </CardTitle>
              <CardDescription>
                {t('section2Rule')}
              </CardDescription>
            </CardHeader>
            <CardContent className="pt-6 space-y-6">
              {/* Order Status Banner */}
              <div className={`p-4 rounded-xl border flex items-start gap-3.5 ${
                analysisResult.priorityOrder.isOrderOptimal 
                  ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-950 dark:text-emerald-200' 
                  : 'bg-amber-500/10 border-amber-500/20 text-amber-950 dark:text-amber-200'
              }`}>
                {analysisResult.priorityOrder.isOrderOptimal ? (
                  <CheckCircle2 className="w-6 h-6 text-emerald-600 shrink-0 mt-0.5" />
                ) : (
                  <AlertTriangle className="w-6 h-6 text-amber-600 shrink-0 mt-0.5" />
                )}
                <div>
                  <h4 className="font-bold text-base">
                    {analysisResult.priorityOrder.isOrderOptimal 
                      ? t('orderOptimal') 
                      : t('orderNeedsAdjustment')}
                  </h4>
                  <p className="text-sm mt-1 opacity-90 leading-relaxed">
                    {analysisResult.priorityOrder.advice}
                  </p>
                </div>
              </div>

              {/* Order Comparison */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6 pt-2">
                <div className="p-4 rounded-xl border bg-muted/20 space-y-2">
                  <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground block">
                    {t('currentOrderLabel')}
                  </span>
                  <div className="text-sm font-medium text-foreground bg-card p-3 rounded-lg border">
                    {analysisResult.priorityOrder.currentOrderDescription}
                  </div>
                </div>

                <div className="p-4 rounded-xl border bg-primary/5 border-primary/20 space-y-2">
                  <span className="text-xs font-bold uppercase tracking-wider text-primary block">
                    {t('recommendedOrderLabel')}
                  </span>
                  <div className="text-sm font-bold text-primary bg-card p-3 rounded-lg border border-primary/30">
                    {analysisResult.priorityOrder.recommendedOrderDescription}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Section 3: Detailed Improvement Recommendations */}
          <Card className="shadow-sm">
            <CardHeader className="border-b bg-muted/20">
              <CardTitle className="text-lg font-bold flex items-center gap-2.5">
                <Sparkles className="w-5 h-5 text-primary" /> {t('section3Title')}
              </CardTitle>
              <CardDescription>
                {t('section3Desc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="pt-6 space-y-4">
              {analysisResult.recommendations.length === 0 ? (
                <div className="p-6 text-center text-muted-foreground text-sm">
                  {t('noRecommendations')}
                </div>
              ) : (
                analysisResult.recommendations.map((rec, idx) => (
                  <div key={idx} className="p-5 rounded-xl border bg-card hover:border-primary/40 transition-colors space-y-3">
                    <div className="flex items-center justify-between gap-3 flex-wrap">
                      <div className="flex items-center gap-2.5">
                        <Badge variant="outline" className="text-xs font-bold">
                          {rec.category}
                        </Badge>
                        <h4 className="font-bold text-base text-foreground">{rec.title}</h4>
                      </div>
                      {getPriorityBadge(rec.priority)}
                    </div>

                    <p className="text-sm text-muted-foreground leading-relaxed">
                      {rec.description}
                    </p>

                    {/* Example Before / After if available */}
                    {(rec.exampleBefore || rec.exampleAfter) && (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
                        {rec.exampleBefore && (
                          <div className="p-3.5 rounded-xl bg-rose-500/5 border border-rose-200 dark:border-rose-900/40 text-xs space-y-1.5">
                            <span className="font-bold text-rose-700 dark:text-rose-400 block">{t('exampleBefore')}</span>
                            <p className="text-rose-950 dark:text-rose-200 font-medium text-xs leading-relaxed break-words">
                              {formatExampleText(rec.exampleBefore)}
                            </p>
                          </div>
                        )}
                        {rec.exampleAfter && (
                          <div className="p-3.5 rounded-xl bg-emerald-500/5 border border-emerald-200 dark:border-emerald-900/40 text-xs space-y-1.5">
                            <span className="font-bold text-emerald-700 dark:text-emerald-400 block">{t('exampleAfter')}</span>
                            <p className="text-emerald-950 dark:text-emerald-200 font-medium text-xs leading-relaxed break-words">
                              {formatExampleText(rec.exampleAfter)}
                            </p>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* History Section */}
      <Card className="shadow-sm border-border/80">
        <CardHeader>
          <CardTitle className="text-xl font-bold flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <History className="w-5 h-5 text-primary" /> {t('historyTitle')}
            </div>
            {historyData && historyData.totalCount > 0 && (
              <Badge variant="secondary" className="font-semibold text-xs">
                {t('historyTotal', { count: historyData.totalCount })}
              </Badge>
            )}
          </CardTitle>
          <CardDescription>
            {t('historyDesc')}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {isLoadingHistory ? (
            <div className="flex items-center justify-center p-8 text-muted-foreground gap-2">
              <Loader2 className="w-5 h-5 animate-spin" /> {t('loadingHistory')}
            </div>
          ) : historyItems.length === 0 ? (
            <div className="p-8 text-center border border-dashed rounded-xl space-y-2">
              <History className="w-10 h-10 text-muted-foreground mx-auto opacity-40" />
              <p className="font-medium text-foreground text-sm">{t('noHistoryTitle')}</p>
              <p className="text-xs text-muted-foreground">{t('noHistoryDesc')}</p>
            </div>
          ) : (
            <>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {historyItems.map((item) => {
                  const formattedDate = new Date(item.createdAt).toLocaleDateString('vi-VN', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit'
                  });

                  const isHigh = item.overallScore >= 80;
                  const isMed = item.overallScore >= 60;

                  return (
                    <div
                      key={item.sessionId}
                      className="p-5 rounded-2xl border bg-card hover:border-primary/50 transition-all flex flex-col justify-between space-y-4 shadow-sm hover:shadow-md"
                    >
                      <div className="space-y-2">
                        <div className="flex items-start justify-between gap-2">
                          <span className="font-bold text-base text-foreground line-clamp-1 truncate" title={item.cvFileName || 'CV'}>
                            {item.cvFileName || t('defaultHistoryName')}
                          </span>
                          <Badge
                            className={`shrink-0 font-extrabold ${
                              isHigh
                                ? 'bg-emerald-500/10 text-emerald-700 border-emerald-300'
                                : isMed
                                ? 'bg-amber-500/10 text-amber-700 border-amber-300'
                                : 'bg-rose-500/10 text-rose-700 border-rose-300'
                            }`}
                          >
                            {item.overallScore.toFixed(0)}/100
                          </Badge>
                        </div>
                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                          <Calendar className="w-3.5 h-3.5" />
                          <span>{formattedDate}</span>
                        </div>
                      </div>

                      <div className="flex items-center justify-between pt-2 border-t">
                        <Button
                          variant="ghost"
                          size="sm"
                          disabled={isLoadingHistoryDetail === item.sessionId}
                          onClick={() => handleViewHistoryDetail(item.sessionId)}
                          className="gap-1.5 text-xs font-bold text-primary hover:text-primary hover:bg-primary/10 pl-0"
                        >
                          {isLoadingHistoryDetail === item.sessionId ? (
                            <Loader2 className="w-3.5 h-3.5 animate-spin" />
                          ) : (
                            <Eye className="w-3.5 h-3.5" />
                          )}
                          {t('viewReportBtn')}
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => deleteHistoryMutation.mutate(item.sessionId)}
                          className="h-8 w-8 text-muted-foreground hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/30"
                          title="{t('deleteHistoryBtn')}"
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Pagination */}
              <ListPagination
                page={historyPage}
                totalPages={totalPages}
                setPage={setHistoryPage}
              />
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
