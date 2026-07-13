'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  useGetInterviewSessions,
  useCreateInterviewSession,
  useDeleteInterviewSession,
} from '@/hooks/useInterview';
import { useGetMyCvs } from '@/hooks/useCv';
import { usePublicJobs } from '@/hooks/usePublicJobs';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
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
} from 'lucide-react';
import type { DifficultyLevel } from '@/types/interview.types';

export default function CandidateInterviewPage() {
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('MEDIUM');
  const [selectedCv, setSelectedCv] = useState<string>('none');
  const [selectedJob, setSelectedJob] = useState<string>('none');
  const [selectedModel, setSelectedModel] = useState<string>('Gemini');

  const { data: sessionsRes, isLoading: sessionsLoading } = useGetInterviewSessions();
  const { data: cvsRes } = useGetMyCvs();
  const { data: jobsRes } = usePublicJobs({ page: 1, pageSize: 50 });
  const startSessionMutation = useCreateInterviewSession();
  const deleteSessionMutation = useDeleteInterviewSession();

  const sessions = sessionsRes?.data || [];
  const cvs = cvsRes?.data || [];
  const jobs = jobsRes?.data || [];

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

  const handleDeleteSession = async (e: React.MouseEvent, sessionId: string) => {
    e.stopPropagation();
    if (confirm('Bạn có chắc chắn muốn xóa phiên phỏng vấn thử này không?')) {
      try {
        await deleteSessionMutation.mutateAsync(sessionId);
      } catch (err) {
        console.error(err);
      }
    }
  };

  return (
    <div className="container mx-auto px-4 py-8 max-w-6xl space-y-8 animate-in fade-in duration-500">
      {/* Header section with glassy gradients aligned with light theme */}
      <div className="relative overflow-hidden rounded-3xl border border-border bg-card p-8 md:p-12 shadow-sm">
        <div className="absolute -right-20 -top-20 h-60 w-60 rounded-full bg-primary/10 blur-[100px] pointer-events-none" />
        <div className="absolute -left-20 -bottom-20 h-60 w-60 rounded-full bg-indigo-500/10 blur-[100px] pointer-events-none" />

        <div className="relative flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
          <div className="space-y-4 max-w-2xl">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 border border-primary/20 text-primary text-sm font-semibold">
              <BrainCircuit className="h-4 w-4" /> AI Mock Interview
            </div>
            <h1 className="text-3xl md:text-4xl font-extrabold tracking-tight text-foreground">
              Luyện Tập Phỏng Vấn Với AI
            </h1>
            <p className="text-muted-foreground leading-relaxed text-sm md:text-base">
              Nâng cao kỹ năng phỏng vấn của bạn. AI Interviewer sẽ hỏi các câu hỏi kỹ thuật, 
              kỹ năng mềm phù hợp dựa trên CV và công việc bạn ứng tuyển, kèm theo nhận xét chi tiết sau mỗi câu trả lời.
            </p>
          </div>
          <Button
            size="lg"
            onClick={() => setIsOpen(true)}
            className="w-full md:w-auto bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm px-8 py-6 rounded-2xl transition-all duration-300 font-semibold"
          >
            <Plus className="h-5 w-5 mr-2" /> Bắt đầu phỏng vấn thử
          </Button>
        </div>
      </div>

      {/* Main Sessions Section */}
      <div className="space-y-6">
        <h2 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
          <MessageSquare className="h-5 w-5 text-primary" /> Lịch sử phỏng vấn thử ({sessions.length})
        </h2>

        {sessionsLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[1, 2, 3].map((n) => (
              <Card key={n} className="bg-card border-border">
                <CardHeader className="space-y-2">
                  <Skeleton className="h-4 w-1/3" />
                  <Skeleton className="h-6 w-3/4" />
                </CardHeader>
                <CardContent>
                  <Skeleton className="h-4 w-1/2" />
                </CardContent>
              </Card>
            ))}
          </div>
        ) : sessions.length === 0 ? (
          <Card className="border border-dashed border-border bg-muted/20 py-16 text-center rounded-2xl">
            <CardContent className="space-y-6 max-w-sm mx-auto">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-card border border-border text-muted-foreground">
                <MessageSquare className="h-8 w-8" />
              </div>
              <div className="space-y-2">
                <h3 className="text-lg font-semibold text-foreground">Chưa có lượt phỏng vấn thử nào</h3>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  Hãy bắt đầu lượt đầu tiên của bạn để nâng cấp kỹ năng trả lời và nhận phản hồi chi tiết từ AI.
                </p>
              </div>
              <Button
                variant="outline"
                onClick={() => setIsOpen(true)}
                className="w-full border-border text-foreground hover:bg-muted"
              >
                Nhấp vào đây để bắt đầu
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {sessions.map((session) => (
              <Card
                key={session.id}
                onClick={() => router.push(`/candidate/interview/${session.id}`)}
                className="group cursor-pointer bg-card hover:bg-muted/10 border border-border hover:border-primary/30 shadow-sm hover:shadow-md rounded-2xl transition-all duration-300 flex flex-col justify-between overflow-hidden relative"
              >
                <CardHeader className="pb-4">
                  <div className="flex items-center justify-between gap-2 mb-2">
                    <div className="flex items-center gap-1.5">
                      <Badge
                        variant="secondary"
                        className={
                          session.status === 'IN_PROGRESS'
                            ? 'bg-emerald-50 text-emerald-600 border border-emerald-200'
                            : 'bg-slate-100 text-slate-600 border border-slate-200'
                        }
                      >
                        {session.status === 'IN_PROGRESS' ? 'Đang diễn ra' : 'Đã kết thúc'}
                      </Badge>
                      <Badge variant="outline" className="border-border text-muted-foreground">
                        {session.difficultyLevel}
                      </Badge>
                    </div>

                    <Button
                      variant="ghost"
                      size="icon"
                      disabled={deleteSessionMutation.isPending}
                      onClick={(e) => handleDeleteSession(e, session.id)}
                      className="h-7 w-7 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors shrink-0"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                  <CardTitle className="text-lg font-semibold text-foreground group-hover:text-primary transition-colors line-clamp-1">
                    {session.jobTitle || 'Phỏng vấn thử tự do'}
                  </CardTitle>
                  <CardDescription className="text-xs text-muted-foreground flex items-center gap-1.5 mt-1">
                    <Calendar className="h-3.5 w-3.5" />
                    {session.startedAt ? new Date(session.startedAt).toLocaleDateString('vi-VN') : 'N/A'}
                  </CardDescription>
                </CardHeader>
                <CardContent className="pb-4 space-y-2">
                  {session.cvFileName && (
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <FileText className="h-3.5 w-3.5 text-primary shrink-0" />
                      <span className="truncate">{session.cvFileName}</span>
                    </div>
                  )}
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <Cpu className="h-3.5 w-3.5 text-indigo-500 shrink-0" />
                    <span>Model: {session.aiProvider || 'Gemini'}</span>
                  </div>
                </CardContent>
                <CardFooter className="pt-2 border-t border-border bg-muted/10 flex justify-between items-center text-xs font-semibold text-primary group-hover:text-primary/80">
                  <span>Xem chi tiết</span>
                  <ArrowRight className="h-4 w-4 transform group-hover:translate-x-1 transition-transform" />
                </CardFooter>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Startup Session Modal */}
      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="max-w-[95vw] sm:max-w-lg max-h-[90vh] overflow-y-auto bg-card border border-border text-foreground rounded-2xl p-6">
          <DialogHeader>
            <DialogTitle className="text-xl font-bold flex items-center gap-2 text-foreground">
              <BrainCircuit className="h-5 w-5 text-primary" /> Thiết lập buổi phỏng vấn thử
            </DialogTitle>
            <DialogDescription className="text-muted-foreground text-sm">
              Tùy chỉnh thông số để buổi phỏng vấn thử phù hợp nhất với mong muốn của bạn.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-5 py-4">
            {/* Choose CV */}
            <div className="space-y-2">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <FileText className="h-4 w-4 text-primary" /> Sử dụng thông tin từ CV (Tùy chọn)
              </label>
              <Select value={selectedCv} onValueChange={(val) => setSelectedCv(val ?? 'none')}>
                <SelectTrigger className="w-full bg-card border-input focus:ring-primary text-foreground rounded-xl">
                  <SelectValue placeholder="Chọn CV" />
                </SelectTrigger>
                <SelectContent className="bg-popover border-border text-popover-foreground">
                  <SelectItem value="none">Không dùng CV (Hỏi kiến thức chung)</SelectItem>
                  {cvs.map((cv) => (
                    <SelectItem key={cv.id} value={cv.id}>
                      {cv.fileName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Choose Job Description */}
            <div className="space-y-2">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <Briefcase className="h-4 w-4 text-indigo-500" /> Phỏng vấn theo Tin tuyển dụng (Tùy chọn)
              </label>
              <Select value={selectedJob} onValueChange={(val) => setSelectedJob(val ?? 'none')}>
                <SelectTrigger className="w-full bg-card border-input focus:ring-primary text-foreground rounded-xl">
                  <SelectValue placeholder="Chọn Tin tuyển dụng" />
                </SelectTrigger>
                <SelectContent className="bg-popover border-border text-popover-foreground">
                  <SelectItem value="none">Không dùng JD (Câu hỏi tự do)</SelectItem>
                  {jobs.map((job) => (
                    <SelectItem key={job.id} value={job.id}>
                      {job.title}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Difficulty Level & AI Model */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                  <Layers className="h-4 w-4 text-emerald-500" /> Cấp độ khó
                </label>
                <Select value={difficulty} onValueChange={(val) => setDifficulty((val ?? 'MEDIUM') as DifficultyLevel)}>
                  <SelectTrigger className="w-full bg-card border-input focus:ring-primary text-foreground rounded-xl">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="bg-popover border-border text-popover-foreground">
                    <SelectItem value="EASY">Dễ (Easy)</SelectItem>
                    <SelectItem value="MEDIUM">Trung bình (Medium)</SelectItem>
                    <SelectItem value="HARD">Khó (Hard)</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                  <Cpu className="h-4 w-4 text-cyan-500" /> Chọn Model AI
                </label>
                <Select value={selectedModel} onValueChange={(val) => setSelectedModel(val ?? 'Gemini')}>
                  <SelectTrigger className="w-full bg-card border-input focus:ring-primary text-foreground rounded-xl">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="bg-popover border-border text-popover-foreground">
                    <SelectItem value="Gemini">Gemini 2.5 Flash</SelectItem>
                    <SelectItem value="OpenAI">GPT-4o (OpenAI)</SelectItem>
                    <SelectItem value="Claude">Claude 3.5 Sonnet</SelectItem>
                    <SelectItem value="Groq">Groq (Llama 3.3)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>

          <DialogFooter className="mt-6 flex flex-col sm:flex-row gap-3">
            <Button
              variant="outline"
              onClick={() => setIsOpen(false)}
              className="w-full border-border text-muted-foreground hover:bg-muted"
            >
              Hủy
            </Button>
            <Button
              onClick={handleStartInterview}
              disabled={startSessionMutation.isPending}
              className="w-full bg-primary hover:bg-primary/90 text-primary-foreground font-semibold flex items-center justify-center gap-2"
            >
              {startSessionMutation.isPending ? (
                <>Khởi tạo phỏng vấn...</>
              ) : (
                <>
                  <Play className="h-4 w-4 fill-current" /> Bắt đầu ngay
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
