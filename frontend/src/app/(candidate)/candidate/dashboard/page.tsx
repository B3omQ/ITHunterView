"use client"

import { useAuthStore } from "@/store/auth.store"
import Link from "next/link"
import Image from "next/image"
import { 
  Sparkles, FileSearch, 
  MessageCircleQuestion, 
  ChevronRight,
  Clock,
  Briefcase,
  AlertCircle,
  PlayCircle,
  TrendingUp,
  Activity,
  Crosshair,
  ScanSearch,
  CheckCircle2,
  Circle,
  Mic,
  BookOpen
} from "lucide-react"
import { useGetMatchHistory } from "@/hooks/useCvMatch"
import { useGetInterviewSessions } from "@/hooks/useInterview"
import { useMyLearningPaths } from "@/hooks/useLearningPath"
import { useProfileCompletionStatus, useClaimNewbieReward } from "@/hooks/useCandidateProfile"
import { Progress } from "@/components/ui/progress"
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, ResponsiveContainer, Radar, RadarChart, PolarGrid, PolarAngleAxis, PolarRadiusAxis } from "recharts"
import { useTranslations } from "next-intl"

export default function CandidateDashboard() {
  const { user } = useAuthStore()
  const t = useTranslations("CandidateDashboard")

  // 1. Fetch Data
  const { data: completionStatus } = useProfileCompletionStatus()
  const { mutate: claimReward, isPending: isClaiming } = useClaimNewbieReward()
  const { data: matchHistoryRes, isLoading: isMatchLoading } = useGetMatchHistory(1, 10)
  const matchHistory = (matchHistoryRes?.data?.items || []).filter(m => m.matchScore !== null && m.matchScore !== undefined)

  const { data: interviewsRes, isLoading: isInterviewLoading } = useGetInterviewSessions()
  const interviews = interviewsRes?.data || []

  const { data: pathsRes, isLoading: isPathsLoading } = useMyLearningPaths()
  const paths = pathsRes?.data || []

  // 2. Calculate KPIs
  // Average Match Score
  const avgMatchScore = matchHistory.length > 0 
    ? Math.round(matchHistory.reduce((acc, m) => acc + (m.matchScore || 0) * 100, 0) / matchHistory.length) 
    : 0;

  // Interviews Completed Count
  const completedInterviews = interviews.filter((i: any) => i.status === 'COMPLETED').length;

  // Active Learning Path Progress & Pending Tasks
  let activePath: any = null;
  let activeProgress = 0;
  let pendingTasks: any[] = [];

  if (paths.length > 0) {
    const pathsWithProgress = paths.map((path: any) => {
      let pd = path.pathData;
      if (typeof pd === 'string') {
        try { pd = JSON.parse(pd); } catch { pd = {}; }
      }
      pd = pd || {};
      
      let progress = 0;
      let totalTasks = 0;
      let completedTasksCount = 0;

      if (pd.modules) {
        pd.modules.forEach((m: any) => {
          if (m.tasks && m.tasks.length > 0) {
            totalTasks += m.tasks.length;
            completedTasksCount += m.tasks.filter((t: any) => t.completed).length;
          }
        });
        progress = totalTasks > 0 ? Math.round((completedTasksCount / totalTasks) * 100) : 0;
      }

      return { 
        ...path, 
        progress,
        roleName: pd.target_profile?.role_name || pd.title || path.title || 'Untitled Path',
        modules: pd.modules || [],
        pathData: pd
      };
    }).sort((a: any, b: any) => {
      if (b.progress !== a.progress) return b.progress - a.progress;
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });

    activePath = pathsWithProgress[0];
    activeProgress = activePath.progress;

    // Extract pending tasks
    if (activePath.modules) {
      for (const m of activePath.modules) {
        if (m.tasks) {
          for (const t of m.tasks) {
            if (!t.completed) {
              pendingTasks.push({ ...t, moduleTitle: m.title });
              if (pendingTasks.length >= 3) break;
            }
          }
        }
        if (pendingTasks.length >= 3) break;
      }
    }
  }

  // 3. Prepare Chart Data
  // Line Chart Data (Match History reversed for chronological order)
  const lineChartData = matchHistory.slice(0, 5).reverse().map(m => ({
    name: m.jdTitle?.substring(0, 10) + '...',
    score: m.matchScore,
    date: m.updatedAt ? new Date(m.updatedAt).toLocaleDateString() : 'N/A'
  }));

  // Real Radar Chart Data (Skill Readiness) from Active Path
  let radarChartData: any[] = [];
  if (activePath && activePath.pathData?.gap_summary?.gaps) {
    const gaps = activePath.pathData.gap_summary.gaps;
    // Map max 6 skills to keep radar chart clean
    radarChartData = gaps.slice(0, 6).map((gap: any) => ({
      subject: gap.skill_name || gap.skill_code || 'Skill',
      Current: gap.current_level || 0,
      Target: gap.target_level || 0,
      fullMark: 7
    }));
  }

  return (
    <div className="w-full pb-10 space-y-6">
      
      {/* Header Greeting */}
      <div className="mb-8 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-extrabold text-foreground tracking-tight mb-1 flex items-center gap-2">
            {t('greeting', { name: user?.fullName?.split(" ")[0] || "there" })}
            <Image src="/images/mascotAvatarGreeting.png" alt="Waving Mascot" width={36} height={36} className="w-9 h-9 object-contain mix-blend-multiply dark:mix-blend-normal transform hover:scale-110 hover:rotate-12 transition-transform cursor-pointer" />
          </h1>
          <p className="text-muted-foreground text-sm">
            {t('overviewText')}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Link href="/candidate/cv-matching" className="flex items-center gap-2 bg-blue-50 text-blue-600 hover:bg-blue-100 px-4 py-2 rounded-xl text-sm font-semibold transition-colors">
            <ScanSearch size={16} /> {t('scanCv')}
          </Link>
          <Link href="/candidate/interview" className="flex items-center gap-2 bg-emerald-50 text-emerald-600 hover:bg-emerald-100 px-4 py-2 rounded-xl text-sm font-semibold transition-colors">
            <Mic size={16} /> {t('mockInterview')}
          </Link>
        </div>
      </div>

      {/* Newbie Welcome Reward Banner */}
      {completionStatus && !completionStatus.isNewbieRewardClaimed && (
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-r from-violet-600 via-purple-600 to-indigo-700 p-6 sm:p-8 text-white shadow-xl shadow-purple-500/20 border border-white/10 transition-all duration-300 hover:shadow-2xl hover:shadow-purple-500/30 mb-8">
          <div className="absolute -top-24 -right-24 w-72 h-72 bg-white/10 rounded-full blur-3xl pointer-events-none" />
          <div className="absolute -bottom-24 -left-24 w-72 h-72 bg-amber-500/20 rounded-full blur-3xl pointer-events-none" />
          
          <div className="relative z-10 flex flex-col lg:flex-row items-start lg:items-center justify-between gap-6">
            <div className="space-y-3 max-w-2xl">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/20 backdrop-blur-md text-amber-300 text-xs font-bold tracking-wide uppercase border border-white/20 shadow-sm animate-pulse">
                <Sparkles size={14} className="text-amber-300" /> {t('newbieReward')}
              </div>
              <h2 className="text-2xl sm:text-3xl font-black tracking-tight text-white flex items-center gap-2">
                {t('receiveCoinTitle1')} <span className="text-transparent bg-clip-text bg-gradient-to-r from-amber-300 via-yellow-200 to-amber-400 font-extrabold">{t('receiveCoinTitle2')}</span> {t('receiveCoinTitle3')}
              </h2>
              <p className="text-purple-100 text-sm leading-relaxed">
                {t('newbieRewardDesc')}
              </p>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
                <div className="flex items-center gap-3 p-3 rounded-2xl bg-white/10 backdrop-blur-sm border border-white/10 text-sm">
                  {completionStatus.isEmailVerified ? (
                    <div className="p-1 rounded-full bg-emerald-500 text-white shadow-sm flex-shrink-0"><CheckCircle2 size={16} /></div>
                  ) : (
                    <div className="p-1 rounded-full bg-amber-500/20 text-amber-300 border border-amber-400/30 flex-shrink-0"><Circle size={16} /></div>
                  )}
                  <div className="flex flex-col">
                    <span className="font-semibold text-white">{t('verifyEmail')}</span>
                    <span className="text-xs text-purple-200">
                      {completionStatus.isEmailVerified ? t('completed') : t('checkEmail')}
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-3 p-3 rounded-2xl bg-white/10 backdrop-blur-sm border border-white/10 text-sm">
                  {completionStatus.isComplete ? (
                    <div className="p-1 rounded-full bg-emerald-500 text-white shadow-sm flex-shrink-0"><CheckCircle2 size={16} /></div>
                  ) : (
                    <div className="p-1 rounded-full bg-amber-500/20 text-amber-300 border border-amber-400/30 flex-shrink-0"><Circle size={16} /></div>
                  )}
                  <div className="flex flex-col">
                    <span className="font-semibold text-white">{t('completeProfile')}</span>
                    <span className="text-xs text-purple-200">
                      {completionStatus.isComplete ? t('completed') : t('progress', { percentage: completionStatus.completionPercentage })}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <div className="flex flex-col items-stretch sm:items-end w-full lg:w-auto mt-2 lg:mt-0 gap-3">
              {completionStatus.canClaimNewbieReward ? (
                <button
                  onClick={() => claimReward()}
                  disabled={isClaiming}
                  className="relative group overflow-hidden rounded-2xl bg-gradient-to-r from-amber-400 via-amber-300 to-yellow-400 px-8 py-4 text-slate-950 font-black text-base shadow-xl shadow-amber-500/25 hover:shadow-amber-500/40 hover:scale-[1.02] active:scale-[0.98] transition-all duration-300 disabled:opacity-60 flex items-center justify-center gap-2.5"
                >
                  <span className="relative z-10 flex items-center gap-2">
                    <Sparkles className="w-5 h-5 text-purple-700 animate-bounce" />
                    {isClaiming ? t('claiming') : t('claimNow')}
                  </span>
                </button>
              ) : (
                <Link
                  href="/candidate/profile"
                  className="rounded-2xl bg-white/15 hover:bg-white/20 border border-white/20 px-6 py-4 text-center font-bold text-sm text-white shadow-lg backdrop-blur-md transition-all flex items-center justify-center gap-2 group"
                >
                  <span>{t('completeConditions')}</span>
                  <ChevronRight size={18} className="group-hover:translate-x-1 transition-transform" />
                </Link>
              )}
              <span className="text-xs text-purple-200/80 text-center lg:text-right">
                {t('rewardNote')}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Row 1: Top-level KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
        <div className="bg-card border border-border rounded-2xl p-6 shadow-sm flex items-center gap-4">
          <div className="bg-blue-50 p-4 rounded-full flex-shrink-0">
            <Activity size={24} className="text-blue-500" />
          </div>
          <div>
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-1">{t('avgMatchScore')}</p>
            <div className="flex items-end gap-2">
              <h3 className="text-2xl font-black text-foreground">{avgMatchScore}%</h3>
              <span className="text-xs font-medium text-emerald-500 mb-1 flex items-center"><TrendingUp size={12} className="mr-0.5"/> {t('top20')}</span>
            </div>
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl p-6 shadow-sm flex items-center gap-4">
          <div className="bg-emerald-50 p-4 rounded-full flex-shrink-0">
            <MessageCircleQuestion size={24} className="text-emerald-500" />
          </div>
          <div>
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-1">{t('mockInterviews')}</p>
            <div className="flex items-end gap-2">
              <h3 className="text-2xl font-black text-foreground">{completedInterviews}</h3>
              <span className="text-xs font-medium text-muted-foreground mb-1">{t('mockInterviewsCompleted')}</span>
            </div>
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl p-6 shadow-sm flex items-center gap-4">
          <div className="bg-teal-50 p-4 rounded-full flex-shrink-0">
            <Crosshair size={24} className="text-teal-500" />
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-1">{t('learningProgress')}</p>
            <div className="flex items-end justify-between gap-2 mb-2">
              <h3 className="text-xl font-black text-foreground truncate">{activePath?.roleName || t('noPath')}</h3>
              <span className="text-sm font-bold text-teal-600">{activeProgress}%</span>
            </div>
            <Progress value={activeProgress} className="h-1.5 bg-teal-100 [&>div]:bg-teal-500 rounded-full" />
          </div>
        </div>
      </div>

      {/* Row 2: Data Visualizations */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        {/* Match Score Trend (Line Chart) */}
        <div className="bg-card border border-border rounded-2xl p-6 shadow-sm flex flex-col">
          <div className="mb-6">
            <h2 className="text-lg font-bold flex items-center gap-2">
              <TrendingUp size={20} className="text-blue-500" />
              {t('matchScoreTrend')}
            </h2>
            <p className="text-xs text-muted-foreground mt-1">{t('matchScoreTrendDesc')}</p>
          </div>
          <div className="flex-1 min-h-[250px]">
            {lineChartData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={lineChartData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                  <XAxis dataKey="name" axisLine={false} tickLine={false} tick={{ fontSize: 12, fill: '#6b7280' }} dy={10} />
                  <YAxis axisLine={false} tickLine={false} tick={{ fontSize: 12, fill: '#6b7280' }} domain={[0, 100]} />
                  <RechartsTooltip 
                    contentStyle={{ borderRadius: '12px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                    labelStyle={{ fontWeight: 'bold', color: '#111827', marginBottom: '4px' }}
                  />
                  <Line type="monotone" dataKey="score" stroke="#3b82f6" strokeWidth={3} dot={{ r: 4, fill: '#3b82f6', strokeWidth: 2, stroke: '#fff' }} activeDot={{ r: 6 }} />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-full flex items-center justify-center text-sm text-muted-foreground">{t('notEnoughData')}</div>
            )}
          </div>
        </div>

        {/* Skill Readiness (Radar Chart) */}
        <div className="bg-card border border-border rounded-2xl p-6 shadow-sm flex flex-col">
          <div className="mb-2">
            <h2 className="text-lg font-bold flex items-center gap-2">
              <Sparkles size={20} className="text-indigo-500" />
              {t('skillReadiness')}
            </h2>
            <p className="text-xs text-muted-foreground mt-1">{t('skillReadinessDesc')}</p>
          </div>
          {radarChartData.length >= 3 ? (
            <div className="flex-1 min-h-[250px] -mt-4">
              <ResponsiveContainer width="100%" height="100%">
                <RadarChart cx="50%" cy="50%" outerRadius="70%" data={radarChartData}>
                  <PolarGrid stroke="#e5e7eb" />
                  <PolarAngleAxis dataKey="subject" tick={{ fontSize: 10, fill: '#4b5563', fontWeight: 500 }} />
                  <Radar name="Target Level" dataKey="Target" stroke="#d1d5db" strokeWidth={2} fill="#e5e7eb" fillOpacity={0.3} />
                  <Radar name="Current Level" dataKey="Current" stroke="#6366f1" strokeWidth={2} fill="#6366f1" fillOpacity={0.5} />
                  <RechartsTooltip 
                    contentStyle={{ borderRadius: '12px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                  />
                </RadarChart>
              </ResponsiveContainer>
            </div>
          ) : radarChartData.length > 0 ? (
            <div className="flex-1 min-h-[250px] flex flex-col justify-between py-2 space-y-6">
              <div className="space-y-5 my-auto">
                {radarChartData.map((item, idx) => {
                  const current = Math.min(Math.max(0, Number(item.Current) || 0), item.fullMark || 7);
                  const target = Math.min(Math.max(0, Number(item.Target) || 0), item.fullMark || 7);
                  const maxLevel = item.fullMark || 7;
                  
                  return (
                    <div key={idx} className="space-y-2">
                      <div className="flex items-center justify-between text-sm">
                        <span className="font-bold text-foreground truncate max-w-[70%]" title={item.subject}>{item.subject}</span>
                        <div className="flex items-center gap-2 text-xs font-semibold">
                          <span className="text-indigo-600 bg-indigo-50 dark:bg-indigo-950/60 dark:text-indigo-400 px-2 py-0.5 rounded-md">
                            {t('currentLevel', { level: current })}
                          </span>
                          <span className="text-muted-foreground bg-muted px-2 py-0.5 rounded-md">
                            {t('targetLevel', { level: target })}
                          </span>
                        </div>
                      </div>
                      <div className="grid grid-cols-7 gap-1.5 pt-1">
                        {Array.from({ length: maxLevel }).map((_, stepIdx) => {
                          const stepLevel = stepIdx + 1;
                          const isCurrent = stepLevel <= current;
                          const isTargetGap = stepLevel > current && stepLevel <= target;
                          
                          return (
                            <div 
                              key={stepIdx} 
                              className={`h-3 rounded-sm transition-all duration-300 ${
                                isCurrent 
                                  ? "bg-indigo-500" 
                                  : isTargetGap 
                                  ? "bg-indigo-100 border border-indigo-300 dark:bg-indigo-950 dark:border-indigo-700" 
                                  : "bg-muted/40"
                              }`}
                              title={`Level ${stepLevel}${isCurrent ? ' (Current)' : isTargetGap ? ' (Target Gap)' : ''}`}
                            />
                          );
                        })}
                      </div>
                    </div>
                  );
                })}
              </div>

              <div className="flex items-center justify-center gap-5 pt-3 text-[11px] font-medium text-muted-foreground border-t border-border/40">
                <div className="flex items-center gap-1.5">
                  <span className="w-2.5 h-2.5 rounded-sm bg-indigo-500 inline-block"></span>
                  <span>{t('currentLevelLabel')}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <span className="w-2.5 h-2.5 rounded-sm bg-indigo-100 border border-indigo-300 dark:bg-indigo-950 dark:border-indigo-700 inline-block"></span>
                  <span>{t('targetGapLabel')}</span>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex-1 min-h-[250px] flex flex-col items-center justify-center text-center text-sm text-muted-foreground p-4">
              <Sparkles size={24} className="text-muted-foreground/50 mb-2" />
              <p>{t('noSkillGap')}</p>
            </div>
          )}
        </div>
      </div>

      {/* Row 3: Data Tables & Actionable Lists */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        
        {/* Recent Matches */}
        <div className="bg-card border border-border rounded-2xl p-6 shadow-sm flex flex-col">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-lg font-bold flex items-center gap-2">
              <FileSearch size={20} className="text-blue-500" />
              {t('recentMatches')}
            </h2>
            <Link href="/candidate/cv-matching" className="text-xs font-semibold text-blue-600 hover:text-blue-700 flex items-center">
              {t('viewAll')} <ChevronRight size={14} />
            </Link>
          </div>
          
          <div className="flex-1">
            {isMatchLoading ? <div className="text-sm text-muted-foreground animate-pulse">{t('loadingMatches')}</div> : null}
            {!isMatchLoading && matchHistory.length === 0 ? (
              <div className="bg-muted/30 rounded-xl p-6 text-center h-full flex flex-col items-center justify-center border border-border/50">
                <AlertCircle size={28} className="text-blue-400 mb-2" />
                <p className="text-sm text-muted-foreground mb-3">{t('noMatches')}</p>
              </div>
            ) : (
              <div className="space-y-3">
                {matchHistory.slice(0, 4).map((item) => (
                  <Link href={`/candidate/cv-matching/${item.jobId}/optimize`} key={item.jobId} className="block">
                    <div className="bg-background border border-border/50 hover:border-blue-300 hover:shadow-sm transition-all rounded-xl p-3 px-4 group flex items-center justify-between gap-4">
                      <div className="flex-1 min-w-0">
                        <h3 className="font-bold text-sm text-foreground truncate group-hover:text-blue-600 transition-colors">{item.jdTitle || t('untitledJob')}</h3>
                        <p className="text-xs text-muted-foreground truncate flex items-center gap-1 mt-0.5">
                          <Clock size={10} /> {item.updatedAt ? new Date(item.updatedAt).toLocaleDateString() : 'N/A'}
                        </p>
                      </div>
                      <div className="flex items-center gap-3 flex-shrink-0">
                        <div className={`px-2 py-1 rounded-md text-[10px] font-bold uppercase tracking-wider ${item.status === 'Optimized' ? 'bg-indigo-50 text-indigo-600' : 'bg-gray-100 text-gray-600'}`}>
                          {item.status || t('matched')}
                        </div>
                        <span className={`text-base font-extrabold w-10 text-right ${item.matchScore && item.matchScore * 100 >= 70 ? 'text-emerald-500' : 'text-amber-500'}`}>
                          {item.matchScore ? `${Math.round(item.matchScore * 100)}%` : 'N/A'}
                        </span>
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Pending Learning Tasks */}
        <div className="bg-card border border-border rounded-2xl p-6 shadow-sm flex flex-col">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-lg font-bold flex items-center gap-2">
              <BookOpen size={20} className="text-teal-500" />
              {t('pendingTasks')}
            </h2>
            <Link href="/candidate/learning-path" className="text-xs font-semibold text-teal-600 hover:text-teal-700 flex items-center">
              {t('continueLearning')} <ChevronRight size={14} />
            </Link>
          </div>
          
          <div className="flex-1">
            {isPathsLoading ? <div className="text-sm text-muted-foreground animate-pulse">{t('loadingTasks')}</div> : null}
            {!isPathsLoading && pendingTasks.length === 0 ? (
              <div className="bg-muted/30 rounded-xl p-6 text-center h-full flex flex-col items-center justify-center border border-border/50">
                <CheckCircle2 size={28} className="text-teal-400 mb-2" />
                <p className="text-sm text-muted-foreground">{t('allCaughtUp')}</p>
              </div>
            ) : (
              <div className="space-y-3">
                {pendingTasks.map((task, idx) => (
                  <div key={idx} className="bg-background border border-border/50 hover:border-teal-300 hover:shadow-sm rounded-xl p-3 px-4 flex items-start gap-3 transition-colors">
                    <Circle size={16} className="text-muted-foreground mt-0.5 flex-shrink-0" />
                    <div className="flex-1 min-w-0">
                      <h3 className="font-bold text-sm text-foreground leading-snug">{task.title}</h3>
                      <p className="text-[11px] font-medium text-teal-600 uppercase tracking-wider mt-1 truncate">{t('module')}: {task.moduleTitle}</p>
                    </div>
                    <div className="flex-shrink-0">
                      <Link href={`/candidate/learning-path/${activePath?.id}`}>
                         <div className="w-8 h-8 rounded-full bg-teal-50 flex items-center justify-center hover:bg-teal-100 transition-colors">
                            <PlayCircle size={16} className="text-teal-600" />
                         </div>
                      </Link>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

      </div>
    </div>
  )
}
