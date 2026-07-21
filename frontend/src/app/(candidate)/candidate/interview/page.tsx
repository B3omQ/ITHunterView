'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import {
  useGetInterviewSessions,
  useCreateInterviewSession,
  useDeleteInterviewSession,
} from '@/hooks/useInterview';
import { useGetMyCvs } from '@/hooks/useCv';
import { usePublicJobs } from '@/hooks/usePublicJobs';
import { useJobDetail } from '@/hooks/useJobDetail';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { ListPagination } from '@/components/shared/ListPagination';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
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
} from 'lucide-react';
import type { DifficultyLevel } from '@/types/interview.types';

import { Suspense } from 'react';
import { PageLoader } from '@/components/shared/PageLoader';

function CandidateInterviewContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [isOpen, setIsOpen] = useState(false);
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('MEDIUM');
  const [selectedCv, setSelectedCv] = useState<string>('none');
  const [selectedJob, setSelectedJob] = useState<string>('none');
  const [selectedModel, setSelectedModel] = useState<string>('Gemini');
  const [isJdPopoverOpen, setIsJdPopoverOpen] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null);

  const { data: sessionsRes, isLoading: sessionsLoading } = useGetInterviewSessions();
  const { data: cvsRes } = useGetMyCvs();
  const { data: jobsRes } = usePublicJobs({ page: 1, pageSize: 50 });
  const { data: jobDetailRes, isLoading: jobDetailLoading } = useJobDetail(
    selectedJob === 'none' ? '' : selectedJob,
    true
  );
  const startSessionMutation = useCreateInterviewSession();
  const deleteSessionMutation = useDeleteInterviewSession();

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
    try {
      const res = await startSessionMutation.mutateAsync({
        difficultyLevel: difficulty,
        cvId: selectedCv === 'none' ? undefined : selectedCv,
        jobId: selectedJob === 'none' ? undefined : selectedJob,
        aiProvider: selectedModel,
      });

      if (res.success && res.data) {
        setIsOpen(false);
        router.push(`/candidate/interview/${res.data.id}`);
      }
    } catch (err) {
      console.error(err);
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

  const renderCvPreview = () => {
    const activeCv = cvs.find(c => c.id === selectedCv);
    if (!activeCv) return (
      <div className="flex items-center justify-center h-full text-sm text-muted-foreground bg-card">
        CV data not found
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
              Size: {formatSize(activeCv.fileSize)}
            </span>
          </div>
          <a
            href={activeCv.fileUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 rounded-md border border-border bg-card px-2.5 py-1 text-[11px] font-semibold text-foreground hover:bg-muted transition-colors"
          >
            <ExternalLink className="h-3 w-3" />
            <span>Open in new tab</span>
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
          Job details not found
        </div>
      );
    }

    const formatSalary = (min?: number, max?: number, curr: string = 'VND') => {
      if (min && max) return `${min.toLocaleString()} - ${max.toLocaleString()} ${curr}`;
      if (min) return `From ${min.toLocaleString()} ${curr}`;
      if (max) return `Up to ${max.toLocaleString()} ${curr}`;
      return 'Negotiable';
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
              <h5 className="font-bold text-foreground">Required Skills:</h5>
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
              <h5 className="font-bold text-foreground">Job Requirements:</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {jobDetail.requirements}
              </div>
            </div>
          )}

          {jobDetail.description && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">Job Description:</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {jobDetail.description}
              </div>
            </div>
          )}

          {jobDetail.benefits && (
            <div className="space-y-1">
              <h5 className="font-bold text-foreground">Benefits & Perks:</h5>
              <div className="text-muted-foreground whitespace-pre-line text-[11px] bg-muted/20 p-2.5 rounded-lg border border-border/40">
                {jobDetail.benefits}
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
          <h1 className="text-3xl font-bold tracking-tight">AI Mock Interview</h1>
          <p className="text-muted-foreground mt-2 max-w-2xl">
            Master your interview skills with personalized AI sessions.
          </p>
        </div>
        <Button onClick={() => setIsOpen(true)} className="bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
          <Plus className="mr-1 h-4 w-4" />
          Start Mock Interview
          <Sparkles className="mr-2 h-4 w-4 ml-1" />
        </Button>
      </div>

      <div className="flex flex-col gap-4">

          {sessionsLoading ? (
            <div className="flex flex-col gap-3">
              {[1, 2, 3].map((n) => <CardSkeleton key={n} />)}
            </div>
          ) : sessions.length === 0 ? (
            <Card className="border border-dashed border-border bg-muted/20 py-16 text-center rounded-2xl">
              <CardContent className="space-y-6 max-w-sm mx-auto">
                <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-card border border-border text-muted-foreground">
                  <MessageSquare className="h-8 w-8" />
                </div>
                <div className="space-y-2">
                  <h3 className="text-lg font-semibold text-foreground">No mock interview sessions yet</h3>
                  <p className="text-sm text-muted-foreground leading-relaxed">
                    Start your first session to level up your interview skills and get detailed AI feedback.
                  </p>
                </div>
                <Button
                  variant="outline"
                  onClick={() => setIsOpen(true)}
                  className="w-full border-border text-foreground hover:bg-muted"
                >
                  Click here to start
                </Button>
              </CardContent>
            </Card>
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
                          <div className={`shrink-0 w-11 h-11 rounded-lg flex items-center justify-center border border-border ${
                            session.status === 'IN_PROGRESS'
                              ? 'bg-blue-500/10 text-blue-600'
                              : 'bg-emerald-500/10 text-emerald-600'
                          }`}>
                            <MessageSquare className="w-5 h-5" />
                          </div>

                          {/* Center: Info */}
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 min-w-0">
                              <span className="font-medium text-base text-foreground group-hover:text-primary transition-colors line-clamp-1 leading-snug">
                                {session.jobTitle || 'Free Mock Interview'}
                              </span>
                            </div>
                            <div className="flex items-center gap-4 flex-wrap mt-1 text-sm text-slate-600">
                              <div className="flex items-center">
                                <Badge
                                  className={`shrink-0 text-xs px-2 py-0.5 border-none font-medium ${
                                    session.status === 'IN_PROGRESS'
                                      ? 'bg-blue-500/10 text-blue-700'
                                      : 'bg-emerald-500/10 text-emerald-700'
                                  }`}
                                >
                                  {session.status === 'IN_PROGRESS' ? 'In Progress' : 'Completed'}
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
                          <Button size="sm" variant="outline" className="gap-1.5 h-9" onClick={(e) => { e.stopPropagation(); router.push(`/candidate/interview/${session.id}`); }}>
                            {session.status === 'IN_PROGRESS' ? (
                              <><Play className="w-4 h-4 fill-current" /> Resume</>
                            ) : (
                              <><Eye className="w-4 h-4" /> Review</>
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
                                  className="w-full justify-start gap-2 h-9 text-rose-600 hover:text-rose-700 hover:bg-rose-50"
                                  onClick={(e) => handleDeleteSession(e, session.id)}
                                >
                                  <Trash2 className="h-4 w-4" />
                                  <span>Delete Session</span>
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
                  <BrainCircuit className="h-5 w-5 text-primary" /> Mock Interview Setup
                </DialogTitle>
                <DialogDescription className="text-muted-foreground text-sm">
                  Customize parameters for your best mock interview experience.
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-5 py-2">
                {/* Choose CV */}
                <div className="space-y-2">
                  <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                    <FileText className="h-4 w-4 text-primary" /> Use CV Information (Optional)
                  </label>
                  <Select value={selectedCv} onValueChange={(val) => setSelectedCv(val ?? 'none')}>
                    <SelectTrigger className="w-full h-11 px-3 bg-card border-border hover:border-primary/50 focus:ring-primary/20 hover:bg-muted/10 transition-all rounded-xl shadow-sm text-sm font-medium">
                      <SelectValue placeholder="Select CV">
                        {selectedCv === 'none'
                          ? (
                            <div className="flex items-center gap-2 text-muted-foreground font-medium">
                              <X className="h-4 w-4 shrink-0" />
                              <span>No CV (General Knowledge)</span>
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
                          <span>No CV (General Knowledge)</span>
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
                    <Briefcase className="h-4 w-4 text-indigo-500" /> Interview based on Job Description (Optional)
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
                            <span className="text-muted-foreground font-normal">No JD (Freeform Questions)</span>
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
                        <CommandInput placeholder="Search job descriptions..." className="h-9" />
                        <CommandList>
                          <CommandEmpty>No job descriptions found.</CommandEmpty>
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
                                  <span>No JD (Freeform Questions)</span>
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
                      <Layers className="h-4 w-4 text-indigo-500" /> Select Level
                    </label>
                    <Select value={difficulty} onValueChange={(val) => setDifficulty(val as DifficultyLevel)}>
                      <SelectTrigger className="w-full h-11 px-3 bg-card border-border hover:border-primary/50 focus:ring-primary/20 hover:bg-muted/10 transition-all rounded-xl shadow-sm text-sm font-medium">
                        <SelectValue placeholder="Select Level" />
                      </SelectTrigger>
                      <SelectContent alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground">
                        <SelectItem value="EASY">Intern / Fresher</SelectItem>
                        <SelectItem value="MEDIUM">Middle / Junior</SelectItem>
                        <SelectItem value="HARD">Senior</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                )}
              </div>
            </div>

            <DialogFooter className="mt-8 flex flex-col sm:flex-row gap-3 pt-4 border-t border-border/60 shrink-0">
              <Button
                variant="outline"
                onClick={() => setIsOpen(false)}
                className="w-full h-10 border-border text-muted-foreground hover:bg-muted font-medium"
              >
                Cancel
              </Button>
              <Button
                onClick={handleStartInterview}
                disabled={startSessionMutation.isPending}
                className="w-full h-10 bg-primary hover:bg-primary/90 text-primary-foreground font-semibold flex items-center justify-center gap-2 transition-colors"
              >
                {startSessionMutation.isPending ? (
                  <>Initializing interview...</>
                ) : (
                  <>
                    <Play className="h-4 w-4 fill-current" /> Start Now
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
                <h3 className="text-lg font-bold text-foreground">Ready for your interview?</h3>
                <p className="text-xs text-muted-foreground max-w-xs leading-relaxed">
                  Select a CV or Job Description on the left to preview the document and let AI prepare the best questions for you.
                </p>
                <div className="bg-card border border-border rounded-xl p-4 text-left text-[11px] text-muted-foreground w-full max-w-xs space-y-2">
                  <p className="font-semibold text-foreground flex items-center gap-1.5">
                    <Info className="h-3.5 w-3.5 text-primary" /> Preparation Tips:
                  </p>
                  <ul className="list-disc pl-4 space-y-1">
                    <li>Use your CV to let AI dive deep into your experience.</li>
                    <li>Use a Job Description to align with your target role.</li>
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
                          <FileText className="h-3.5 w-3.5 mr-1.5" /> View CV
                        </TabsTrigger>
                        <TabsTrigger value="jd" className="text-xs font-semibold py-1.5 px-3">
                          <Briefcase className="h-3.5 w-3.5 mr-1.5" /> JD Details
                        </TabsTrigger>
                      </TabsList>
                      <div className="text-[10px] text-muted-foreground font-medium hidden lg:block">
                        CV & JD Combined Mode
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
                        <FileText className="h-4 w-4 text-primary" /> Preview Selected CV
                      </h3>
                      <span className="text-[10px] bg-primary/10 text-primary px-2 py-0.5 rounded-full font-semibold">
                        CV Only
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
                        <Briefcase className="h-4 w-4 text-indigo-500" /> Job Description Details
                      </h3>
                      <span className="text-[10px] bg-indigo-500/10 text-indigo-600 px-2 py-0.5 rounded-full font-semibold">
                        JD Only
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
            <DialogTitle>Delete Mock Interview Session?</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this mock interview session? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 flex flex-col sm:flex-row gap-2 justify-end">
            <Button variant="outline" onClick={() => setSessionToDelete(null)} disabled={deleteSessionMutation.isPending}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleConfirmDelete} disabled={deleteSessionMutation.isPending}>
              {deleteSessionMutation.isPending ? 'Deleting...' : 'Delete'}
            </Button>
          </DialogFooter>
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
