'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useGenerateLearningPath, useExtractFromCvJd, useExtractFromInterview, useTargetRoles, usePreviewContext } from '@/hooks/useLearningPath';
import { useGetMatchHistory } from '@/hooks/useCvMatch';
import { useGetInterviewSessions } from '@/hooks/useInterview';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertTitle, AlertDescription } from '@/components/ui/alert';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Loader2, Sparkles, Info, ArrowLeft, CheckCircle2, Coins, Zap } from 'lucide-react';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useWalletBalance } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';
import { toast } from 'sonner';
import { Badge } from '@/components/ui/badge';
import { getScorePercent } from '@/lib/matching-score';

const SFIA_LEVEL_HINTS: Record<number, { title: string; essence: string }> = {
  1: { title: "Follow", essence: "Works under close supervision. Uses little discretion." },
  2: { title: "Assist", essence: "Works under routine supervision. Uses minor discretion." },
  3: { title: "Apply", essence: "Works under general supervision. Uses discretion in identifying and responding to complex issues." },
  4: { title: "Enable", essence: "Works under general direction within a clear framework of accountability. Exercises substantial personal responsibility." },
  5: { title: "Ensure, advise", essence: "Works under broad direction. Is fully accountable for own technical work and/or project/supervisory responsibilities." },
  6: { title: "Initiate, influence", essence: "Has defined authority and responsibility for a significant area of work, including technical, financial and quality aspects." },
  7: { title: "Set strategy, inspire, mobilise", essence: "At the highest organizational level, has authority over all aspects of a significant area of work." }
};
import AILoadingState from '../components/AILoadingState';

export default function NewLearningPathPage() {
  const router = useRouter();
  
  const generateMutation = useGenerateLearningPath();
  const extractFromCvJdMutation = useExtractFromCvJd();
  const extractFromInterviewMutation = useExtractFromInterview();

  const { data: walletRes } = useWalletBalance();
  const { data: coinConfigRes } = usePublicCoinConfig();

  const balance = walletRes?.data?.balance ?? 0;
  const activeSubName = walletRes?.data?.activeSubscriptionName;
  const learningPathCost = coinConfigRes?.data?.featureCosts?.learningPath ?? 500;
  const learningPathLimit = walletRes?.data?.learningPathLimit ?? (walletRes?.data?.learningPathSlotLimit ?? 0);
  const learningPathUsed = walletRes?.data?.learningPathUsed ?? 0;

  const isSubUnlimited = activeSubName && (learningPathLimit === -1 || learningPathLimit >= 999);
  const subRemaining = activeSubName && !isSubUnlimited ? Math.max(0, (learningPathLimit || 0) - learningPathUsed) : 0;
  const hasActiveSub = !!activeSubName && (isSubUnlimited || subRemaining > 0);

  const { data: matchHistoryData } = useGetMatchHistory(1, 50);
  const { data: interviewSessionsData } = useGetInterviewSessions();

  const { data: targetRolesData, isLoading: isTargetRolesLoading } = useTargetRoles();

  const [targetRoleTemplateId, setTargetRoleTemplateId] = useState('');
  const [currentSkills, setCurrentSkills] = useState<{ skillCode: string; currentLevel: number | null }[]>([]);
  
  const [roleSearchQuery, setRoleSearchQuery] = useState('');
  const [roleLevelFilter, setRoleLevelFilter] = useState('All');
  
  const [personalContext, setPersonalContext] = useState('');

  const [selectedMatchScoreId, setSelectedMatchScoreId] = useState<string>('');
  const [selectedSessionId, setSelectedSessionId] = useState<string>('');
  
  const [customProfile, setCustomProfile] = useState<any>(null);

  const { data: cvJdPreview } = usePreviewContext('cv-jd', selectedMatchScoreId);
  const { data: interviewPreview } = usePreviewContext('interview', selectedSessionId);

  const [confirmedRoleId, setConfirmedRoleId] = useState('');

  const selectedRoleTemplate = targetRolesData?.data?.find(r => r.id === targetRoleTemplateId);
  const assessingRoleTemplate = targetRolesData?.data?.find(r => r.id === confirmedRoleId);

  const filteredRoles = (targetRolesData?.data || []).filter(role => {

    const matchesSearch = role.roleName.toLowerCase().includes(roleSearchQuery.toLowerCase());
    if (roleLevelFilter === 'All') return matchesSearch;
    return matchesSearch && role.roleName.toLowerCase().includes(roleLevelFilter.toLowerCase());
  });

  const handleSkillLevelChange = (skillCode: string, level: number) => {
    setCurrentSkills(prev => {
      if (prev.some(s => s.skillCode === skillCode)) {
        return prev.map(s => s.skillCode === skillCode ? { ...s, currentLevel: level } : s);
      }
      return [...prev, { skillCode, currentLevel: level }];
    });
  };

  const handleGenerate = () => {
    if (!hasActiveSub && balance < learningPathCost) {
      toast.error(
        <div className="flex flex-col gap-2">
          <div className="font-semibold text-rose-600">Số dư ví không đủ!</div>
          <div className="text-sm">Tính năng tạo Lộ trình học thuật (Learning Path) cần <b>{learningPathCost.toLocaleString()} Coin</b>. Bạn hiện có <b>{balance.toLocaleString()} Coin</b>.</div>
          <a href="/candidate/top-up" className="inline-block mt-1 text-xs font-bold bg-amber-500 hover:bg-amber-600 text-white py-1 px-3 rounded text-center transition">
            Nạp ngay
          </a>
        </div>,
        { duration: 5000 }
      );
      return;
    }

    if (customProfile) {
      generateMutation.mutate({
        customTargetRoleName: customProfile.customRoleName,
        customTargetSkills: customProfile.skills.map((s: any) => ({
          skillCode: s.skillCode,
          targetLevel: s.targetLevel,
          currentLevel: s.currentLevel
        })),
        currentSkills: [],
        personalContext: personalContext.trim() !== '' ? personalContext : undefined,
      });
    } else {
      generateMutation.mutate({
        targetRoleTemplateId: confirmedRoleId,
        currentSkills: currentSkills.filter(s => s.currentLevel !== null) as { skillCode: string, currentLevel: number }[],
        personalContext: personalContext.trim() !== '' ? personalContext : undefined,
      });
    }
  };

  const handleRoleChange = (roleId: string) => {
    setTargetRoleTemplateId(roleId);
    setCustomProfile(null);
  };

  const handleExtractCvJd = () => {
    if (!selectedMatchScoreId) return;
    extractFromCvJdMutation.mutate(selectedMatchScoreId, {
      onSuccess: (res) => {
        if (res.data) setCustomProfile(res.data);
      }
    });
  };

  const handleExtractInterview = () => {
    if (!selectedSessionId) return;
    extractFromInterviewMutation.mutate(selectedSessionId, {
      onSuccess: (res) => {
        if (res.data) setCustomProfile(res.data);
      }
    });
  };

  const handleCustomSkillLevelChange = (skillCode: string, newLevel: number) => {
    setCustomProfile((prev: any) => {
      if (!prev) return prev;
      return {
        ...prev,
        skills: prev.skills.map((s: any) => s.skillCode === skillCode ? { ...s, currentLevel: newLevel } : s)
      };
    });
  };

  const allAssessed = assessingRoleTemplate ? assessingRoleTemplate.requiredSkills.every(reqSkill => {
    const skillState = currentSkills.find(s => s.skillCode === reqSkill.skillCode);
    return skillState && skillState.currentLevel !== null;
  }) : false;

  const totalGaps = assessingRoleTemplate ? assessingRoleTemplate.requiredSkills.reduce((acc, reqSkill) => {
    const skillState = currentSkills.find(s => s.skillCode === reqSkill.skillCode);
    if (skillState && skillState.currentLevel !== null) {
      const gap = reqSkill.targetLevel - skillState.currentLevel;
      return acc + (gap > 0 ? gap : 0);
    }
    return acc;
  }, 0) : 0;

  const isGenerateDisabled = () => {
    if (generateMutation.isPending) return true;
    if (customProfile) return false;
    
    if (!confirmedRoleId) return true;
    if (assessingRoleTemplate) {
      if (!allAssessed) return true;
      if (totalGaps === 0) return true;
      return false;
    }
    return true;
  };

  const isAnyError = generateMutation.isError || extractFromCvJdMutation.isError || extractFromInterviewMutation.isError;
  const errorObj = generateMutation.error || extractFromCvJdMutation.error || extractFromInterviewMutation.error;
  let errorMessage = 'Failed to process request. Please try again.';
  if (errorObj) {
    const axiosError = errorObj as any;
    if (axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT') {
      errorMessage = 'The AI is taking too long to respond. Please try again.';
    } else if (axiosError.response?.data?.message) {
      errorMessage = axiosError.response.data.message;
    }
  }

  useEffect(() => {
    if (generateMutation.isSuccess) {
      router.push('/candidate/learning-path');
    }
  }, [generateMutation.isSuccess, router]);

  if (generateMutation.isPending) {
    return (
      <div className="container mx-auto py-8">
        <AILoadingState />
      </div>
    );
  }

  return (
    <div className="w-full pb-8 space-y-8">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" size="icon" onClick={() => router.back()}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Create Learning Path</h1>
          <p className="text-muted-foreground mt-2">
            Configure your target role and assess your skills to generate a personalized journey.
          </p>
        </div>
      </div>

      {/* Feature Cost & Wallet Balance Banner */}
      <div className="p-4 rounded-xl bg-gradient-to-r from-purple-500/10 via-amber-500/10 to-transparent border border-purple-500/20 shadow-sm flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-500/20 text-amber-500 shadow-inner">
            {hasActiveSub ? <Zap className="h-5 w-5 text-purple-600 dark:text-purple-400 fill-purple-600/20" /> : <Coins className="h-5 w-5 text-amber-500 fill-amber-500/20" />}
          </div>
          <div className="flex flex-col">
            <div className="flex items-center gap-1.5 flex-wrap">
              <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Phí dịch vụ:</span>
              {hasActiveSub ? (
                <>
                  <Badge className="bg-purple-600 text-white text-[10px] font-bold px-2 py-0.5 shadow-sm">
                    FREE ({activeSubName})
                  </Badge>
                  <span className="text-xs font-semibold text-purple-600 dark:text-purple-400">
                    {isSubUnlimited ? "• Vô hạn lượt" : `• Còn ${subRemaining}/${learningPathLimit} lượt`}
                  </span>
                </>
              ) : (
                <span className="text-sm font-black text-amber-600 dark:text-amber-400">
                  {learningPathCost.toLocaleString()} Coin / Lượt
                </span>
              )}
            </div>
            {!!activeSubName && !hasActiveSub && (
              <span className="text-xs text-rose-500 mt-0.5 font-medium">
                Gói {activeSubName} đã hết lượt miễn phí. Chuyển sang trừ Coin:
              </span>
            )}
            {!hasActiveSub && (
              <span className="text-xs text-muted-foreground mt-0.5 font-medium">
                Số dư hiện tại: <strong className={balance < learningPathCost ? "text-rose-500 font-bold" : "text-emerald-600 font-bold"}>{balance.toLocaleString()} Coin</strong>
              </span>
            )}
          </div>
        </div>

        {!hasActiveSub && balance < learningPathCost && (
          <Button
            type="button"
            size="sm"
            onClick={() => router.push('/candidate/top-up')}
            className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-3 py-1.5 text-xs rounded-lg shadow-md hover:shadow-amber-500/25 transition-all shrink-0"
          >
            Nạp Coin
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Learning Path Configuration</CardTitle>
          <CardDescription>Fill in your details manually or use AI to extract them from your past activities.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-8">
          
          {/* Optional Autofill Section */}
          <div className="bg-muted/30 p-5 rounded-lg border border-border/50">
            <h3 className="font-semibold text-sm mb-4 flex items-center"><Sparkles className="h-4 w-4 mr-2 text-primary" /> AI Autofill (Optional)</h3>
            <div className="grid md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <Label className="text-xs text-muted-foreground">From CV-JD Match Result</Label>
                <div className="flex gap-2">
                  <Select value={selectedMatchScoreId} onValueChange={(v) => setSelectedMatchScoreId(v || '')}>
                    <SelectTrigger className="flex-1">
                      <SelectValue placeholder="Select a match result...">
                        {selectedMatchScoreId && matchHistoryData?.data?.items?.find(m => m.jobId === selectedMatchScoreId) 
                          ? (() => {
                              const match = matchHistoryData.data.items.find(m => m.jobId === selectedMatchScoreId)!;
                              return `${match.jdTitle || 'Unknown Job'} - ${getScorePercent(match).toFixed(1)}/100`;
                            })()
                          : null}
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      {matchHistoryData?.data?.items?.filter(m => m.status === 'Completed').map(match => (
                        <SelectItem key={match.jobId} value={match.jobId}>
                          {match.jdTitle} - {getScorePercent(match).toFixed(1)}/100
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Button 
                    variant="secondary" 
                    onClick={handleExtractCvJd} 
                    disabled={!selectedMatchScoreId || extractFromCvJdMutation.isPending}
                  >
                    {extractFromCvJdMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Extract'}
                  </Button>
                </div>
                {cvJdPreview?.data && (
                  <details className="mt-2 text-xs border rounded-md bg-muted/50 overflow-hidden">
                    <summary className="p-2 cursor-pointer hover:bg-muted font-medium text-muted-foreground flex items-center">
                      🐛 Debug: View AI Context Data
                    </summary>
                    <div className="p-3 border-t bg-background">
                      <pre className="whitespace-pre-wrap font-mono text-[10px] leading-tight text-foreground/80">
                        {cvJdPreview.data.contextPreview}
                      </pre>
                    </div>
                  </details>
                )}
              </div>

              <div className="space-y-2">
                <Label className="text-xs text-muted-foreground">From Mock Interview Session</Label>
                <div className="flex gap-2">
                  <Select value={selectedSessionId} onValueChange={(v) => setSelectedSessionId(v || '')}>
                    <SelectTrigger className="flex-1">
                      <SelectValue placeholder="Select an interview...">
                        {selectedSessionId && interviewSessionsData?.data?.find(s => s.id === selectedSessionId)
                          ? (() => {
                              const session = interviewSessionsData.data.find(s => s.id === selectedSessionId)!;
                              return `${session.jobTitle || 'General'} (${session.difficultyLevel})`;
                            })()
                          : null}
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      {interviewSessionsData?.data?.filter(s => s.status === 'COMPLETED').map(session => (
                        <SelectItem key={session.id} value={session.id}>
                          {session.jobTitle || 'General'} ({session.difficultyLevel})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Button 
                    variant="secondary" 
                    onClick={handleExtractInterview} 
                    disabled={!selectedSessionId || extractFromInterviewMutation.isPending}
                  >
                    {extractFromInterviewMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Extract'}
                  </Button>
                </div>
                {interviewPreview?.data && (
                  <details className="mt-2 text-xs border rounded-md bg-muted/50 overflow-hidden">
                    <summary className="p-2 cursor-pointer hover:bg-muted font-medium text-muted-foreground flex items-center">
                      🐛 Debug: View AI Context Data
                    </summary>
                    <div className="p-3 border-t bg-background">
                      <pre className="whitespace-pre-wrap font-mono text-[10px] leading-tight text-foreground/80">
                        {interviewPreview.data.contextPreview}
                      </pre>
                    </div>
                  </details>
                )}
              </div>
            </div>
          </div>

          <div className="space-y-8">
            {customProfile ? (
              <div className="space-y-4 animate-in fade-in slide-in-from-bottom-4 duration-500">
                <div className="bg-primary/5 p-6 rounded-xl border border-primary/20 space-y-2">
                  <h3 className="font-semibold text-xl flex items-center text-primary">
                    <Sparkles className="mr-2 h-5 w-5" /> AI Custom Role: {customProfile.customRoleName}
                  </h3>
                  <p className="text-sm text-muted-foreground">{customProfile.customRoleDescription}</p>
                </div>
                
                <h3 className="font-semibold text-lg flex items-center border-t pt-6">
                  <CheckCircle2 className="mr-2 h-5 w-5 text-primary" /> Target Skills & Gap Analysis
                </h3>
                <p className="text-sm text-muted-foreground mb-4">
                  The AI has mapped your gaps to specific SFIA skills. Review and adjust your current proficiency level if necessary.
                </p>

                <TooltipProvider delay={200}>
                  <div className="space-y-6">
                    {customProfile.skills.map((skill: any) => {
                      const currentLvlState = skill.currentLevel;
                      const isAssessed = currentLvlState !== undefined && currentLvlState !== null;
                      
                      return (
                        <div key={skill.skillCode} className={`space-y-4 border p-5 rounded-xl transition-colors ${isAssessed ? 'border-primary/20 bg-primary/5' : 'border-border'}`}>
                          <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-4 mb-2">
                            <div className="flex-1">
                              <h4 className="font-medium text-foreground flex items-center gap-2">
                                <span className="text-xs text-muted-foreground font-mono bg-muted px-1.5 py-0.5 rounded">{skill.skillCode}</span>
                              </h4>
                              <p className="text-sm text-muted-foreground mt-1" title={skill.justification}>
                                {skill.justification}
                              </p>
                            </div>
                            <div className="text-left sm:text-right shrink-0">
                              <span className="text-sm font-semibold bg-primary/10 text-primary px-2 py-1 rounded">Target Level: {skill.targetLevel}</span>
                            </div>
                          </div>
                          
                          <div className="flex flex-wrap gap-2 pt-2">
                            <button
                              type="button"
                              onClick={() => handleCustomSkillLevelChange(skill.skillCode, 0)}
                              className={`px-3 py-2 text-xs font-medium rounded-lg border transition-all ${currentLvlState === 0 ? 'bg-destructive/10 text-destructive border-destructive/30' : 'bg-background hover:bg-muted text-muted-foreground border-border'}`}
                            >
                              0 - No experience
                            </button>
                            
                            {[1, 2, 3, 4, 5, 6, 7].map(lvl => {
                              const isSelected = currentLvlState === lvl;
                              
                              return (
                                <Tooltip key={lvl}>
                                  <TooltipTrigger 
                                    type="button"
                                    onClick={() => handleCustomSkillLevelChange(skill.skillCode, lvl)}
                                    className={`
                                      relative px-4 py-2 text-sm font-medium rounded-lg border transition-all
                                      ${isSelected 
                                        ? 'bg-primary text-primary-foreground border-primary shadow-sm' 
                                        : 'bg-background hover:bg-muted text-foreground border-border'}
                                    `}
                                  >
                                    {lvl}
                                  </TooltipTrigger>
                                  <TooltipContent side="top" className="max-w-[250px] p-3 shadow-lg">
                                    <p className="font-semibold text-[13px] text-primary-foreground mb-1">Level {lvl}: {SFIA_LEVEL_HINTS[lvl]?.title}</p>
                                    <p className="text-xs text-primary-foreground/90 leading-relaxed">
                                      {SFIA_LEVEL_HINTS[lvl]?.essence}
                                    </p>
                                  </TooltipContent>
                                </Tooltip>
                              );
                            })}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </TooltipProvider>

                {/* Section 3: Personal Context */}
                <div className="mt-8 space-y-4 pt-6 border-t">
                  <h3 className="font-semibold text-lg flex items-center"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Prior Knowledge & Context (Optional)</h3>
                  <p className="text-sm text-muted-foreground">
                    Describe any other relevant context.
                  </p>
                  <Textarea 
                    placeholder="E.g., I have 1 year of experience with Python..."
                    value={personalContext}
                    onChange={(e) => setPersonalContext(e.target.value)}
                    className="min-h-[100px] resize-y"
                  />
                </div>
              </div>
            ) : (
            <>
            {/* Section 1: Core Information */}
            <div className="space-y-4">
              <h3 className="font-semibold text-lg flex items-center"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Target Role</h3>
              <div className="space-y-3 bg-muted/20 p-4 rounded-xl border border-border/50">
                <div className="grid sm:grid-cols-2 gap-4">
                  <div className="space-y-1.5">
                    <Label className="text-xs text-muted-foreground">Search Role</Label>
                    <Input 
                      placeholder="e.g. Developer, Data..." 
                      value={roleSearchQuery}
                      onChange={(e) => setRoleSearchQuery(e.target.value)}
                      className="bg-background"
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-xs text-muted-foreground">Filter by Seniority</Label>
                    <Select value={roleLevelFilter} onValueChange={(val) => setRoleLevelFilter(val || '')}>
                      <SelectTrigger className="bg-background">
                        <SelectValue placeholder="All Levels" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="All">All Levels</SelectItem>
                        <SelectItem value="Junior">Junior</SelectItem>
                        <SelectItem value="Mid">Mid-level</SelectItem>
                        <SelectItem value="Senior">Senior</SelectItem>
                        <SelectItem value="Lead">Lead</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-1.5 pt-2">
                  <Label htmlFor="targetRole">Select Target Role Template <span className="text-red-500">*</span></Label>
                  <Select value={targetRoleTemplateId} onValueChange={(v) => handleRoleChange(v || '')}>
                    <SelectTrigger id="targetRole" className="bg-background font-medium">
                      <SelectValue placeholder={isTargetRolesLoading ? "Loading templates..." : "Select a role..."}>
                        {selectedRoleTemplate?.roleName}
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      {filteredRoles.map(role => (
                        <SelectItem key={role.id} value={role.id}>{role.roleName}</SelectItem>
                      ))}
                      {filteredRoles.length === 0 && (
                        <div className="p-3 text-center text-sm text-muted-foreground">No roles match your search.</div>
                      )}
                    </SelectContent>
                  </Select>
                </div>

                {selectedRoleTemplate && (
                  <p className="text-xs text-muted-foreground pt-1">{selectedRoleTemplate.description}</p>
                )}
                
                <div className="flex justify-end pt-4">
                  <Button 
                    type="button"
                    onClick={() => setConfirmedRoleId(targetRoleTemplateId)}
                    disabled={!targetRoleTemplateId || targetRoleTemplateId === confirmedRoleId}
                  >
                    {targetRoleTemplateId === confirmedRoleId ? (
                      <><CheckCircle2 className="mr-2 h-4 w-4" /> Role Confirmed</>
                    ) : (
                      'Confirm Target Role'
                    )}
                  </Button>
                </div>
              </div>
            </div>

            {/* Section 2: Technical Information */}
            {assessingRoleTemplate && (
              <div className="space-y-4 animate-in fade-in slide-in-from-bottom-4 duration-500">
                <h3 className="font-semibold text-lg flex items-center border-t pt-6"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Self-Assess SFIA Skills for {assessingRoleTemplate.roleName}</h3>
                <p className="text-sm text-muted-foreground mb-4">
                  Please verify or assess your current proficiency level for the core skills required by this role (0 = No experience, 1-7 = SFIA Levels).{' '}
                  <a href="https://sfia-online.org/en/sfia-9/responsibilities" target="_blank" rel="noopener noreferrer" className="text-primary hover:underline inline-flex items-center">
                    Learn more about SFIA levels <Info className="w-3 h-3 ml-1" />
                  </a>
                </p>
                
                <TooltipProvider delay={200}>
                  <div className="space-y-6">
                    {assessingRoleTemplate.requiredSkills.map(skill => {
                    const currentLvlState = currentSkills.find(s => s.skillCode === skill.skillCode)?.currentLevel;
                    const isAssessed = currentLvlState !== undefined && currentLvlState !== null;
                    
                    return (
                      <div key={skill.skillCode} className={`space-y-4 border p-5 rounded-xl transition-colors ${isAssessed ? 'border-primary/20 bg-primary/5' : 'border-border'}`}>
                        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-2 mb-2">
                          <div>
                            <h4 className="font-medium text-foreground flex items-center gap-2">
                              {skill.skillName} 
                              <span className="text-xs text-muted-foreground font-mono bg-muted px-1.5 py-0.5 rounded">{skill.skillCode}</span>
                            </h4>
                            {skill.description && (
                              <p className="text-sm text-muted-foreground mt-1 line-clamp-2" title={skill.description}>
                                {skill.description}
                              </p>
                            )}
                          </div>
                          <div className="text-left sm:text-right">
                            {isAssessed ? (
                              <span className="text-sm font-semibold text-primary">Assessed Level: {currentLvlState}</span>
                            ) : (
                              <span className="text-sm font-medium text-destructive flex items-center gap-1.5">
                                <Info size={14} /> Action Required
                              </span>
                            )}
                          </div>
                        </div>
                        
                        <div className="flex flex-wrap gap-2 pt-2">
                          <button
                            type="button"
                            onClick={() => handleSkillLevelChange(skill.skillCode, 0)}
                            className={`px-3 py-2 text-xs font-medium rounded-lg border transition-all ${currentLvlState === 0 ? 'bg-destructive/10 text-destructive border-destructive/30' : 'bg-background hover:bg-muted text-muted-foreground border-border'}`}
                          >
                            0 - No experience
                          </button>
                          
                          {[1, 2, 3, 4, 5, 6, 7].map(lvl => {
                            const cleanLevels = skill.availableLevels ? skill.availableLevels.replace(/"/g, '') : '';
                            const validLevels = cleanLevels ? cleanLevels.split(',').map(s => Number(s.trim())) : [1, 2, 3, 4, 5, 6, 7];
                            const isValid = validLevels.includes(lvl);

                            const isSelected = currentLvlState === lvl;
                            
                            return (
                                <Tooltip key={lvl}>
                                  <TooltipTrigger 
                                    type="button"
                                    onClick={() => handleSkillLevelChange(skill.skillCode, lvl)}
                                    className={`
                                      relative px-4 py-2 text-sm font-medium rounded-lg border transition-all
                                      ${isSelected 
                                        ? 'bg-primary text-primary-foreground border-primary shadow-sm' 
                                        : !isValid 
                                          ? 'bg-muted/30 text-muted-foreground/60 border-dashed hover:bg-muted/50' 
                                          : 'bg-background hover:bg-muted text-foreground border-border'}
                                    `}
                                  >
                                    {lvl}
                                  </TooltipTrigger>
                                  <TooltipContent side="top" className="max-w-[250px] p-3 shadow-lg">
                                    {!isValid ? (
                                      <p className="text-xs text-primary-foreground/90 leading-relaxed text-center">
                                        This skill is not officially defined at Level {lvl} in SFIA. However, you can still select it to represent your approximate experience level.
                                      </p>
                                    ) : (
                                      <>
                                        <p className="font-semibold text-[13px] text-primary-foreground mb-1">Level {lvl}: {SFIA_LEVEL_HINTS[lvl]?.title}</p>
                                        <p className="text-xs text-primary-foreground/90 leading-relaxed">
                                          {SFIA_LEVEL_HINTS[lvl]?.essence}
                                        </p>
                                      </>
                                    )}
                                  </TooltipContent>
                                </Tooltip>
                              );
                          })}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </TooltipProvider>
              
              {/* Section 3: Personal Context */}
              <div className="mt-8 space-y-4 pt-6 border-t">
                <h3 className="font-semibold text-lg flex items-center"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Prior Knowledge & Context (Optional)</h3>
                <p className="text-sm text-muted-foreground">
                  If you are between SFIA levels or already know some specific tools (e.g. &quot;I know basic React but not Redux&quot;), describe it here. The AI will use this to skip redundant basics and tailor your learning path.
                </p>
                <Textarea 
                  placeholder="E.g., I have 1 year of experience with Python but I'm completely new to Django..."
                  value={personalContext}
                  onChange={(e) => setPersonalContext(e.target.value)}
                  className="min-h-[100px] resize-y"
                />
              </div>

              </div>
            )}
            </>
            )}
          </div>

          {!customProfile && allAssessed && totalGaps === 0 && (
            <Alert variant="default" className="mt-6 border-green-500 bg-green-50/50">
              <Sparkles className="h-4 w-4 text-green-600" />
              <AlertTitle className="text-green-700">Congratulations!</AlertTitle>
              <AlertDescription className="text-green-700/90">
                You already meet or exceed all the required skills for this role! There are no skill gaps to bridge. We recommend choosing a higher-level role (e.g., Mid-level or Senior) to discover new areas for career growth.
              </AlertDescription>
            </Alert>
          )}

          <Button
            className={`w-full mt-6 text-base font-semibold transition-all ${
              !hasActiveSub && balance < learningPathCost
                ? "bg-rose-500 hover:bg-rose-600 text-white opacity-90 cursor-not-allowed"
                : ""
            }`}
            disabled={isGenerateDisabled() || (!hasActiveSub && balance < learningPathCost)}
            onClick={handleGenerate}
          >
            {!hasActiveSub && balance < learningPathCost ? (
              `Không đủ Coin (${balance.toLocaleString()}/${learningPathCost.toLocaleString()})`
            ) : (
              `Generate Learning Path ${hasActiveSub ? "(Free)" : `(-${learningPathCost.toLocaleString()} Coin)`}`
            )}
          </Button>

          {isAnyError && (
            <p className="text-sm text-destructive mt-2 text-center">
              {errorMessage}
            </p>
          )}

          {generateMutation.isSuccess && (
            <p className="text-sm text-green-600 mt-2 text-center">
              Learning path successfully generated! Redirecting...
            </p>
          )}

          {/* Debug Section for AI Extracted Data */}
          {customProfile && (
            <div className="mt-8 border-t pt-6">
              <details className="group cursor-pointer">
                <summary className="text-sm font-medium text-muted-foreground hover:text-primary transition-colors flex items-center">
                  <span>View AI Extracted Raw Data (Debug)</span>
                </summary>
                <div className="mt-4 bg-muted/30 p-4 rounded-lg border overflow-auto text-xs font-mono max-h-96">
                  <pre>{JSON.stringify(customProfile, null, 2)}</pre>
                </div>
              </details>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
