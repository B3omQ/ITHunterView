'use client';

import { useState, useRef, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import {
  useGetInterviewSessionDetail,
  useSubmitInterviewReply,
  useSwitchInterviewModel,
  useCompleteInterviewSession,
  useTranscribeAudio,
} from '@/hooks/useInterview';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Progress } from '@/components/ui/progress';
import {
  Bot,
  User,
  ArrowLeft,
  Send,
  Flag,
  Sparkles,
  Cpu,
  Layers,
  CheckCircle,
  AlertCircle,
  Check,
  Mic,
  Loader2,
} from 'lucide-react';

export default function CandidateInterviewActivePage() {
  const params = useParams();
  const router = useRouter();
  const sessionId = typeof params.sessionId === 'string' ? params.sessionId : '';

  const { data: detailRes, isLoading, isError } = useGetInterviewSessionDetail(sessionId);
  const submitReplyMutation = useSubmitReplyWithAutoScroll();
  const switchModelMutation = useSwitchInterviewModel(sessionId);
  const completeSessionMutation = useCompleteInterviewSession(sessionId);

  const [inputMessage, setInputMessage] = useState('');
  const [isReportOpen, setIsReportOpen] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);
  const chatScrollContainerRef = useRef<HTMLDivElement>(null);

  // STT recording states
  const [isRecording, setIsRecording] = useState(false);
  const [recordingDuration, setRecordingDuration] = useState(0);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  const transcribeMutation = useTranscribeAudio();

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      audioChunksRef.current = [];
      
      let options = {};
      if (MediaRecorder.isTypeSupported('audio/webm')) {
        options = { mimeType: 'audio/webm' };
      }
      
      const mediaRecorder = new MediaRecorder(stream, options);
      mediaRecorderRef.current = mediaRecorder;

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onstop = async () => {
        const audioBlob = new Blob(audioChunksRef.current, { type: mediaRecorder.mimeType || 'audio/webm' });
        // Stop all tracks to release microphone
        stream.getTracks().forEach(track => track.stop());
        
        try {
          const file = new File([audioBlob], 'recording.webm', { type: audioBlob.type });
          const res = await transcribeMutation.mutateAsync(file);
          const text = res.data;
          if (res.success && text) {
            setInputMessage((prev) => (prev ? prev + ' ' + text : text));
          }
        } catch (err) {
          console.error('Transcription error:', err);
        }
      };

      mediaRecorder.start();
      setIsRecording(true);
      setRecordingDuration(0);

      timerRef.current = setInterval(() => {
        setRecordingDuration((prev) => prev + 1);
      }, 1000);

    } catch (err) {
      console.error('Microphone access denied:', err);
      alert('Không thể truy cập Microphone. Vui lòng kiểm tra quyền thiết bị.');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      mediaRecorderRef.current.stop();
    }
    setIsRecording(false);
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
  };

  const formatDuration = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  useEffect(() => {
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  const detail = detailRes?.data;
  const session = detail?.session;
  const messages = detail?.messages || [];

  // Show report automatically if completed
  useEffect(() => {
    if (session?.status === 'COMPLETED') {
      setIsReportOpen(true);
    }
  }, [session?.status]);

  // Local state messages for instant optimistic render
  const [localMessages, setLocalMessages] = useState<any[]>([]);

  useEffect(() => {
    if (messages && messages.length > 0) {
      setLocalMessages(messages);
    }
  }, [messages]);

  // Helper hook to submit and scroll to bottom
  function useSubmitReplyWithAutoScroll() {
    return useSubmitInterviewReply(sessionId);
  }

  // Scroll to bottom when localMessages or loading state changes
  useEffect(() => {
    if (chatScrollContainerRef.current) {
      chatScrollContainerRef.current.scrollTop = chatScrollContainerRef.current.scrollHeight;
    }
  }, [localMessages, submitReplyMutation.isPending]);

  if (isLoading) {
    return (
      <div className="flex h-[80vh] items-center justify-center bg-background text-foreground">
        <div className="text-center space-y-4">
          <div className="relative flex justify-center items-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary" />
            <Sparkles className="absolute h-5 w-5 text-primary animate-pulse" />
          </div>
          <p className="text-muted-foreground text-sm">Đang tải buổi phỏng vấn...</p>
        </div>
      </div>
    );
  }

  if (isError || !session) {
    return (
      <div className="flex h-[80vh] items-center justify-center bg-background text-foreground">
        <div className="text-center space-y-4 max-w-md mx-auto px-4">
          <AlertCircle className="h-16 w-16 text-rose-500 mx-auto" />
          <h2 className="text-xl font-bold">Không tìm thấy buổi phỏng vấn</h2>
          <p className="text-muted-foreground text-sm">
            Buổi phỏng vấn có thể không tồn tại hoặc bạn không có quyền truy cập.
          </p>
          <Button onClick={() => router.push('/candidate/interview')} className="bg-primary hover:bg-primary/95 text-white">
            Quay lại lịch sử
          </Button>
        </div>
      </div>
    );
  }

  const handleSend = async () => {
    if (!inputMessage.trim() || submitReplyMutation.isPending) return;

    const messageToSend = inputMessage;
    setInputMessage('');

    // Optimistically update the last question with user response transcript instantly
    if (localMessages.length > 0) {
      const updated = [...localMessages];
      const lastIdx = updated.length - 1;
      updated[lastIdx] = {
        ...updated[lastIdx],
        candidateTranscript: messageToSend,
      };
      setLocalMessages(updated);
    }

    try {
      await submitReplyMutation.mutateAsync({ message: messageToSend });
    } catch (err) {
      console.error(err);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleSwitchModel = async (model: string | null) => {
    if (!model) return;
    try {
      await switchModelMutation.mutateAsync({ aiProvider: model });
    } catch (err) {
      console.error(err);
    }
  };

  const handleCompleteInterview = async () => {
    if (window.confirm('Bạn có chắc chắn muốn kết thúc buổi phỏng vấn thử này?')) {
      try {
        await completeSessionMutation.mutateAsync();
      } catch (err) {
        console.error(err);
      }
    }
  };

  return (
    <div className="flex flex-col lg:flex-row h-[calc(100vh-64px)] bg-background text-foreground overflow-hidden animate-in fade-in duration-300">
      {/* Configuration Sidebar */}
      <div className="w-full lg:w-80 border-b lg:border-b-0 lg:border-r border-border bg-card p-5 flex flex-col gap-6 shrink-0 shadow-sm">
        {/* Top meta */}
        <div className="flex items-center gap-3">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => router.push('/candidate/interview')}
            className="text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl"
          >
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h2 className="font-bold text-foreground text-base">Quay lại lịch sử</h2>
            <p className="text-xs text-muted-foreground">Xem các buổi luyện tập trước</p>
          </div>
        </div>

        <div className="border-t border-border my-1" />

        {/* Configurations */}
        <div className="space-y-5">
          <div>
            <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block mb-2">
              Chủ đề phỏng vấn
            </span>
            <div className="text-sm font-bold text-foreground line-clamp-2">
              {session.jobTitle || 'Luyện tập tự do'}
            </div>
            {session.cvFileName && (
              <div className="text-xs text-primary mt-1 truncate">
                CV: {session.cvFileName}
              </div>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block mb-1">
                Cấp độ
              </span>
              <div className="flex items-center gap-1.5 text-foreground">
                <Layers className="h-4 w-4 text-emerald-500" />
                <span className="text-sm font-semibold">{session.difficultyLevel}</span>
              </div>
            </div>
            <div>
              <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block mb-1">
                Trạng thái
              </span>
              <div className="flex items-center gap-1.5">
                <Badge
                  variant="secondary"
                  className={
                    session.status === 'IN_PROGRESS'
                      ? 'bg-emerald-50 text-emerald-600 border border-emerald-200 text-[10px]'
                      : 'bg-slate-100 text-slate-600 border border-slate-200 text-[10px]'
                  }
                >
                  {session.status === 'IN_PROGRESS' ? 'Đang phỏng vấn' : 'Hoàn thành'}
                </Badge>
              </div>
            </div>
          </div>

          {/* Model AI selection */}
          <div className="space-y-2">
            <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block">
              Mô hình AI phỏng vấn
            </span>
            <Select
              value={session.aiProvider || 'Gemini'}
              onValueChange={handleSwitchModel}
              disabled={session.status === 'COMPLETED' || switchModelMutation.isPending}
            >
              <SelectTrigger className="w-full bg-card border-input focus:ring-primary text-foreground rounded-xl h-10">
                <Cpu className="h-4 w-4 text-primary mr-2" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground">
                <SelectItem value="Gemini">Gemini 2.5 Flash</SelectItem>
                <SelectItem value="OpenAI">GPT-4o (OpenAI)</SelectItem>
                <SelectItem value="Claude">Claude 3.5 Sonnet</SelectItem>
                <SelectItem value="Groq">Groq (Llama 3.3)</SelectItem>
              </SelectContent>
            </Select>
            {session.status === 'IN_PROGRESS' && (
              <span className="text-[10px] text-muted-foreground block leading-tight">
                *Bạn có thể thay đổi mô hình bất cứ lúc nào trong cuộc đối thoại.
              </span>
            )}
          </div>
        </div>

        <div className="mt-auto pt-6 border-t border-border">
          {session.status === 'IN_PROGRESS' ? (
            <Button
              onClick={handleCompleteInterview}
              disabled={completeSessionMutation.isPending}
              className="w-full bg-destructive hover:bg-destructive/90 text-destructive-foreground rounded-xl py-5 font-semibold flex items-center justify-center gap-2"
            >
              <Flag className="h-4 w-4" /> Kết thúc buổi phỏng vấn
            </Button>
          ) : (
            <div className="p-4 rounded-xl border border-primary/20 bg-primary/5 text-center space-y-2">
              <CheckCircle className="h-8 w-8 text-primary mx-auto" />
              <div className="text-sm font-bold text-foreground">Phỏng vấn hoàn thành</div>
              <p className="text-[10px] text-muted-foreground">
                Buổi phỏng vấn đã được ghi nhận. Bạn có thể xem lại toàn bộ câu hỏi và nhận xét đánh giá ở khung bên.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col h-full bg-background relative overflow-hidden">
        {/* Scrollable messages history using standard div for native scrolling */}
        <div ref={chatScrollContainerRef} className="flex-1 overflow-y-auto px-4 md:px-8 py-6 space-y-8 pb-12">
          <div className="max-w-3xl mx-auto space-y-8">
            {localMessages.map((msg) => (
              <div key={msg.id} className="space-y-6 animate-in fade-in duration-300">
                {/* AI Question */}
                <div className="flex items-start gap-4">
                  <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-primary/10 border border-primary/20 text-primary shadow-sm">
                    <Bot className="h-5 w-5" />
                  </div>
                  <div className="flex-1 space-y-1.5">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold text-foreground">AI Interviewer</span>
                      <span className="text-[10px] text-muted-foreground">
                        {new Date(msg.createdAt).toLocaleTimeString([], {
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </span>
                    </div>
                    <div className="text-sm leading-relaxed text-foreground bg-muted/40 border border-border p-4 rounded-2xl rounded-tl-none inline-block max-w-full">
                      {msg.questionText}
                    </div>
                  </div>
                </div>

                {/* User Answer (if transcript is filled) */}
                {msg.candidateTranscript && (
                  <div className="flex items-start justify-end gap-4">
                    <div className="flex-1 flex flex-col items-end space-y-1.5 max-w-[85%]">
                      <div className="flex items-center gap-2">
                        <span className="text-[10px] text-muted-foreground">
                          {new Date(msg.createdAt).toLocaleTimeString([], {
                            hour: '2-digit',
                            minute: '2-digit',
                          })}
                        </span>
                        <span className="text-xs font-bold text-primary">Bạn</span>
                      </div>
                      <div className="text-sm leading-relaxed text-white bg-primary p-4 rounded-2xl rounded-tr-none text-left">
                        {msg.candidateTranscript}
                      </div>
                    </div>
                    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-primary text-white shadow-sm">
                      <User className="h-5 w-5" />
                    </div>
                  </div>
                )}

                {/* Evaluation Feedback & Scores (if present) */}
                {msg.aiFeedback && (
                  (() => {
                    interface RubricEvaluation {
                      question_type: string;
                      technical_score?: {
                        [key: string]: number | null | undefined;
                        average?: number;
                      };
                      soft_skill_score?: {
                        [key: string]: number | null | undefined;
                        average?: number;
                      };
                      evidence?: string;
                      general_feedback?: string;
                      strengths?: string[];
                      improvements?: string[];
                    }

                    const rubric = (() => {
                      if (!msg.aiFeedback) return null;
                      const trimmed = msg.aiFeedback.trim();
                      if (trimmed.startsWith('{') && trimmed.endsWith('}')) {
                        try {
                          return JSON.parse(trimmed) as RubricEvaluation;
                        } catch (e) {
                          return null;
                        }
                      }
                      return null;
                    })();

                    const rubricDefinitions: Record<string, string> = {
                      T1: "Độ chính xác kiến thức",
                      T2: "Độ sâu / hiểu bản chất",
                      T3: "Khả năng giải quyết vấn đề",
                      T4: "Chất lượng giải pháp/code",
                      T5: "Ứng dụng thực tế",
                      T6: "Nhận biết giới hạn bản thân",
                      S1: "Cấu trúc trình bày (STAR)",
                      S2: "Sự rõ ràng & súc tích",
                      S3: "Sự tự tin & thái độ",
                      S4: "Khả năng giao tiếp kỹ thuật",
                      S5: "Tư duy phản biện/tự nhận thức",
                      S6: "Khả năng xử lý áp lực",
                    };

                    const getBadgeColor = (score: number | null | undefined) => {
                      if (score === null || score === undefined) return "bg-muted/40 text-muted-foreground border-border";
                      if (score >= 4) return "bg-emerald-50 text-emerald-600 border-emerald-200";
                      if (score >= 3) return "bg-amber-50 text-amber-600 border-amber-200";
                      return "bg-rose-50 text-rose-600 border-rose-200";
                    };

                    if (!rubric) {
                      // Fallback: render old simple text feedback
                      return (
                        <div className="pl-13 max-w-3xl">
                          <Card className="border border-border bg-card overflow-hidden rounded-2xl shadow-sm">
                            {/* Metric Scores */}
                            <div className="grid grid-cols-3 border-b border-border bg-muted/20 p-4 gap-4">
                              <div className="space-y-1.5">
                                <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider block">
                                  Logic & Thuật toán
                                </span>
                                <div className="flex items-center gap-2">
                                  <span className="text-sm font-extrabold text-primary">{msg.scoreLogic}%</span>
                                  <Progress value={msg.scoreLogic ?? null} className="h-1.5 bg-muted" />
                                </div>
                              </div>
                              <div className="space-y-1.5">
                                <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider block">
                                  Technical Depth
                                </span>
                                <div className="flex items-center gap-2">
                                  <span className="text-sm font-extrabold text-indigo-600">{msg.scoreTech}%</span>
                                  <Progress value={msg.scoreTech ?? null} className="h-1.5 bg-muted" />
                                </div>
                              </div>
                              <div className="space-y-1.5">
                                <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider block">
                                  Truyền đạt (Comm)
                                </span>
                                <div className="flex items-center gap-2">
                                  <span className="text-sm font-extrabold text-emerald-600">{msg.scoreCommunication}%</span>
                                  <Progress value={msg.scoreCommunication ?? null} className="h-1.5 bg-muted" />
                                </div>
                              </div>
                            </div>
                            {/* Feedback Text */}
                            <CardContent className="p-4 space-y-2">
                              <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-1.5">
                                <Sparkles className="h-3.5 w-3.5 text-primary" /> Đánh giá & Gợi ý cải thiện
                              </div>
                              <p className="text-xs md:text-sm text-muted-foreground leading-relaxed italic">
                                "{msg.aiFeedback}"
                              </p>
                            </CardContent>
                          </Card>
                        </div>
                      );
                    }

                    // Render beautiful rubric scorecard
                    return (
                      <div className="pl-13 max-w-3xl space-y-3">
                        <Card className="border border-border bg-card overflow-hidden rounded-2xl shadow-sm">
                          {/* Top averages */}
                          <div className="grid grid-cols-3 border-b border-border bg-muted/20 p-4 gap-4">
                            <div className="space-y-1.5">
                              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider block">
                                Logic & Thuật toán
                              </span>
                              <div className="flex items-center gap-2">
                                <span className="text-sm font-extrabold text-primary">{msg.scoreLogic}%</span>
                                <Progress value={msg.scoreLogic ?? null} className="h-1.5 bg-muted" />
                              </div>
                            </div>
                            <div className="space-y-1.5">
                              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider block">
                                Technical Depth (T1-T6)
                              </span>
                              <div className="flex items-center gap-2">
                                <span className="text-sm font-extrabold text-indigo-600">{msg.scoreTech}%</span>
                                <Progress value={msg.scoreTech ?? null} className="h-1.5 bg-muted" />
                              </div>
                            </div>
                            <div className="space-y-1.5">
                              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider block">
                                Kỹ năng mềm (S1-S6)
                              </span>
                              <div className="flex items-center gap-2">
                                <span className="text-sm font-extrabold text-emerald-600">{msg.scoreCommunication}%</span>
                                <Progress value={msg.scoreCommunication ?? null} className="h-1.5 bg-muted" />
                              </div>
                            </div>
                          </div>

                          <CardContent className="p-5 space-y-5">
                            {/* Rubric metrics grid */}
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                              {/* Technical Rubric */}
                              {rubric.technical_score && (
                                <div className="space-y-2">
                                  <div className="text-xs font-bold text-indigo-600 uppercase tracking-wider">
                                    Tiêu chí Kỹ thuật (Technical)
                                  </div>
                                  <div className="flex flex-col gap-1.5">
                                    {["T1", "T2", "T3", "T4", "T5", "T6"].map((tKey) => {
                                      const score = rubric.technical_score?.[tKey];
                                      return (
                                        <div
                                          key={tKey}
                                          className={`px-3 py-1.5 text-xs font-semibold rounded-lg border flex items-center justify-between transition-all duration-200 hover:scale-[1.01] ${getBadgeColor(score)}`}
                                        >
                                          <span>{rubricDefinitions[tKey]}</span>
                                          <span>{score !== null && score !== undefined ? `${score}/5` : 'N/A'}</span>
                                        </div>
                                      );
                                    })}
                                  </div>
                                </div>
                              )}

                              {/* Soft Skill Rubric */}
                              {rubric.soft_skill_score && (
                                <div className="space-y-2">
                                  <div className="text-xs font-bold text-emerald-600 uppercase tracking-wider">
                                    Tiêu chí Kỹ năng mềm (Soft Skills)
                                  </div>
                                  <div className="flex flex-col gap-1.5">
                                    {["S1", "S2", "S3", "S4", "S5", "S6"].map((sKey) => {
                                      const score = rubric.soft_skill_score?.[sKey];
                                      return (
                                        <div
                                          key={sKey}
                                          className={`px-3 py-1.5 text-xs font-semibold rounded-lg border flex items-center justify-between transition-all duration-200 hover:scale-[1.01] ${getBadgeColor(score)}`}
                                        >
                                          <span>{rubricDefinitions[sKey]}</span>
                                          <span>{score !== null && score !== undefined ? `${score}/5` : 'N/A'}</span>
                                        </div>
                                      );
                                    })}
                                  </div>
                                </div>
                              )}
                            </div>

                            {/* General Feedback Block */}
                            {(rubric.general_feedback || rubric.evidence) && (
                              <div className="bg-muted/30 border border-border/80 rounded-xl p-3.5 space-y-1">
                                <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                                  Nhận xét chung về câu trả lời
                                </div>
                                <p className="text-xs text-foreground leading-relaxed">
                                  {rubric.general_feedback || rubric.evidence}
                                </p>
                              </div>
                            )}

                            {/* Strengths & Improvements */}
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                              {rubric.strengths && rubric.strengths.length > 0 && (
                                <div className="space-y-2">
                                  <div className="text-xs font-bold text-emerald-600 uppercase tracking-wider flex items-center gap-1">
                                    <CheckCircle className="h-3.5 w-3.5" /> Điểm mạnh nổi bật
                                  </div>
                                  <ul className="text-xs text-muted-foreground space-y-1.5 pl-1">
                                    {rubric.strengths.map((str, idx) => (
                                      <li key={idx} className="flex items-start gap-1.5">
                                        <Check className="h-3.5 w-3.5 text-emerald-500 shrink-0 mt-0.5" />
                                        <span>{str}</span>
                                      </li>
                                    ))}
                                  </ul>
                                </div>
                              )}

                              {rubric.improvements && rubric.improvements.length > 0 && (
                                <div className="space-y-2">
                                  <div className="text-xs font-bold text-amber-600 uppercase tracking-wider flex items-center gap-1">
                                    <AlertCircle className="h-3.5 w-3.5" /> Gợi ý cải thiện
                                  </div>
                                  <ul className="text-xs text-muted-foreground space-y-1.5 pl-1">
                                    {rubric.improvements.map((imp, idx) => (
                                      <li key={idx} className="flex items-start gap-1.5">
                                        <span className="h-1.5 w-1.5 rounded-full bg-amber-400 shrink-0 mt-1.5 ml-1" />
                                        <span>{imp}</span>
                                      </li>
                                    ))}
                                  </ul>
                                </div>
                              )}
                            </div>
                          </CardContent>
                        </Card>
                      </div>
                    );
                  })()
                )}
              </div>
            ))}

            {/* Simulated loading bubble when AI is processing */}
            {submitReplyMutation.isPending && (
              <div className="flex items-start gap-4 animate-pulse">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-primary/10 border border-primary/20 text-primary">
                  <Bot className="h-5 w-5" />
                </div>
                <div className="flex-1 space-y-2">
                  <div className="text-xs font-bold text-muted-foreground">AI đang đánh giá và soạn câu hỏi tiếp theo...</div>
                  <div className="bg-muted/40 border border-border p-4 rounded-2xl rounded-tl-none inline-block">
                    <div className="flex gap-1.5 items-center">
                      <span className="h-2 w-2 bg-primary rounded-full animate-bounce [animation-delay:-0.3s]" />
                      <span className="h-2 w-2 bg-primary rounded-full animate-bounce [animation-delay:-0.15s]" />
                      <span className="h-2 w-2 bg-primary rounded-full animate-bounce" />
                    </div>
                  </div>
                </div>
              </div>
            )}

            <div ref={chatEndRef} />
          </div>
        </div>

        {/* Bottom Input Area */}
        <div className="p-4 md:p-6 border-t border-border bg-card">
          <div className="max-w-3xl mx-auto">
            {session.status === 'IN_PROGRESS' ? (
              <div className="relative flex items-center bg-background border border-border rounded-2xl p-2 focus-within:border-primary/50 focus-within:ring-1 focus-within:ring-primary/20 transition-all">
                <Textarea
                  value={inputMessage}
                  onChange={(e) => setInputMessage(e.target.value)}
                  onKeyDown={handleKeyPress}
                  placeholder="Nhập câu trả lời phỏng vấn của bạn tại đây... (Nhấn Enter để gửi)"
                  disabled={submitReplyMutation.isPending}
                  className="flex-1 min-h-[50px] max-h-[160px] bg-transparent border-0 focus-visible:ring-0 focus-visible:ring-offset-0 text-sm resize-none text-foreground placeholder-muted-foreground py-2.5 px-3"
                  rows={1}
                />
                {/* Voice transcription loading indicator */}
                {transcribeMutation.isPending && (
                  <div className="flex items-center gap-1.5 px-3 text-xs text-primary">
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    <span>Đang dịch...</span>
                  </div>
                )}
                
                {/* Recording duration counter */}
                {isRecording && (
                  <div className="flex items-center gap-1.5 px-3 text-xs text-rose-500 font-semibold animate-pulse">
                    <span className="h-2 w-2 rounded-full bg-rose-500" />
                    <span>Đang ghi âm: {formatDuration(recordingDuration)}</span>
                  </div>
                )}

                {/* Mic Record Toggle Button */}
                <Button
                  type="button"
                  onClick={isRecording ? stopRecording : startRecording}
                  disabled={submitReplyMutation.isPending || transcribeMutation.isPending}
                  size="icon"
                  className={`h-10 w-10 shrink-0 rounded-xl transition-all mr-2 ${
                    isRecording 
                      ? "bg-rose-500 hover:bg-rose-600 text-white animate-pulse" 
                      : "bg-muted hover:bg-muted/80 text-muted-foreground"
                  }`}
                >
                  <Mic className="h-4.5 w-4.5" />
                </Button>

                <Button
                  onClick={handleSend}
                  disabled={!inputMessage.trim() || submitReplyMutation.isPending || isRecording || transcribeMutation.isPending}
                  size="icon"
                  className="h-10 w-10 shrink-0 bg-primary hover:bg-primary/90 text-primary-foreground rounded-xl transition-all"
                >
                  <Send className="h-4.5 w-4.5 text-white" />
                </Button>
              </div>
            ) : (
              <div className="flex flex-col sm:flex-row items-center justify-between gap-4 p-4 bg-primary/5 border border-primary/20 rounded-2xl animate-in fade-in duration-300">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <CheckCircle className="h-5 w-5 text-emerald-600 shrink-0" />
                  <span className="text-sm font-semibold text-foreground">Buổi phỏng vấn thử này đã hoàn thành.</span>
                </div>
                <Button 
                  onClick={() => setIsReportOpen(true)}
                  className="bg-primary hover:bg-primary/95 text-white font-semibold rounded-xl flex items-center gap-2 shadow-sm shrink-0 w-full sm:w-auto h-10 px-5"
                >
                  <Sparkles className="h-4.5 w-4.5 text-white" /> Xem đánh giá tổng quan
                </Button>
              </div>
            )}
            <div className="flex justify-between items-center mt-2 px-1">
              <span className="text-[10px] text-muted-foreground">
                *Nhấn <span className="font-semibold text-foreground">Shift + Enter</span> để xuống dòng.
              </span>
              <span className="text-[10px] text-muted-foreground flex items-center gap-1">
                <Cpu className="h-3 w-3 text-primary" />
                Active Model: {session.aiProvider || 'Gemini'}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Overall Report Popup Modal */}
      {isReportOpen && session.status === 'COMPLETED' && detail?.report && (() => {
        const report = detail.report;
        const parsedReport = (() => {
          const trimmed = report.overallFeedback.trim();
          if (trimmed.startsWith('{') && trimmed.endsWith('}')) {
            try {
              return JSON.parse(trimmed);
            } catch (e) {
              return null;
            }
          }
          return null;
        })();

        const overallScore = parsedReport?.total_score ?? report.totalScore ?? 0;
        const overallFeedback = parsedReport?.overall_feedback ?? report.overallFeedback;
        const strengths = parsedReport?.strengths ?? [];
        const improvements = parsedReport?.improvements ?? [];

        return (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
            <div className="relative w-full max-w-3xl bg-card border border-border shadow-2xl rounded-3xl overflow-hidden max-h-[90vh] flex flex-col animate-in zoom-in-95 duration-200">
              
              {/* Close Button */}
              <button 
                onClick={() => setIsReportOpen(false)}
                className="absolute top-4 right-4 text-muted-foreground hover:text-foreground hover:bg-muted p-2 rounded-xl transition-all z-10"
              >
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>

              <div className="overflow-y-auto p-6 md:p-8 space-y-6">
                {/* Header Section */}
                <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 border-b border-border/80 pb-5 pr-8">
                  <div className="space-y-1">
                    <h3 className="text-lg md:text-xl font-extrabold text-foreground flex items-center gap-2">
                      <Sparkles className="h-5 w-5 text-primary" /> BÁO CÁO ĐÁNH GIÁ TỔNG QUAN
                    </h3>
                    <p className="text-xs text-muted-foreground">
                      Tổng hợp kết quả phỏng vấn thử từ AI Interviewer
                    </p>
                  </div>
                  {/* Score Badge */}
                  <div className="flex items-center gap-3 bg-card px-4 py-2.5 rounded-xl border border-border shadow-sm shrink-0">
                    <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                      Tổng điểm năng lực
                    </span>
                    <div className="flex items-baseline gap-0.5">
                      <span className="text-2xl font-black text-primary">{overallScore}</span>
                      <span className="text-xs text-muted-foreground">/100</span>
                    </div>
                  </div>
                </div>

                {/* General overview text */}
                <div className="space-y-2">
                  <h4 className="text-xs font-bold text-primary uppercase tracking-wider">
                    Đánh giá chung
                  </h4>
                  <p className="text-sm text-foreground leading-relaxed whitespace-pre-line bg-muted/20 border border-border/40 rounded-2xl p-4">
                    {overallFeedback}
                  </p>
                </div>

                {/* Strengths & Improvements */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {/* Strengths */}
                  <div className="space-y-3">
                    <h4 className="text-xs font-bold text-emerald-600 uppercase tracking-wider flex items-center gap-1.5">
                      <CheckCircle className="h-4 w-4" /> Điểm mạnh nổi bật
                    </h4>
                    {strengths.length > 0 ? (
                      <ul className="space-y-2 bg-emerald-50/10 border border-emerald-500/10 rounded-2xl p-4">
                        {strengths.map((str: string, idx: number) => (
                          <li key={idx} className="flex items-start gap-2 text-xs md:text-sm text-muted-foreground">
                            <Check className="h-4 w-4 text-emerald-500 shrink-0 mt-0.5" />
                            <span className="leading-relaxed">{str}</span>
                          </li>
                        ))}
                      </ul>
                    ) : (
                      <p className="text-xs text-muted-foreground italic">Không có thông tin.</p>
                    )}
                  </div>

                  {/* Improvements */}
                  <div className="space-y-3">
                    <h4 className="text-xs font-bold text-amber-600 uppercase tracking-wider flex items-center gap-1.5">
                      <AlertCircle className="h-4 w-4" /> Khía cạnh cần cải thiện
                    </h4>
                    {improvements.length > 0 ? (
                      <ul className="space-y-2 bg-amber-50/10 border border-amber-500/10 rounded-2xl p-4">
                        {improvements.map((imp: string, idx: number) => (
                          <li key={idx} className="flex items-start gap-2 text-xs md:text-sm text-muted-foreground">
                            <span className="h-1.5 w-1.5 rounded-full bg-amber-400 shrink-0 mt-2 ml-1.5" />
                            <span className="leading-relaxed">{imp}</span>
                          </li>
                        ))}
                      </ul>
                    ) : (
                      <p className="text-xs text-muted-foreground italic">Không có thông tin.</p>
                    )}
                  </div>
                </div>
              </div>

              {/* Footer */}
              <div className="border-t border-border p-4 bg-muted/20 flex justify-end gap-3 shrink-0">
                <Button 
                  onClick={() => setIsReportOpen(false)}
                  className="bg-primary hover:bg-primary/95 text-white font-semibold rounded-xl px-6"
                >
                  Đóng
                </Button>
              </div>

            </div>
          </div>
        );
      })()}
    </div>
  );
}
