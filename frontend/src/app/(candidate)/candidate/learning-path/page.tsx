'use client';

import { useState } from 'react';
import { useGenerateLearningPath, useMyLearningPaths } from '@/hooks/useLearningPath';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Loader2 } from 'lucide-react';
import { LearningModule } from '@/types/learning-path.types';

export default function LearningPathPage() {
  const { data: myPathsData, isLoading: isLoadingPaths } = useMyLearningPaths();
  const generateMutation = useGenerateLearningPath();

  const [targetRole, setTargetRole] = useState('');
  const [currentSkills, setCurrentSkills] = useState('');
  const [targetSkills, setTargetSkills] = useState('');
  const [timeframeInWeeks, setTimeframeInWeeks] = useState('12');

  const handleGenerate = () => {
    generateMutation.mutate({
      targetRole,
      currentSkills,
      targetSkills,
      timeframeInWeeks: Number(timeframeInWeeks),
    });
  };

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

      <div className="grid md:grid-cols-2 gap-8">
        <Card>
          <CardHeader>
            <CardTitle>Create New Path</CardTitle>
            <CardDescription>Tell us your goals and we'll map out your journey.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
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
            <Button
              className="w-full"
              disabled={generateMutation.isPending || !targetRole || !currentSkills}
              onClick={handleGenerate}
            >
              {generateMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {generateMutation.isPending ? 'Generating Path...' : 'Generate with AI'}
            </Button>

            {generateMutation.isError && (
              <p className="text-sm text-destructive mt-2">Failed to generate path. Please try again.</p>
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <h2 className="text-2xl font-semibold">Your Generated Paths</h2>
          
          {isLoadingPaths ? (
            <div className="flex justify-center p-8"><Loader2 className="h-8 w-8 animate-spin text-muted-foreground" /></div>
          ) : myPathsData?.data && myPathsData.data.length > 0 ? (
            <div className="space-y-4">
              {myPathsData.data.map((path) => (
                <Card key={path.id}>
                  <CardHeader>
                    <CardTitle className="text-lg">Generated Path</CardTitle>
                    <CardDescription>Created on {new Date(path.createdAt).toLocaleDateString()}</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {path.pathData.map((module: LearningModule, index: number) => (
                        <div key={index} className="border-l-2 border-primary pl-4 pb-4">
                          <h4 className="font-semibold text-md">{module.title}</h4>
                          <p className="text-sm text-muted-foreground my-1">{module.description}</p>
                          <div className="flex gap-2 items-center text-sm">
                            <span className="font-medium">{module.durationWeeks} Weeks</span>
                            <span className="text-muted-foreground">•</span>
                            <span className="text-muted-foreground">{module.skills.join(', ')}</span>
                          </div>
                        </div>
                      ))}
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
      </div>
    </div>
  );
}
