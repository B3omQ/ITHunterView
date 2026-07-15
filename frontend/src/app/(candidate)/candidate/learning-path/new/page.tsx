'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useGenerateLearningPath, useExtractFromCvJd, useExtractFromInterview, useTargetRoles } from '@/hooks/useLearningPath';
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
import { Loader2, Sparkles, Info, ArrowLeft } from 'lucide-react';
import AILoadingState from '../components/AILoadingState';

export default function NewLearningPathPage() {
  const router = useRouter();
  
  const generateMutation = useGenerateLearningPath();
  const extractFromCvJdMutation = useExtractFromCvJd();
  const extractFromInterviewMutation = useExtractFromInterview();

  const { data: matchHistoryData } = useGetMatchHistory(1, 50);
  const { data: interviewSessionsData } = useGetInterviewSessions();

  const { data: targetRolesData, isLoading: isTargetRolesLoading } = useTargetRoles();

  const [targetRoleTemplateId, setTargetRoleTemplateId] = useState('');
  const [currentSkills, setCurrentSkills] = useState<{ skillCode: string; currentLevel: number }[]>([]);
  
  const [selectedMatchScoreId, setSelectedMatchScoreId] = useState<string>('');
  const [selectedSessionId, setSelectedSessionId] = useState<string>('');
  
  const [extractedRawData, setExtractedRawData] = useState<any>(null);

  const selectedRoleTemplate = targetRolesData?.data?.find(r => r.id === targetRoleTemplateId);



  const handleSkillLevelChange = (skillCode: string, level: number) => {
    setCurrentSkills(prev => prev.map(s => s.skillCode === skillCode ? { ...s, currentLevel: level } : s));
  };

  const handleGenerate = () => {
    generateMutation.mutate({
      targetRoleTemplateId,
      currentSkills,
    });
  };

  const handleRoleChange = (roleId: string) => {
    setTargetRoleTemplateId(roleId);
    const template = targetRolesData?.data?.find(r => r.id === roleId);
    if (template) {
      setCurrentSkills(template.requiredSkills.map(rs => ({ skillCode: rs.skillCode, currentLevel: 0 })));
    } else {
      setCurrentSkills([]);
    }
  };

  const handleExtractCvJd = () => {
    if (!selectedMatchScoreId) return;
    extractFromCvJdMutation.mutate(selectedMatchScoreId, {
      onSuccess: (res) => {
        if (res.data) {
          setTargetRoleTemplateId(res.data.targetRoleTemplateId);
          setCurrentSkills(res.data.currentSkills);
          setExtractedRawData(res.data);
          // Auto-select Role is triggered but useEffect is avoided because handleRoleChange does it explicitly for manual interactions
        }
      }
    });
  };

  const handleExtractInterview = () => {
    if (!selectedSessionId) return;
    extractFromInterviewMutation.mutate(selectedSessionId, {
      onSuccess: (res) => {
        if (res.data) {
          setTargetRoleTemplateId(res.data.targetRoleTemplateId);
          setCurrentSkills(res.data.currentSkills);
          setExtractedRawData(res.data);
        }
      }
    });
  };

  const isGenerateDisabled = () => {
    return !targetRoleTemplateId || generateMutation.isPending;
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
    <div className="container mx-auto py-8 space-y-8 max-w-6xl">
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
                      <SelectValue placeholder="Select a match result..." />
                    </SelectTrigger>
                    <SelectContent>
                      {matchHistoryData?.data?.items?.filter(m => m.status === 'Completed').map(match => (
                        <SelectItem key={match.jobId} value={match.jobId}>
                          {match.jdTitle} - {match.matchScore?.toFixed(1)}/100
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
              </div>

              <div className="space-y-2">
                <Label className="text-xs text-muted-foreground">From Mock Interview Session</Label>
                <div className="flex gap-2">
                  <Select value={selectedSessionId} onValueChange={(v) => setSelectedSessionId(v || '')}>
                    <SelectTrigger className="flex-1">
                      <SelectValue placeholder="Select an interview..." />
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
              </div>
            </div>
          </div>

          <div className="space-y-8">
            {/* Section 1: Core Information */}
            <div className="space-y-4">
              <h3 className="font-semibold text-lg flex items-center"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Target Role</h3>
              <div className="space-y-2">
                <Label htmlFor="targetRole">Target Role (SFIA Template) <span className="text-red-500">*</span></Label>
                <Select value={targetRoleTemplateId} onValueChange={(v) => handleRoleChange(v || '')}>
                  <SelectTrigger id="targetRole">
                    <SelectValue placeholder={isTargetRolesLoading ? "Loading templates..." : "Select a target role..."} />
                  </SelectTrigger>
                  <SelectContent>
                    {targetRolesData?.data?.map(role => (
                      <SelectItem key={role.id} value={role.id}>{role.roleName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {selectedRoleTemplate && (
                  <p className="text-xs text-muted-foreground">{selectedRoleTemplate.description}</p>
                )}
              </div>
            </div>

            {/* Section 2: Technical Information */}
            {selectedRoleTemplate && (
              <div className="space-y-4">
                <h3 className="font-semibold text-lg flex items-center border-t pt-6"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Self-Assess SFIA Skills</h3>
                <p className="text-sm text-muted-foreground mb-4">Please verify or assess your current proficiency level for the core skills required by this role (0 = No experience, 1-7 = SFIA Levels).</p>
                
                <div className="space-y-6">
                  {selectedRoleTemplate.requiredSkills.map(skill => {
                    const currentLvl = currentSkills.find(s => s.skillCode === skill.skillCode)?.currentLevel || 0;
                    return (
                      <div key={skill.skillCode} className="space-y-2 border p-4 rounded-md">
                        <div className="flex justify-between items-center mb-2">
                          <div>
                            <h4 className="font-medium">{skill.skillName} ({skill.skillCode})</h4>
                            <p className="text-xs text-muted-foreground">Target Level: {skill.targetLevel}</p>
                          </div>
                          <div className="text-right">
                            <span className="text-sm font-semibold text-primary">Your Level: {currentLvl}</span>
                          </div>
                        </div>
                        <input 
                          type="range" 
                          min="0" 
                          max="7" 
                          step="1" 
                          value={currentLvl} 
                          onChange={(e) => handleSkillLevelChange(skill.skillCode, parseInt(e.target.value))}
                          className="w-full h-2 bg-secondary rounded-lg appearance-none cursor-pointer"
                        />
                        <div className="flex justify-between text-xs text-muted-foreground px-1 mt-1">
                          <span className="w-4 text-left">0</span>
                          <span className="w-4 text-center">1</span>
                          <span className="w-4 text-center">2</span>
                          <span className="w-4 text-center">3</span>
                          <span className="w-4 text-center">4</span>
                          <span className="w-4 text-center">5</span>
                          <span className="w-4 text-center">6</span>
                          <span className="w-4 text-right">7</span>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </div>

          <Button
            className="w-full mt-6"
            disabled={isGenerateDisabled()}
            onClick={handleGenerate}
          >
            Generate Learning Path
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
          {extractedRawData && (
            <div className="mt-8 border-t pt-6">
              <details className="group cursor-pointer">
                <summary className="text-sm font-medium text-muted-foreground hover:text-primary transition-colors flex items-center">
                  <span>View AI Extracted Raw Data (Debug)</span>
                </summary>
                <div className="mt-4 bg-muted/30 p-4 rounded-lg border overflow-auto text-xs font-mono max-h-96">
                  <pre>{JSON.stringify(extractedRawData, null, 2)}</pre>
                </div>
              </details>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
); }
