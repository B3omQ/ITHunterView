'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useGenerateLearningPath, useGenerateFromCvJd, useGenerateFromInterview, useMyLearningPaths, useDeleteLearningPath, usePreviewHistoryContext } from '@/hooks/useLearningPath';
import { useGetMatchHistory } from '@/hooks/useCvMatch';
import { useGetInterviewSessions } from '@/hooks/useInterview';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertTitle, AlertDescription } from '@/components/ui/alert';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';

import { Loader2, Sparkles, History, Trash2, Info } from 'lucide-react';
import { LearningModule } from '@/types/learning-path.types';

export default function LearningPathPage() {
  const { data: myPathsData, isLoading: isLoadingPaths } = useMyLearningPaths();
  const generateMutation = useGenerateLearningPath();
  const generateFromCvJdMutation = useGenerateFromCvJd();
  const generateFromInterviewMutation = useGenerateFromInterview();

  const { data: matchHistoryData } = useGetMatchHistory(1, 50);
  const { data: interviewSessionsData } = useGetInterviewSessions();

  const [targetRole, setTargetRole] = useState('');
  const [currentSkills, setCurrentSkills] = useState('');
  const [targetSkills, setTargetSkills] = useState('');
  const [timeframeInWeeks, setTimeframeInWeeks] = useState('12');
  
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
      currentSkills,
      targetSkills,
      timeframeInWeeks: Number(timeframeInWeeks),
    });
  };

  const handleGenerateFromHistory = () => {
    if (generationMethod === 'cv-jd') {
      generateFromCvJdMutation.mutate({
        timeframeInWeeks: 12,
        matchScoreId: selectedMatchScoreId,
      });
    } else if (generationMethod === 'interview') {
      generateFromInterviewMutation.mutate({
        timeframeInWeeks: 12,
        sessionId: selectedSessionId,
      });
    }
  };

  const isAnyPending = generateMutation.isPending || generateFromCvJdMutation.isPending || generateFromInterviewMutation.isPending;

  const isGenerateDisabled = () => {
    if (generationMethod === 'manual') {
       return !targetRole || !currentSkills || isAnyPending;
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

  return (
    <div className="container mx-auto py-8 space-y-8 max-w-5xl">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">AI Learning Path Generator</h1>
          <p className="text-muted-foreground mt-2">
            Generate a personalized, step-by-step career path using our advanced AI.
          </p>
        </div>
      </div>

      <div className="grid md:grid-cols-2 gap-8 mt-6">
        <Card>
          <CardHeader>
            <CardTitle>Create New Path</CardTitle>
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
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="targetRole">Target Role</Label>
                    <Input
                      id="targetRole"
                      placeholder="e.g. Senior Full Stack Developer"
                      value={targetRole}
                      onChange={(e) => setTargetRole(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="currentSkills">Current Skills</Label>
                    <Textarea
                      id="currentSkills"
                      placeholder="e.g. React, Next.js, basic Node.js"
                      value={currentSkills}
                      onChange={(e) => setCurrentSkills(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="targetSkills">Target Skills (Optional)</Label>
                    <Textarea
                      id="targetSkills"
                      placeholder="e.g. System Design, Docker, Kubernetes"
                      value={targetSkills}
                      onChange={(e) => setTargetSkills(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="timeframe">Timeframe (Weeks)</Label>
                    <Input
                      id="timeframe"
                      type="number"
                      min="1"
                      max="52"
                      value={timeframeInWeeks}
                      onChange={(e) => setTimeframeInWeeks(e.target.value)}
                    />
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
                Learning path successfully generated! Check the list.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Kết quả paths panel bên phải */}
        <PathListPanel pathsData={myPathsData?.data} isLoading={isLoadingPaths} />
      </div>
    </div>
  );
}

// ─── Shared sub-component (only used in this file) ───────────────────────────
function PathListPanel({
  pathsData,
  isLoading,
}: {
  pathsData?: import('@/types/learning-path.types').LearningPath[];
  isLoading: boolean;
}) {
  const deleteMutation = useDeleteLearningPath();

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold">Your Generated Paths</h2>

      {isLoading ? (
        <div className="flex justify-center p-8">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : pathsData && pathsData.length > 0 ? (
        <div className="space-y-4">
          {pathsData.map((path) => (
            <Card key={path.id}>
              <CardHeader className="flex flex-row items-start justify-between space-y-0">
                <div>
                  <CardTitle className="text-lg">Generated Path</CardTitle>
                  <CardDescription className="mt-1">
                    Created on {new Date(path.createdAt).toLocaleDateString('vi-VN')}
                  </CardDescription>
                </div>
                <Button 
                  variant="ghost" 
                  size="icon" 
                  className="text-muted-foreground hover:text-destructive -mr-2"
                  onClick={() => {
                    if (confirm('Are you sure you want to delete this learning path?')) {
                      deleteMutation.mutate(path.id);
                    }
                  }}
                  disabled={deleteMutation.isPending && deleteMutation.variables === path.id}
                >
                  {deleteMutation.isPending && deleteMutation.variables === path.id ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Trash2 className="h-4 w-4" />
                  )}
                </Button>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex gap-4 text-sm text-muted-foreground">
                    <div>
                      <span className="font-semibold text-foreground">{path.pathData.length}</span> modules
                    </div>
                    <div>
                      <span className="font-semibold text-foreground">
                        {path.pathData.reduce((acc, curr) => acc + curr.durationWeeks, 0)}
                      </span> weeks total
                    </div>
                  </div>
                  
                  <Link href={`/candidate/learning-path/${path.id}`} passHref>
                    <Button variant="outline" className="w-full mt-2">
                      View Details
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <div className="text-center p-8 border border-dashed rounded-lg text-muted-foreground">
          No learning paths generated yet.
        </div>
      )}
    </div>
  );
}
