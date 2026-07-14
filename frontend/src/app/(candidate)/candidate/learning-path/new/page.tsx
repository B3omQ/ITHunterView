'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useGenerateLearningPath, useGenerateFromCvJd, useGenerateFromInterview, usePreviewHistoryContext } from '@/hooks/useLearningPath';
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

export default function NewLearningPathPage() {
  const router = useRouter();
  
  const generateMutation = useGenerateLearningPath();
  const generateFromCvJdMutation = useGenerateFromCvJd();
  const generateFromInterviewMutation = useGenerateFromInterview();

  const { data: matchHistoryData } = useGetMatchHistory(1, 50);
  const { data: interviewSessionsData } = useGetInterviewSessions();

  const [targetRole, setTargetRole] = useState('');
  const [specificGoal, setSpecificGoal] = useState('');
  const [experienceLevel, setExperienceLevel] = useState('');
  const [currentSkills, setCurrentSkills] = useState('');
  
  const [strengths, setStrengths] = useState('');
  const [weaknesses, setWeaknesses] = useState('');
  const [targetCompanyType, setTargetCompanyType] = useState('');
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

  const handleGenerate = () => {
    generateMutation.mutate({
      targetRole,
      specificGoal,
      experienceLevel,
      currentSkills,
      strengths,
      weaknesses,
      targetCompanyType,
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
       return !targetRole || !specificGoal || !experienceLevel || !currentSkills || isAnyPending;
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
  const errorMessage = (generateMutation.error as any)?.response?.data?.message || 
                       (generateFromCvJdMutation.error as any)?.response?.data?.message || 
                       (generateFromInterviewMutation.error as any)?.response?.data?.message || 
                       'Failed to generate path. Please try again.';

  useEffect(() => {
    if (generateMutation.isSuccess || generateFromCvJdMutation.isSuccess || generateFromInterviewMutation.isSuccess) {
      router.push('/candidate/learning-path');
    }
  }, [generateMutation.isSuccess, generateFromCvJdMutation.isSuccess, generateFromInterviewMutation.isSuccess, router]);

  return (
    <div className="container mx-auto py-8 space-y-8 max-w-3xl">
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
                      <Label htmlFor="targetRole">Target Role <span className="text-red-500">*</span></Label>
                      <Input id="targetRole" placeholder="e.g. Senior Frontend Developer" value={targetRole} onChange={(e) => setTargetRole(e.target.value)} />
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
                <div className="space-y-4">
                  <h3 className="font-semibold text-lg flex items-center border-t pt-6"><Sparkles className="mr-2 h-5 w-5 text-primary" /> Technical Profile</h3>
                  <div className="space-y-2">
                    <Label htmlFor="currentSkills">Current Tech Stack & Proficiency <span className="text-red-500">*</span></Label>
                    <Textarea id="currentSkills" placeholder="e.g. React - Advanced, Node.js - Beginner, SQL - Intermediate" value={currentSkills} onChange={(e) => setCurrentSkills(e.target.value)} />
                  </div>
                  <div className="grid md:grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="strengths">Strengths</Label>
                      <Textarea id="strengths" placeholder="e.g. UI/UX, CSS, API Integration" value={strengths} onChange={(e) => setStrengths(e.target.value)} />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="weaknesses">Weaknesses</Label>
                      <Textarea id="weaknesses" placeholder="e.g. System Design, State Management" value={weaknesses} onChange={(e) => setWeaknesses(e.target.value)} />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="targetCompanyType">Target Company Type</Label>
                    <Select value={targetCompanyType} onValueChange={(v) => setTargetCompanyType(v || '')}>
                      <SelectTrigger id="targetCompanyType"><SelectValue placeholder="Select target company type..." /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Product">Product Company</SelectItem>
                        <SelectItem value="Outsourcing">Outsourcing / Agency</SelectItem>
                        <SelectItem value="Startup">Startup</SelectItem>
                        <SelectItem value="Any">Any / Not sure</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

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
