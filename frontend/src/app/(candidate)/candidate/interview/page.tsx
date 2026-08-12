'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Image from 'next/image';
import {
  useGetInterviewSessions,
  useCreateInterviewSession,
  useDeleteInterviewSession,
  useRenameInterviewSession,
} from '@/hooks/useInterview';
import { useGetMyCvs } from '@/hooks/useCv';
import { usePublicJobs } from '@/hooks/usePublicJobs';
import { useJobDetail } from '@/hooks/useJobDetail';
import { useWalletBalance } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

import { Card, CardContent } from '@/components/ui/card';
import { ListPagination } from '@/components/shared/ListPagination';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
import { EmptyState } from '@/components/shared/EmptyState';


import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Command, CommandInput, CommandList, CommandEmpty, CommandGroup, CommandItem } from '@/components/ui/command';
import { ScrollArea } from '@/components/ui/scroll-area';
import {
  MessageSquare,
  Plus,
  Play,
  Calendar,
  Layers,
  Cpu,
  ArrowRight,
  BrainCircuit,
  Briefcase,
  FileText,
  Trash2,
  ExternalLink,
  DollarSign,
  MapPin,
  Info,
  Sparkles,
  X,
  Check,
  ChevronsUpDown,
  ChevronLeft,
  ChevronRight,
  MoreHorizontal,
  Eye,
  Globe,
  Coins,
  Zap,
  Pencil,
} from 'lucide-react';
import type { DifficultyLevel, InterviewSession } from '@/types/interview.types';

import { Suspense } from 'react';
import { PageLoader } from '@/components/shared/PageLoader';
import { useTranslations } from 'next-intl';

function CandidateInterviewContent() {
  const router = useRouter();
  const t = useTranslations("CandidateInterview");
  const searchParams = useSearchParams();
  const [isOpen, setIsOpen] = useState(false);
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('MEDIUM');
  const [language, setLanguage] = useState<'vi' | 'en'>('vi');
  const [selectedCv, setSelectedCv] = useState<string>('none');
  const [selectedJob, setSelectedJob] = useState<string>('none');
  const [selectedModel, setSelectedModel] = useState<string>('Gemini');
  const [isJdPopoverOpen, setIsJdPopoverOpen] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null);

  // Setup info modal states
  const [setupModalSession, setSetupModalSession] = useState<InterviewSession | null>(null);
  const [setupTab, setSetupTab] = useState<'cv' | 'jd'>('cv');
  const { data: setupJobDetailRes, isLoading: setupJobDetailLoading } = useJobDetail(
    setupModalSession?.jobId || '',
    !!setupModalSession?.jobId
  );
  const setupJobDetail = setupJobDetailRes?.data;

  const { data: sessionsRes, isLoading: sessionsLoading } = useGetInterviewSessions();
  const { data: cvsRes } = useGetMyCvs();
  const { data: jobsRes } = usePublicJobs({ page: 1, pageSize: 50 });
  const { data: jobDetailRes, isLoading: jobDetailLoading } = useJobDetail(
    selectedJob === 'none' ? '' : selectedJob,
    true
  );
  const startSessionMutation = useCreateInterviewSession();
  const deleteSessionMutation = useDeleteInterviewSession();
  const { data: walletRes } = useWalletBalance();
  const { data: coinConfigRes } = usePublicCoinConfig();

  const balance = walletRes?.data?.balance ?? 0;
  const activeSubName = walletRes?.data?.activeSubscriptionName;
  const mockInterviewCost = coinConfigRes?.data?.featureCosts?.mockInterview ?? 2000;
  
  const mockLimit = walletRes?.data?.mockInterviewLimit ?? 0;
  const mockUsed = walletRes?.data?.mockInterviewUsed ?? 0;
  const isSubUnlimited = mockLimit === -1;
  const subRemaining = isSubUnlimited ? -1 : Math.max(0, mockLimit - mockUsed);
  const hasActiveSub = !!activeSubName && (isSubUnlimited || subRemaining > 0);

  const sessions = sessionsRes?.data || [];
  const cvs = cvsRes?.data || [];
  const jobs = jobsRes?.data || [];
  const jobDetail = jobDetailRes?.data;

  const itemsPerPage = 6;
  const totalPages = Math.ceil(sessions.length / itemsPerPage);
  const maxPage = Math.max(1, totalPages);

  useEffect(() => {
    if (currentPage > maxPage) {
      setCurrentPage(maxPage);
    }
  }, [currentPage, maxPage]);

  useEffect(() => {
    const prefill = searchParams.get('prefillJobId');
    const openModal = searchParams.get('openModal');
    if (prefill) {
      setSelectedJob(prefill);
    }
    if (openModal === 'true') {
      setIsOpen(true);
    }
  }, [searchParams]);

  const startIndex = (currentPage - 1) * itemsPerPage;
  const paginatedSessions = sessions.slice(startIndex, startIndex + itemsPerPage);

  const getEmbedUrl = (url: string) => {
    if (!url) return '';
    const cleanUrl = url.split('?')[0].toLowerCase();
    const isDoc = cleanUrl.endsWith('.doc') || cleanUrl.endsWith('.docx');

    if (isDoc) {
      return `https://docs.google.com/gview?url=${encodeURIComponent(url)}&embedded=true`;
    }

    return url;
  };

  const handleStartInterview = async () => {
    if (!hasActiveSub && balance < mockInterviewCost) {
      toast.error(
        <div className="flex flex-col gap-1.5">
          <span className="font-semibold text-rose-600 dark:text-rose-400">{t('notEnoughCoinTitle')}</span>
          <span className="text-xs text-muted-foreground">
            {t('notEnoughCoinDesc', { balance: balance.toLocaleString(), cost: mockInterviewCost.toLocaleString() })}
          </span>
          <div className="flex items-center gap-2 mt-1">
            <button 
              onClick={() => { setIsOpen(false); router.push('/candidate/top-up'); }}
              className="px-3 py-1 bg-amber-500 hover:bg-amber-600 text-white font-medium text-xs rounded-lg shadow-sm transition"
            >
              {t('topUpNow')}
            </button>
            <button 
              onClick={() => { setIsOpen(false); router.push('/candidate/pricing'); }}
              className="px-3 py-1 bg-purple-600 hover:bg-purple-700 text-white font-medium text-xs rounded-lg shadow-sm transition"
            >
              {t('viewPricing')}
            </button>
          </div>
        </div>,
        { duration: 6000 }
      );
      return;
    }

    try {
      const res = await startSessionMutation.mutateAsync({
        difficultyLevel: difficulty,
        cvId: selectedCv === 'none' ? undefined : selectedCv,
        jobId: selectedJob === 'none' ? undefined : selectedJob,
        aiProvider: selectedModel,
        language: language,
      });

      if (res.success && res.data) {
        setIsOpen(false);
        toast.success(
          hasActiveSub 
            ? (isSubUnlimited ? t('startSuccessUnlimited', { subName: activeSubName }) : t('startSuccessSub', { subName: activeSubName, remaining: Math.max(0, subRemaining - 1) }))
            : t('startSuccessCoin', { cost: mockInterviewCost.toLocaleString() })
        );
        router.push(`/candidate/interview/${res.data.id}`);
      } else {
        toast.error(res.message || t('startError'));
      }
    } catch (err: any) {
      console.error(err);
      toast.error(err?.response?.data?.message || err.message || t('generalError'));
    }
  };

  const handleDeleteSession = (e: React.MouseEvent, sessionId: string) => {
    e.stopPropagation();
    setSessionToDelete(sessionId);
  };

  const handleConfirmDelete = async () => {
    if (!sessionToDelete) return;
    try {
      await deleteSessionMutation.mutateAsync(sessionToDelete);
      setSessionToDelete(null);
    } catch (err) {
      console.error(err);
    }
  };

  // Inline rename session states
  const [editingSessionId, setEditingSessionId] = useState<string | null>(null);
  const [editingTitleValue, setEditingTitleValue] = useState('');
  const renameSessionMutation = useRenameInterviewSession();

  const handleStartInlineEdit = (e: React.MouseEvent, session: InterviewSession) => {
    e.stopPropagation();
    setEditingSessionId(session.id);
    setEditingTitleValue(session.title || session.jobTitle || t('freeMockInterview'));
  };

  const handleInlineSaveRename = async (sessionId: string) => {
    if (!sessionId) return;
    try {
      const res = await renameSessionMutation.mutateAsync({
        sessionId,
        title: editingTitleValue,
      });
      if (res.success) {
        toast.success(t('renameSuccess'));
        setEditingSessionId(null);
      } else {
        toast.error(res.message || 'Error renaming session');
      }
    } catch (err: any) {
      toast.error(err?.message || 'Error renaming session');
    }
  };

  const handleOpenSetupModal = (e: React.MouseEvent, session: InterviewSession) => {
    e.stopPropagation();
    setSetupModalSession(session);
    setSetupTab('cv');
  };

  const renderCvPreview = () => {
    const activeCv = cvs.find(c => c.id === selectedCv);
    if (!activeCv) return (
      <div className="flex items-center justify-center h-full text-sm text-muted-foreground bg-card">
        {t('cvDataNotFound')}
      </div>
    );

    const formatSize = (bytes: number | null) => {
      if (!bytes) return '—';
      if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    };

    return (
      <div className="flex flex-col h-full bg-card overflow-hidden">
        {/* CV Summary Bar */}
        <div className="flex items-center justify-between border-b border-border bg-muted/20 px-4 py-2 shrink-0">
          <div className="flex flex-col min-w-0">
            <span className="text-xs font-semibold text-foreground truncate max-w-[200px]" title={activeCv.fileName}>
              {activeCv.fileName}
            </span>
            <span className="text-[10px] text-muted-foreground">
              {t('size', { size: formatSize(activeCv.fileSize) })}
            </span>
          </div>
          <a
            href={activeCv.fileUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 rounded-md border border-border bg-card px-2.5 py-1 text-[11px] font-semibold text-foreground hover:bg-muted transition-colors"
          >
            <ExternalLink className="h-3 w-3" />
            <span>{t('openInNewTab')}</span>
          </a>
        </div>

        {/* CV PDF viewer */}
        <div className="flex-1 bg-muted/30">
          <iframe
            src={getEmbedUrl(activeCv.fileUrl)}
            className="w-full h-full border-0"
            title={activeCv.fileName}
          />
        </div>
      </div>
    );
  };

  const renderJdPreview = () => {
    if (jobDetailLoading) {
      return (
        <div className="p-6 space-y-4 h-full overflow-y-auto">
          <Skeleton className="h-6 w-2/3" />
          <Skeleton className="h-4 w-1/3" />
          <div className="space-y-2 pt-4">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
            <Skeleton className="h-4 w-4/5" />
          </div>
        </div>
      );
    }

    if (!jobDetail) {
      return (
        <div className="flex items-center justify-center h-full text-sm text-muted-foreground">
          {t('jdNotFound')}
        </div>
      );
    }

    const formatSalary = (min?: number, max?: number, curr: string = 'VND') => {
      if (min && max) return `${min.toLocaleString()} - ${max.toLocaleString()} ${curr}`;
      if (min) return `From ${min.toLocaleString()} ${curr}`;
      if (max) return `Up to ${max.toLocaleString()} ${curr}`;
      return t('negotiable');
    };

    return (
      <div className="h-full flex flex-col bg-card overflow-hidden">
        {/* JD Main Header */}
        <div className="p-4 border-b border-border bg-muted/5 shrink-0">
          <div className="flex items-start gap-3">
            {jobDetail.logoUrl ? (
              <img
                src={jobDetail.logoUrl}
                alt={jobDetail.companyName}
                className="w-10 h-10 rounded-lg object-cover border border-border bg-white"
              />
            ) : (
              <div className="w-10 h-10 rounded-lg border border-border bg-muted flex items-center justify-center text-muted-foreground font-semibold text-sm">
                {jobDetail.companyName?.substring(0, 2).toUpperCase()}
              </div>
            )}
            <div className="min-w-0">
              <h4 className="text-sm font-bold text-foreground truncate">{jobDetail.title}</h4>
              <p className="text-xs text-muted-foreground truncate">{jobDetail.companyName}</p>
            </div>
          </div>

          {/* Info Grid */}
          <div className="grid grid-cols-2 gap-2 mt-3 text-[11px] text-muted-foreground">
            <div className="flex items-center gap-1.5">
              <DollarSign className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
              <span className="truncate font-medium text-foreground">
                {formatSalary(jobDetail.minSalary, jobDetail.maxSalary, jobDetail.currency)}
              </span>
            </div>
            <div className="flex items-center gap-1.5">
              <MapPin className="h-3.5 w-3.5 text-primary shrink-0" />
              <span className="truncate">{jobDetail.location}</span>
            </div>
            {jobDetail.level && (
              <div className="flex items-center gap-1.5">
                <Layers className="h-3.5 w-3.5 text-indigo-500 shrink-0" />
                <span className="truncate">{jobDetail.level}</span>
              </div>
            )}
            {jobDetail.workingModel && (
              <div className="flex items-center gap-1.5">
                <Briefcase className="h-3.5 w-3.5 text-cyan-500 shrink-0" />
                <span className="truncate">{jobDetail.workingModel}</span>
              </div>
            )}
          </div>
        </div>

        {/* JD Content Body */}
        <div className="flex-1 overflow-y-auto p-4 space-y-4 text-xs leading-relaxed">
          {jobDetail.skills && jobDetail.skills.length > 0 && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('reqSkills')}</h5>
              <div className="flex flex-wrap gap-1">
                {jobDetail.skills.map((s) => (
                  <Badge key={s} variant="secondary" className="text-[10px] py-0 px-1.5 bg-muted font-normal text-muted-foreground">
                    {s}
                  </Badge>
                ))}
              </div>
            </div>
          )}

          {jobDetail.requirements && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('jobReqs')}</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {jobDetail.requirements}
              </div>
            </div>
          )}

          {jobDetail.description && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('jobDesc')}</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {jobDetail.description}
              </div>
            </div>
          )}

          {jobDetail.benefits && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('benefits')}</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {jobDetail.benefits}
              </div>
            </div>
          )}
        </div>
      </div>
    );
  };

  const renderSetupCvPreview = (cvId: string) => {
    const activeCv = cvs.find((c) => c.id === cvId);
    if (!activeCv) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-sm text-muted-foreground bg-card border border-border rounded-xl p-6">
          <FileText className="w-8 h-8 mb-2 opacity-40" />
          <p>{t('cvDataNotFound')}</p>
        </div>
      );
    }

    const formatSize = (bytes: number | null) => {
      if (!bytes) return '—';
      if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    };

    return (
      <div className="flex flex-col h-full bg-card border border-border rounded-xl overflow-hidden">
        <div className="flex items-center justify-between border-b border-border bg-muted/20 px-4 py-2 shrink-0">
          <div className="flex flex-col min-w-0">
            <span className="text-xs font-semibold text-foreground truncate max-w-[300px]" title={activeCv.fileName}>
              {activeCv.fileName}
            </span>
            <span className="text-[10px] text-muted-foreground">
              {t('size', { size: formatSize(activeCv.fileSize) })}
            </span>
          </div>
          <a
            href={activeCv.fileUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 rounded-md border border-border bg-card px-2.5 py-1 text-[11px] font-semibold text-foreground hover:bg-muted transition-colors"
          >
            <ExternalLink className="h-3 w-3" />
            <span>{t('openInNewTab')}</span>
          </a>
        </div>
        <div className="flex-1 bg-muted/30">
          <iframe
            src={getEmbedUrl(activeCv.fileUrl)}
            className="w-full h-full border-0"
            title={activeCv.fileName}
          />
        </div>
      </div>
    );
  };

  const renderSetupJdPreview = () => {
    if (setupJobDetailLoading) {
      return (
        <div className="p-6 space-y-4 h-full overflow-y-auto border border-border rounded-xl bg-card">
          <Skeleton className="h-6 w-2/3" />
          <Skeleton className="h-4 w-1/3" />
          <div className="space-y-2 pt-4">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
          </div>
        </div>
      );
    }

    if (!setupJobDetail) {
      return (
        <div className="flex items-center justify-center h-full text-sm text-muted-foreground border border-border rounded-xl bg-card">
          {t('jdNotFound')}
        </div>
      );
    }

    const formatSalary = (min?: number, max?: number, curr: string = 'VND') => {
      if (min && max) return `${min.toLocaleString()} - ${max.toLocaleString()} ${curr}`;
      if (min) return `From ${min.toLocaleString()} ${curr}`;
      if (max) return `Up to ${max.toLocaleString()} ${curr}`;
      return t('negotiable');
    };

    return (
      <div className="h-full flex flex-col bg-card border border-border rounded-xl overflow-hidden">
        <div className="p-4 border-b border-border bg-muted/5 shrink-0">
          <div className="flex items-start gap-3">
            {setupJobDetail.logoUrl ? (
              <img
                src={setupJobDetail.logoUrl}
                alt={setupJobDetail.companyName}
                className="w-10 h-10 rounded-lg object-cover border border-border bg-white"
              />
            ) : (
              <div className="w-10 h-10 rounded-lg border border-border bg-muted flex items-center justify-center text-muted-foreground font-semibold text-sm">
                {setupJobDetail.companyName?.substring(0, 2).toUpperCase()}
              </div>
            )}
            <div className="min-w-0">
              <h4 className="text-sm font-bold text-foreground truncate">{setupJobDetail.title}</h4>
              <p className="text-xs text-muted-foreground truncate">{setupJobDetail.companyName}</p>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-2 mt-3 text-[11px] text-muted-foreground">
            <div className="flex items-center gap-1.5">
              <DollarSign className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
              <span className="truncate font-medium text-foreground">
                {formatSalary(setupJobDetail.minSalary, setupJobDetail.maxSalary, setupJobDetail.currency)}
              </span>
            </div>
            <div className="flex items-center gap-1.5">
              <MapPin className="h-3.5 w-3.5 text-primary shrink-0" />
              <span className="truncate">{setupJobDetail.location}</span>
            </div>
            {setupJobDetail.level && (
              <div className="flex items-center gap-1.5">
                <Layers className="h-3.5 w-3.5 text-indigo-500 shrink-0" />
                <span className="truncate">{setupJobDetail.level}</span>
              </div>
            )}
            {setupJobDetail.workingModel && (
              <div className="flex items-center gap-1.5">
                <Briefcase className="h-3.5 w-3.5 text-cyan-500 shrink-0" />
                <span className="truncate">{setupJobDetail.workingModel}</span>
              </div>
            )}
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-4 space-y-4 text-xs leading-relaxed">
          {setupJobDetail.skills && setupJobDetail.skills.length > 0 && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('reqSkills')}</h5>
              <div className="flex flex-wrap gap-1">
                {setupJobDetail.skills.map((s) => (
                  <Badge key={s} variant="secondary" className="text-[10px] py-0 px-1.5 bg-muted font-normal text-muted-foreground">
                    {s}
                  </Badge>
                ))}
              </div>
            </div>
          )}

          {setupJobDetail.requirements && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('jobReqs')}</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {setupJobDetail.requirements}
              </div>
            </div>
          )}

          {setupJobDetail.description && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('jobDesc')}</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {setupJobDetail.description}
              </div>
            </div>
          )}

          {setupJobDetail.benefits && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">{t('benefits')}</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {setupJobDetail.benefits}
              </div>
            </div>
          )}
        </div>
      </div>
    );
  };

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('title')}</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            {t('desc')}
          </p>
        </div>
        {sessions.length > 0 && (
          <Button onClick={() => setIsOpen(true)} className="bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
            <Plus className="mr-1 h-4 w-4" />
            {t('startInterview')}
            <Sparkles className="mr-2 h-4 w-4 ml-1" />
          </Button>
        )}
      </div>

      <div className="flex flex-col gap-4">

        {sessionsLoading ? (
          <div className="flex flex-col gap-3">
            {[1, 2, 3].map((n) => <CardSkeleton key={n} />)}
          </div>
        ) : sessions.length === 0 ? (
          <EmptyState
            title={t('noSessionsTitle')}
            description={t('noSessionsDesc')}
            imageUrl="/images/emptyInterview.png"
          >
            <Button onClick={() => setIsOpen(true)} className="mt-4 bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
              <Plus className="mr-1 h-4 w-4" />
              {t('startInterview')}
              <Sparkles className="mr-2 h-4 w-4 ml-1" />
            </Button>
          </EmptyState>
        ) : (
          <>
            <div className="flex flex-col gap-4">
              {paginatedSessions.map((session) => (
                <Card
                  key={session.id}
                  onClick={() => router.push(`/candidate/interview/${session.id}`)}
                  className="group cursor-pointer hover:border-primary/50 transition-colors"
                >
                  <CardContent className="flex flex-col gap-3">
                    <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                      <div className="flex items-center gap-3 flex-1 min-w-0">
                        {/* Left: Status Icon */}
                        <Image src="/images/mascotAvatar.png" alt="Mascot" width={44} height={44} className={`w-11 h-11 rounded-lg shrink-0 object-cover border bg-white dark:bg-slate-900 ${session.status === 'IN_PROGRESS'
                            ? 'border-blue-200 dark:border-blue-800 ring-1 ring-blue-500/20'
                            : 'border-emerald-200 dark:border-emerald-800 ring-1 ring-emerald-500/20'
                          }`} />

                        {/* Center: Info */}
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 min-w-0">
                            {editingSessionId === session.id ? (
                              <div className="flex items-center gap-1.5 flex-1 min-w-0" onClick={(e) => e.stopPropagation()}>
                                <Input
                                  value={editingTitleValue}
                                  onChange={(e) => setEditingTitleValue(e.target.value)}
                                  placeholder={session.jobTitle || t('freeMockInterview')}
                                  className="h-9 text-sm font-medium border-primary focus-visible:ring-1 focus-visible:ring-primary py-1 px-3 bg-card shadow-sm rounded-lg"
                                  autoFocus
                                  onKeyDown={(e) => {
                                    if (e.key === 'Enter') handleInlineSaveRename(session.id);
                                    if (e.key === 'Escape') setEditingSessionId(null);
                                  }}
                                />
                                <button
                                  type="button"
                                  onClick={() => handleInlineSaveRename(session.id)}
                                  disabled={renameSessionMutation.isPending}
                                  className="p-1.5 bg-emerald-500/10 text-emerald-600 hover:bg-emerald-500/20 rounded-lg shrink-0 transition-colors"
                                  title={t('save')}
                                >
                                  <Check className="w-4 h-4" />
                                </button>
                                <button
                                  type="button"
                                  onClick={() => setEditingSessionId(null)}
                                  className="p-1.5 bg-muted text-muted-foreground hover:bg-muted/80 rounded-lg shrink-0 transition-colors"
                                  title={t('cancel')}
                                >
                                  <X className="w-4 h-4" />
                                </button>
                              </div>
                            ) : (
                              <>
                                <span className="font-medium text-base text-foreground group-hover:text-primary transition-colors line-clamp-1 leading-snug">
                                  {session.title || session.jobTitle || t('freeMockInterview')}
                                </span>
                                <button
                                  type="button"
                                  onClick={(e) => handleStartInlineEdit(e, session)}
                                  className="p-1 text-muted-foreground hover:text-primary transition-colors rounded-md hover:bg-muted shrink-0"
                                  title={t('renameSession')}
                                >
                                  <Pencil className="w-3.5 h-3.5" />
                                </button>
                              </>
                            )}
                          </div>
                          <div className="flex items-center gap-4 flex-wrap mt-1 text-sm text-slate-600">
                            <div className="flex items-center">
                              <Badge
                                className={`shrink-0 text-xs px-2 py-0.5 border-none font-medium ${session.status === 'IN_PROGRESS'
                                    ? 'bg-blue-500/10 text-blue-700'
                                    : 'bg-emerald-500/10 text-emerald-700'
                                  }`}
                              >
                                {session.status === 'IN_PROGRESS' ? t('inProgress') : t('completed')}
                              </Badge>
                            </div>
                            <span className="flex items-center gap-1.5">
                              <Calendar className="h-4 w-4 shrink-0 text-slate-400" />
                              {session.startedAt ? new Date(session.startedAt).toLocaleDateString('vi-VN') : 'N/A'}
                            </span>
                            {session.cvFileName && (
                              <span className="flex items-center gap-1.5 truncate max-w-[180px]">
                                <FileText className="h-4 w-4 shrink-0 text-slate-400" />
                                {session.cvFileName}
                              </span>
                            )}
                          </div>
                        </div>
                      </div>

                      {/* Action Zone (Right side) */}
                      <div className="flex items-center gap-2 shrink-0">
                        <Button
                          size="sm"
                          variant="outline"
                          className="gap-1.5 h-9 bg-blue-500/5 text-blue-600 dark:text-blue-400 border-blue-200 dark:border-blue-800 hover:bg-blue-500/10 transition-colors"
                          onClick={(e) => handleOpenSetupModal(e, session)}
                        >
                          <Info className="w-4 h-4 text-blue-500" />
                          <span>{t('setupInfo')}</span>
                        </Button>

                        <Button size="sm" variant="outline" className="gap-1.5 h-9" onClick={(e) => { e.stopPropagation(); router.push(`/candidate/interview/${session.id}`); }}>
                          {session.status === 'IN_PROGRESS' ? (
                            <><Play className="w-4 h-4 fill-current" /> {t('resume')}</>
                          ) : (
                            <><Eye className="w-4 h-4" /> {t('review')}</>
                          )}
                        </Button>

                        <Popover>
                          <PopoverTrigger className="inline-flex items-center justify-center h-9 w-9 text-slate-500 hover:text-foreground shrink-0 border border-transparent hover:border-border hover:bg-muted/50 rounded-lg transition-colors focus-visible:outline-hidden focus-visible:ring-1 focus-visible:ring-ring" onClick={(e) => e.stopPropagation()}>
                            <MoreHorizontal className="h-4 w-4" />
                          </PopoverTrigger>
                          <PopoverContent align="end" className="w-48 p-1">
                            <div className="flex flex-col">
                              <Button
                                variant="ghost"
                                className="w-full justify-start gap-2 h-9 text-foreground hover:bg-muted"
                                onClick={(e) => handleStartInlineEdit(e, session)}
                              >
                                <Pencil className="h-4 w-4" />
                                <span>{t('renameSession')}</span>
                              </Button>
                              <Button
                                variant="ghost"
                                className="w-full justify-start gap-2 h-9 text-rose-600 hover:text-rose-700 hover:bg-rose-50"
                                onClick={(e) => handleDeleteSession(e, session.id)}
                              >
                                <Trash2 className="h-4 w-4" />
                                <span>{t('deleteSession')}</span>
                              </Button>
                            </div>
                          </PopoverContent>
                        </Popover>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>

            {/* Pagination Controls */}
            <ListPagination page={currentPage} totalPages={totalPages} setPage={setCurrentPage} />
          </>
        )}
      </div>

      {/* Startup Session Modal */}
      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="max-w-[95vw] md:max-w-5xl h-[85vh] md:h-[75vh] min-h-[500px] overflow-hidden bg-card border border-border text-foreground rounded-2xl p-0 flex flex-col md:flex-row shadow-2xl">
          {/* Left Column: Form Configuration */}
          <div className="w-full md:w-1/2 flex flex-col justify-between border-b md:border-b-0 md:border-r border-border h-full overflow-y-auto p-6">
            <div className="space-y-6">
              <DialogHeader>
                <DialogTitle className="text-xl font-bold flex items-center gap-2 text-foreground">
                  <BrainCircuit className="h-5 w-5 text-primary" /> {t('setupTitle')}
                </DialogTitle>
                <DialogDescription className="text-muted-foreground text-sm">
                  {t('setupDesc')}
                </DialogDescription>
              </DialogHeader>

              {/* Feature Cost & Wallet Balance Banner */}
              <div className="p-4 rounded-xl bg-gradient-to-r from-purple-500/10 via-amber-500/10 to-transparent border border-purple-500/20 shadow-sm flex items-center justify-between gap-4">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-500/20 text-amber-500 shadow-inner">
                    {hasActiveSub ? <Zap className="h-5 w-5 text-purple-600 dark:text-purple-400 fill-purple-600/20" /> : <Coins className="h-5 w-5 text-amber-500 fill-amber-500/20" />}
                  </div>
                  <div className="flex flex-col">
                    <div className="flex items-center gap-1.5 flex-wrap">
                      <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground">{t('serviceFee')}</span>
                      {hasActiveSub ? (
                        <>
                          <Badge className="bg-purple-600 text-white text-[10px] font-bold px-2 py-0.5 shadow-sm">
                            {t('freeSub', { subName: activeSubName })}
                          </Badge>
                          <span className="text-xs font-semibold text-purple-600 dark:text-purple-400">
                            {isSubUnlimited ? t('unlimitedMatches') : t('remainingMatches', { remaining: subRemaining, limit: mockLimit })}
                          </span>
                        </>
                      ) : (
                        <span className="text-sm font-black text-amber-600 dark:text-amber-400">
                          {t('coinPerMatch', { coin: mockInterviewCost.toLocaleString() })}
                        </span>
                      )}
                    </div>
                    {!!activeSubName && !hasActiveSub && (
                      <span className="text-xs text-rose-500 mt-0.5 font-medium">
                        {t('subExpired', { subName: activeSubName })}
                      </span>
                    )}
                    {!hasActiveSub && (
                      <span className="text-xs text-muted-foreground mt-0.5 font-medium">
                        {t('currentBalance')} <strong className={balance < mockInterviewCost ? "text-rose-500 font-bold" : "text-emerald-600 font-bold"}>{balance.toLocaleString()} Coin</strong>
                      </span>
                    )}
                  </div>
                </div>

                {!hasActiveSub && balance < mockInterviewCost && (
                  <Button
                    type="button"
                    size="sm"
                    onClick={() => {
                      setIsOpen(false);
                      router.push('/candidate/top-up');
                    }}
                    className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-3 py-1.5 text-xs rounded-lg shadow-md hover:shadow-amber-500/25 transition-all shrink-0"
                  >
                    {t('topUpCoin')}
                  </Button>
                )}
              </div>

              <div className="space-y-5 py-2">
                {/* Choose CV */}
                <div className="space-y-2">
                  <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                    <FileText className="h-4 w-4 text-primary" /> {t('useCv')}
                  </label>
                  <Select value={selectedCv} onValueChange={(val) => setSelectedCv(val ?? 'none')}>
                    <SelectTrigger className="w-full h-11 px-3 bg-card border-border hover:border-primary/50 focus:ring-primary/20 hover:bg-muted/10 transition-all rounded-xl shadow-sm text-sm font-medium">
                      <SelectValue placeholder={t('selectCv')}>
                        {selectedCv === 'none'
                          ? (
                            <div className="flex items-center gap-2 text-muted-foreground font-medium">
                              <X className="h-4 w-4 shrink-0" />
                              <span>{t('noCv')}</span>
                            </div>
                          )
                          : (
                            <div className="flex items-center gap-2">
                              <FileText className="h-4 w-4 text-primary shrink-0" />
                              <span className="truncate max-w-[240px]">
                                {cvs.find((c) => c.id === selectedCv)?.fileName || 'Selected CV'}
                              </span>
                            </div>
                          )}
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground max-h-60 overflow-y-auto">
                      <SelectItem value="none">
                        <div className="flex items-center gap-2 text-muted-foreground font-medium">
                          <X className="h-4 w-4 shrink-0" />
                          <span>{t('noCv')}</span>
                        </div>
                      </SelectItem>
                      {cvs.map((cv) => (
                        <SelectItem key={cv.id} value={cv.id}>
                          <div className="flex items-center gap-2">
                            <FileText className="h-4 w-4 text-primary shrink-0" />
                            <span className="truncate max-w-[240px]">{cv.fileName}</span>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {/* Choose Job Description (Searchable Popover + Command) */}
                <div className="space-y-2">
                  <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                    <Briefcase className="h-4 w-4 text-indigo-500" /> {t('useJd')}
                  </label>
                  <Popover open={isJdPopoverOpen} onOpenChange={setIsJdPopoverOpen}>
                    <PopoverTrigger render={
                      <Button
                        variant="outline"
                        role="combobox"
                        aria-expanded={isJdPopoverOpen}
                        className="w-full h-11 px-3 justify-between bg-card border-border hover:border-primary/50 hover:bg-muted/10 transition-all rounded-xl shadow-sm text-sm font-medium"
                      >
                        <span className="truncate flex items-center gap-2">
                          <Briefcase className="h-4 w-4 text-indigo-500 shrink-0" />
                          {selectedJob === 'none' ? (
                            <span className="text-muted-foreground font-normal">{t('noJd')}</span>
                          ) : (
                            <span className="font-semibold text-foreground truncate max-w-[220px]">
                              {jobs.find((job) => job.id === selectedJob)?.title || jobDetail?.title || selectedJob}
                            </span>
                          )}
                        </span>
                        <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                      </Button>
                    } />
                    <PopoverContent className="w-(--anchor-width) p-0 gap-0" align="start">
                      <Command className="bg-popover text-popover-foreground">
                        <CommandInput placeholder={t('searchJd')} className="h-9" />
                        <CommandList>
                          <CommandEmpty>{t('noJdFound')}</CommandEmpty>
                          <CommandGroup>
                            <ScrollArea className="h-48">
                              <CommandItem
                                value="none"
                                onSelect={() => {
                                  setSelectedJob('none');
                                  setIsJdPopoverOpen(false);
                                }}
                                className="flex items-center justify-between py-2 px-3 text-sm cursor-default"
                              >
                                <div className="flex items-center gap-2 text-muted-foreground font-medium">
                                  <X className="h-4 w-4 shrink-0" />
                                  <span>{t('noJd')}</span>
                                </div>
                                {selectedJob === 'none' && <Check className="h-4 w-4 text-primary shrink-0" />}
                              </CommandItem>
                              {jobs.map((job) => (
                                <CommandItem
                                  key={job.id}
                                  value={job.title}
                                  onSelect={() => {
                                    setSelectedJob(job.id);
                                    setIsJdPopoverOpen(false);
                                  }}
                                  className="flex items-center justify-between py-2 px-3 text-sm cursor-default"
                                >
                                  <div className="flex items-center gap-2 min-w-0">
                                    <Briefcase className="h-4 w-4 text-indigo-500 shrink-0" />
                                    <span className="truncate font-semibold max-w-[180px]">{job.title}</span>
                                    {job.companyName && (
                                      <span className="text-[10px] text-muted-foreground font-normal truncate max-w-[80px]">
                                        · {job.companyName}
                                      </span>
                                    )}
                                  </div>
                                  {selectedJob === job.id && <Check className="h-4 w-4 text-primary shrink-0" />}
                                </CommandItem>
                              ))}
                            </ScrollArea>
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </PopoverContent>
                  </Popover>
                </div>

                {/* Difficulty Level */}
                {selectedJob === 'none' && (
                  <div className="space-y-2">
                    <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                      <Layers className="h-4 w-4 text-indigo-500" /> {t('selectLevel')}
                    </label>
                    <Select value={difficulty} onValueChange={(val) => setDifficulty(val as DifficultyLevel)}>
                      <SelectTrigger className="w-full h-11 px-3 bg-card border-border hover:border-primary/50 focus:ring-primary/20 hover:bg-muted/10 transition-all rounded-xl shadow-sm text-sm font-medium">
                        <SelectValue placeholder={t('selectLevel')} />
                      </SelectTrigger>
                      <SelectContent alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground">
                        <SelectItem value="EASY">{t('intern')}</SelectItem>
                        <SelectItem value="MEDIUM">{t('middle')}</SelectItem>
                        <SelectItem value="HARD">{t('senior')}</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                )}

                {/* Interview Language */}
                <div className="space-y-2">
                  <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                    <Globe className="h-4 w-4 text-emerald-500" /> {t('interviewLang')}
                  </label>
                  <Select value={language} onValueChange={(val) => setLanguage((val ?? 'vi') as 'vi' | 'en')}>
                    <SelectTrigger className="w-full h-11 px-3 bg-card border-border hover:border-primary/50 focus:ring-primary/20 hover:bg-muted/10 transition-all rounded-xl shadow-sm text-sm font-medium">
                      <SelectValue placeholder={t('selectLang')} />
                    </SelectTrigger>
                    <SelectContent alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground">
                      <SelectItem value="vi">
                        <div className="flex items-center gap-2 font-medium">
                          <span>{t('vi')}</span>
                        </div>
                      </SelectItem>
                      <SelectItem value="en">
                        <div className="flex items-center gap-2 font-medium">
                          <span>{t('en')}</span>
                        </div>
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            <DialogFooter className="mt-8 flex flex-col sm:flex-row gap-3 pt-4 border-t border-border/60 shrink-0">
              <Button
                variant="outline"
                onClick={() => setIsOpen(false)}
                className="w-full h-10 border-border text-muted-foreground hover:bg-muted font-medium"
              >
                {t('cancel')}
              </Button>
              <Button
                onClick={handleStartInterview}
                disabled={startSessionMutation.isPending || (!hasActiveSub && balance < mockInterviewCost)}
                className={`w-full h-10 font-semibold flex items-center justify-center gap-2 transition-colors ${
                  !hasActiveSub && balance < mockInterviewCost
                    ? "bg-rose-500 hover:bg-rose-600 text-white opacity-90 cursor-not-allowed"
                    : "bg-primary hover:bg-primary/90 text-primary-foreground"
                }`}
              >
                {startSessionMutation.isPending ? (
                  <>{t('initializing')}</>
                ) : !hasActiveSub && balance < mockInterviewCost ? (
                  <>{t('notEnoughCoinBtn', { balance: balance.toLocaleString(), cost: mockInterviewCost.toLocaleString() })}</>
                ) : (
                  <>
                    <Play className="h-4 w-4 fill-current" /> {hasActiveSub ? t('startNowFree') : t('startNowCoin', { coin: mockInterviewCost.toLocaleString() })}
                  </>
                )}
              </Button>
            </DialogFooter>
          </div>

          {/* Right Column: Preview Panel */}
          <div className="hidden md:flex w-1/2 flex-col h-full bg-muted/5 overflow-hidden">
            {selectedCv === 'none' && selectedJob === 'none' ? (
              <div className="flex-1 flex flex-col items-center justify-center p-8 text-center space-y-4">
                <div className="bg-primary/10 p-4 rounded-full text-primary">
                  <BrainCircuit className="h-10 w-10 animate-pulse" />
                </div>
                <h3 className="text-lg font-bold text-foreground">{t('readyForInterview')}</h3>
                <p className="text-xs text-muted-foreground max-w-xs leading-relaxed">
                  {t('readyDesc')}
                </p>
                <div className="bg-card border border-border rounded-xl p-4 text-left text-[11px] text-muted-foreground w-full max-w-xs space-y-2">
                  <p className="font-semibold text-foreground flex items-center gap-1.5">
                    <Info className="h-3.5 w-3.5 text-primary" /> {t('prepTips')}
                  </p>
                  <ul className="list-disc pl-4 space-y-1">
                    <li>{t('prepTip1')}</li>
                    <li>{t('prepTip2')}</li>
                  </ul>
                </div>
              </div>
            ) : (
              <>
                {selectedCv !== 'none' && selectedJob !== 'none' ? (
                  <Tabs defaultValue="cv" className="flex-1 flex flex-col h-full overflow-hidden">
                    <div className="border-b border-border bg-card px-4 py-2 flex items-center justify-between shrink-0 pr-10">
                      <TabsList className="bg-muted">
                        <TabsTrigger value="cv" className="text-xs font-semibold py-1.5 px-3">
                          <FileText className="h-3.5 w-3.5 mr-1.5" /> {t('viewCv')}
                        </TabsTrigger>
                        <TabsTrigger value="jd" className="text-xs font-semibold py-1.5 px-3">
                          <Briefcase className="h-3.5 w-3.5 mr-1.5" /> {t('jdDetails')}
                        </TabsTrigger>
                      </TabsList>
                      <div className="text-[10px] text-muted-foreground font-medium hidden lg:block">
                        {t('cvJdCombinedMode')}
                      </div>
                    </div>
                    <TabsContent value="cv" className="flex-1 h-full min-h-0 overflow-hidden m-0 data-[state=inactive]:hidden flex flex-col">
                      {renderCvPreview()}
                    </TabsContent>
                    <TabsContent value="jd" className="flex-1 h-full min-h-0 overflow-hidden m-0 data-[state=inactive]:hidden flex flex-col">
                      {renderJdPreview()}
                    </TabsContent>
                  </Tabs>
                ) : selectedCv !== 'none' ? (
                  <div className="flex-1 flex flex-col h-full overflow-hidden">
                    <div className="border-b border-border bg-card px-4 py-3 shrink-0 flex justify-between items-center pr-10">
                      <h3 className="text-sm font-bold text-foreground flex items-center gap-1.5">
                        <FileText className="h-4 w-4 text-primary" /> {t('previewSelectedCv')}
                      </h3>
                      <span className="text-[10px] bg-primary/10 text-primary px-2 py-0.5 rounded-full font-semibold">
                        {t('cvOnly')}
                      </span>
                    </div>
                    <div className="flex-1 min-h-0 overflow-hidden">
                      {renderCvPreview()}
                    </div>
                  </div>
                ) : (
                  <div className="flex-1 flex flex-col h-full overflow-hidden">
                    <div className="border-b border-border bg-card px-4 py-3 shrink-0 flex justify-between items-center pr-10">
                      <h3 className="text-sm font-bold text-foreground flex items-center gap-1.5">
                        <Briefcase className="h-4 w-4 text-indigo-500" /> {t('jdDetails')}
                      </h3>
                      <span className="text-[10px] bg-indigo-500/10 text-indigo-600 px-2 py-0.5 rounded-full font-semibold">
                        {t('jdOnly')}
                      </span>
                    </div>
                    <div className="flex-1 min-h-0 overflow-hidden">
                      {renderJdPreview()}
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!sessionToDelete} onOpenChange={(open) => !open && setSessionToDelete(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('deleteHistoryTitle')}</DialogTitle>
            <DialogDescription>
              {t('deleteHistoryConfirm')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 flex flex-col sm:flex-row gap-2 justify-end">
            <Button variant="outline" onClick={() => setSessionToDelete(null)} disabled={deleteSessionMutation.isPending}>
              {t('cancel')}
            </Button>
            <Button variant="destructive" onClick={handleConfirmDelete} disabled={deleteSessionMutation.isPending}>
              {deleteSessionMutation.isPending ? t('deleting') : t('delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Setup Info Modal */}
      <Dialog open={!!setupModalSession} onOpenChange={(open) => !open && setSetupModalSession(null)}>
        <DialogContent className="max-w-[95vw] md:max-w-6xl w-full h-[90vh] md:h-[85vh] min-h-[600px] overflow-hidden bg-card border border-border rounded-2xl p-0 flex flex-col shadow-2xl">
          {/* Header */}
          <div className="p-5 border-b border-border bg-muted/10 shrink-0 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="p-2.5 rounded-xl bg-blue-500/10 text-blue-600 dark:text-blue-400">
                <Info className="w-5 h-5" />
              </div>
              <div>
                <DialogTitle className="text-lg font-bold text-foreground">
                  {t('setupInfoTitle')}
                </DialogTitle>
                <DialogDescription className="text-xs text-muted-foreground">
                  {setupModalSession?.title || setupModalSession?.jobTitle || t('freeMockInterview')}
                </DialogDescription>
              </div>
            </div>
          </div>

          {/* Setup Meta Badges Bar */}
          <div className="px-5 py-3 border-b border-border bg-muted/20 flex flex-wrap items-center gap-4 text-xs shrink-0">
            <div className="flex items-center gap-1.5">
              <span className="font-semibold text-muted-foreground">{t('diffLevel')}:</span>
              <Badge variant="outline" className="bg-background font-medium">
                {setupModalSession?.difficultyLevel === 'EASY' ? 'Intern / Fresher' : setupModalSession?.difficultyLevel === 'HARD' ? 'Senior' : 'Middle'}
              </Badge>
            </div>

            <div className="flex items-center gap-1.5">
              <span className="font-semibold text-muted-foreground">{t('interviewLang')}:</span>
              <Badge variant="outline" className="bg-background font-medium">
                {setupModalSession?.language === 'en' ? t('en') : t('vi')}
              </Badge>
            </div>

            <div className="flex items-center gap-1.5 ml-auto text-muted-foreground">
              <Calendar className="h-3.5 w-3.5" />
              <span>{setupModalSession?.startedAt ? new Date(setupModalSession.startedAt).toLocaleDateString('vi-VN') : 'N/A'}</span>
            </div>
          </div>

          {/* Content Tabs (CV & JD) */}
          <div className="flex-1 overflow-hidden p-4">
            <Tabs value={setupTab} onValueChange={(val) => setSetupTab(val as 'cv' | 'jd')} className="h-full flex flex-col">
              <TabsList className="grid grid-cols-2 w-full max-w-xs shrink-0 mb-3">
                <TabsTrigger value="cv" className="text-xs gap-1.5">
                  <FileText className="w-3.5 h-3.5" />
                  <span>{t('cvTab')}</span>
                </TabsTrigger>
                <TabsTrigger value="jd" className="text-xs gap-1.5">
                  <Briefcase className="w-3.5 h-3.5" />
                  <span>{t('jdTab')}</span>
                </TabsTrigger>
              </TabsList>

              <TabsContent value="cv" className="flex-1 min-h-0 h-full m-0">
                {setupModalSession?.cvId ? (
                  renderSetupCvPreview(setupModalSession.cvId)
                ) : (
                  <div className="flex flex-col items-center justify-center h-full border border-dashed border-border rounded-xl p-8 text-center text-muted-foreground bg-muted/10">
                    <FileText className="w-10 h-10 mb-2 opacity-40 text-muted-foreground" />
                    <p className="text-sm font-medium">{t('noCvAttached')}</p>
                  </div>
                )}
              </TabsContent>

              <TabsContent value="jd" className="flex-1 min-h-0 h-full m-0">
                {setupModalSession?.jobId ? (
                  renderSetupJdPreview()
                ) : (
                  <div className="flex flex-col items-center justify-center h-full border border-dashed border-border rounded-xl p-8 text-center text-muted-foreground bg-muted/10">
                    <Briefcase className="w-10 h-10 mb-2 opacity-40 text-muted-foreground" />
                    <p className="text-sm font-medium">{t('noJdAttached')}</p>
                  </div>
                )}
              </TabsContent>
            </Tabs>
          </div>

          {/* Footer */}
          <div className="p-4 border-t border-border bg-muted/10 shrink-0 flex items-center justify-between">
            <Button variant="outline" size="sm" onClick={() => setSetupModalSession(null)}>
              {t('close')}
            </Button>
            {setupModalSession && (
              <Button
                size="sm"
                className="bg-primary text-primary-foreground gap-1.5"
                onClick={() => {
                  setSetupModalSession(null);
                  router.push(`/candidate/interview/${setupModalSession.id}`);
                }}
              >
                {setupModalSession.status === 'IN_PROGRESS' ? (
                  <><Play className="w-4 h-4 fill-current" /> {t('resume')}</>
                ) : (
                  <><Eye className="w-4 h-4" /> {t('review')}</>
                )}
              </Button>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

export default function CandidateInterviewPage() {
  return (
    <Suspense fallback={<PageLoader />}>
      <CandidateInterviewContent />
    </Suspense>
  );
}
