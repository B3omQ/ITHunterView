'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useGenerateLearningPath, useGenerateFromCvJd, useGenerateFromInterview, usePreviewHistoryContext, useTargetRoles } from '@/hooks/useLearningPath';
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
  const generateFromCvJdMutation = useGenerateFromCvJd();
  const generateFromInterviewMutation = useGenerateFromInterview();

  const { data: matchHistoryData } = useGetMatchHistory(1, 50);
  const { data: interviewSessionsData } = useGetInterviewSessions();

  const { data: targetRolesData, isLoading: isTargetRolesLoading } = useTargetRoles();

  const [targetRoleTemplateId, setTargetRoleTemplateId] = useState('');
  const [specificGoal, setSpecificGoal] = useState('');
  const [experienceLevel, setExperienceLevel] = useState('');
  const [currentSkills, setCurrentSkills] = useState<{ skillCode: string; currentLevel: number }[]>([]);
  
  const [learningStyle, setLearningStyle] = useState('');
  const [additionalPreferences, setAdditionalPreferences] = useState('');
  
  const [generationMethod, setGenerationMethod] = useState<'manual' | 'cv-jd' | 'interview'>('manual');
  const [selectedMatchScoreId, setSelectedMatchScoreId] = useState<string>('');
  const [selectedSessionId, setSelectedSessionId] = useState<string>('');

  const activeHistoryType = generationMethod === 'cv-jd' ? 'cv-jd' : 'interview';
  const activeSourceId = generationMethod === 'cv-jd' ? selectedMatchScoreId : selectedSessionId;
  const { data: previewContextData, isLoading: isPreviewLoading } = usePreviewHistoryContext(
    activeHistoryType,
    generationMethod !== 'manual' ? activeSourceId : null
  );

  const selectedRoleTemplate = targetRolesData?.data?.find(r => r.id === targetRoleTemplateId);

  useEffect(() => {
    if (selectedRoleTemplate) {
      setCurrentSkills(selectedRoleTemplate.requiredSkills.map(rs => ({ skillCode: rs.skillCode, currentLevel: 0 })));
    } else {
      setCurrentSkills([]);
    }
  }, [selectedRoleTemplate]);

  const handleSkillLevelChange = (skillCode: string, level: number) => {
    setCurrentSkills(prev => prev.map(s => s.skillCode === skillCode ? { ...s, currentLevel: level } : s));
  };

  const handleGenerate = () => {
    generateMutation.mutate({
      targetRoleTemplateId,
      specificGoal,
      experienceLevel,
      currentSkills,
      learningStyle,
      additionalPreferences,
    });
  };

  const handleGenerateFromHistory = () => {
    if (generationMethod === 'cv-jd') {
      generateFromCvJdMutation.mutate({
        matchScoreId: selectedMatchScoreId,
      });
    } else if (generationMethod === 'interview') {
      generateFromInterviewMutation.mutate({
        sessionId: selectedSessionId,
      });
    }
  };

  const isAnyPending = generateMutation.isPending || generateFromCvJdMutation.isPending || generateFromInterviewMutation.isPending;

  const isGenerateDisabled = () => {
    if (generationMethod === 'manual') {
       return !targetRoleTemplateId || !specificGoal || !experienceLevel || isAnyPending;
    }
    if (generationMethod === 'cv-jd') {
       return !selectedMatchScoreId || isAnyPending;
    }
    if (generationMethod === 'interview') {
       return !selectedSessionId || isAnyPending;
    }
    return true;
  };

  const isAnyError = generateMutation.isError || generateFromCvJdMutation.isError || generateFromInterviewMutation.isError;
  const errorObj = generateMutation.error || generateFromCvJdMutation.error || generateFromInterviewMutation.error;
  let errorMessage = 'Failed to generate path. Please try again.';
  if (errorObj) {
    const axiosError = errorObj as any;
    if (axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT') {
      errorMessage = 'The AI is taking too long to respond. Please try again.';
    } else if (axiosError.response?.data?.message) {
      errorMessage = axiosError.response.data.message;
    }
  }

  useEffect(() => {
    if (generateMutation.isSuccess || generateFromCvJdMutation.isSuccess || generateFromInterviewMutation.isSuccess) {
      router.push('/candidate/learning-path');
    }
  }, [generateMutation.isSuccess, generateFromCvJdMutation.isSuccess, generateFromInterviewMutation.isSuccess, router]);

  if (isAnyPending) {
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
            Configure how you want the AI to generate your personalized learning journey.
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Generation Method</CardTitle>
          <CardDescription>Select a method to generate your learning path.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <RadioGroup 
            value={generationMethod} 
            onValueChange={(val) => setGenerationMethod(val as 'manual' | 'cv-jd' | 'interview')}
            className="flex flex-col space-y-3 mb-6"
          >
            <div className="flex items-center space-x-2">
              <RadioGroupItem value="manual" id="manual" />
              <Label htmlFor="manual" className="font-medium cursor-pointer">Manual Input</Label>
            </div>
            <div className="flex items-center space-x-2">
              <RadioGroupItem value="cv-jd" id="cv-jd" />
              <Label htmlFor="cv-jd" className="font-medium cursor-pointer">From CV-JD Match Result</Label>
            </div>
            <div className="flex items-center space-x-2">
              <RadioGroupItem value="interview" id="interview" />
              <Label htmlFor="interview" className="font-medium cursor-pointer">From Mock Interview Session</Label>
            </div>
          </RadioGroup>

          <div className="pt-4 border-t border-border">
            {generationMethod === 'manual' && (
              <div className="space-y-8">
                {/* Section 1: Core Information */}
                <div className="space-y-4">
                  <h3 className="font-semibold text-lg flex items-center"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Core Information</h3>
                  <div className="grid md:grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="targetRole">Target Role (SFIA Template) <span className="text-red-500">*</span></Label>
                      <Select value={targetRoleTemplateId} onValueChange={(v) => setTargetRoleTemplateId(v || '')}>
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
                    <div className="space-y-2">
                      <Label htmlFor="experienceLevel">Experience Level <span className="text-red-500">*</span></Label>
                      <Select value={experienceLevel} onValueChange={(v) => setExperienceLevel(v || '')}>
                        <SelectTrigger id="experienceLevel"><SelectValue placeholder="Select experience..." /></SelectTrigger>
                        <SelectContent>
                          <SelectItem value="< 1 year">Less than 1 year</SelectItem>
                          <SelectItem value="1-3 years">1-3 years</SelectItem>
                          <SelectItem value="3-5 years">3-5 years</SelectItem>
                          <SelectItem value="5+ years">5+ years</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="specificGoal">Specific Goal <span className="text-red-500">*</span></Label>
                    <Textarea id="specificGoal" placeholder="e.g. Pass Senior Frontend Interview at a product company, or switch from QA to Developer" value={specificGoal} onChange={(e) => setSpecificGoal(e.target.value)} />
                  </div>
                </div>

                {/* Section 2: Technical Information */}
                {selectedRoleTemplate && (
                  <div className="space-y-4">
                    <h3 className="font-semibold text-lg flex items-center border-t pt-6"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Self-Assess SFIA Skills</h3>
                    <p className="text-sm text-muted-foreground mb-4">Please assess your current proficiency level for the core skills required by this role (0 = No experience, 1-7 = SFIA Levels).</p>
                    
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

                {/* Section 3: Personalization */}
                <div className="space-y-4">
                  <h3 className="font-semibold text-lg flex items-center border-t pt-6"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Personalization</h3>
                  <div className="space-y-2">
                    <Label htmlFor="learningStyle">Preferred Learning Style</Label>
                    <Select value={learningStyle} onValueChange={(v) => setLearningStyle(v || '')}>
                      <SelectTrigger id="learningStyle"><SelectValue placeholder="Select preferred learning style..." /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Project-based">Project-based & Hands-on</SelectItem>
                        <SelectItem value="Theory & Video">Video Courses & Theory</SelectItem>
                        <SelectItem value="Coding Exercises">Coding Exercises (LeetCode style)</SelectItem>
                        <SelectItem value="Mixed">Mixed</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="additionalPreferences">Additional Preferences (Budget, Language, etc.)</Label>
                    <Textarea id="additionalPreferences" placeholder="e.g. Prefer free resources, prefer Vietnamese content" value={additionalPreferences} onChange={(e) => setAdditionalPreferences(e.target.value)} />
                  </div>
                </div>
              </div>
            )}

            {generationMethod === 'cv-jd' && (
              <div className="space-y-2">
                <Label>Select CV-JD Match Result</Label>
                <Select value={selectedMatchScoreId} onValueChange={(v) => setSelectedMatchScoreId(v || '')}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a match result..." />
                  </SelectTrigger>
                  <SelectContent>
                    {matchHistoryData?.data?.items?.filter(m => m.status === 'Completed').map(match => (
                      <SelectItem key={match.jobId} value={match.jobId}>
                        {match.jdTitle} - {match.matchScore?.toFixed(1)}/100 ({new Date(match.updatedAt).toLocaleDateString()})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {generationMethod === 'interview' && (
              <div className="space-y-2">
                <Label>Select Mock Interview Session</Label>
                <Select value={selectedSessionId} onValueChange={(v) => setSelectedSessionId(v || '')}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select an interview session..." />
                  </SelectTrigger>
                  <SelectContent>
                    {interviewSessionsData?.data?.filter(s => s.status === 'COMPLETED').map(session => (
                      <SelectItem key={session.id} value={session.id}>
                        {session.jobTitle || 'General Interview'} ({session.difficultyLevel}) - {new Date(session.endedAt || '').toLocaleDateString()}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>

          {generationMethod !== 'manual' && activeSourceId && (
            <div className="mt-4">
              <Alert className="bg-muted/50 border-blue-100 dark:border-blue-900/50">
                <Info className="h-4 w-4 text-blue-500" />
                <AlertTitle className="text-blue-700 dark:text-blue-400">AI Analysis Preview</AlertTitle>
                <AlertDescription>
                  <p className="text-sm mb-2 text-muted-foreground">The AI will use the following specific skill gaps and feedback to tailor your learning path:</p>
                  {isPreviewLoading ? (
                     <span className="flex items-center text-muted-foreground"><Loader2 className="mr-2 h-3 w-3 animate-spin"/> Loading AI context...</span>
                  ) : (
                     <pre className="whitespace-pre-wrap text-xs mt-2 text-muted-foreground font-mono bg-background p-3 rounded border overflow-auto max-h-48">
                       {previewContextData?.data?.contextPreview || "No specific gap data available. A general path will be generated."}
                     </pre>
                  )}
                </AlertDescription>
              </Alert>
            </div>
          )}

          <Button
            className="w-full mt-6"
            disabled={isGenerateDisabled()}
            onClick={generationMethod === 'manual' ? handleGenerate : handleGenerateFromHistory}
          >
            {isAnyPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isAnyPending ? 'Generating Path...' : 'Generate with AI'}
          </Button>

          {isAnyError && (
            <p className="text-sm text-destructive mt-2">
              {errorMessage}
            </p>
          )}

          {(generateMutation.isSuccess || generateFromCvJdMutation.isSuccess || generateFromInterviewMutation.isSuccess) && (
            <p className="text-sm text-green-600 mt-2">
              Learning path successfully generated! Redirecting...
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
