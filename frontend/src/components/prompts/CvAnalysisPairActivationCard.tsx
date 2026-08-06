'use client';

import { useState } from 'react';
import { Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useActivateCvAnalysisPromptPair, useCvAnalysisPromptPair } from '@/hooks/use-prompts';

function activeOrFirstVersionId(versions: { id: string; isActive: boolean }[]) {
  return versions.find((version) => version.isActive)?.id || versions[0]?.id || '';
}

export function CvAnalysisPairActivationCard() {
  const { data: response, isLoading, isError } = useCvAnalysisPromptPair();
  const activateMutation = useActivateCvAnalysisPromptPair();
  const pair = response?.data;
  const [systemVersionId, setSystemVersionId] = useState('');
  const [userVersionId, setUserVersionId] = useState('');

  if (isLoading) {
    return <Card><CardContent className="py-6 text-sm text-muted-foreground">Loading CV analysis prompt pair...</CardContent></Card>;
  }

  if (isError || !pair) {
    return <Card><CardContent className="py-6 text-sm text-destructive">Failed to load the CV analysis prompt pair.</CardContent></Card>;
  }

  const systemVersions = pair.systemPrompt.versions || [];
  const userVersions = pair.userPrompt.versions || [];
  const resolvedSystemVersionId = systemVersions.some((version) => version.id === systemVersionId)
    ? systemVersionId
    : activeOrFirstVersionId(systemVersions);
  const resolvedUserVersionId = userVersions.some((version) => version.id === userVersionId)
    ? userVersionId
    : activeOrFirstVersionId(userVersions);
  const canActivate = Boolean(resolvedSystemVersionId && resolvedUserVersionId);
  const handleActivate = () => {
    if (!canActivate) return;
    if (confirm('Activate these CV system and user prompt versions together? The previous pair will be replaced atomically.')) {
      activateMutation.mutate({ systemVersionId: resolvedSystemVersionId, userVersionId: resolvedUserVersionId });
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Activate CV analysis prompt pair</CardTitle>
        <CardDescription>
          Choose one compatible system prompt and one user template. They are activated together, so a CV parse never uses a mixed pair.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <label className="grid gap-1.5 text-sm font-medium">
          {pair.systemPrompt.promptKey}
          <select
            className="h-10 rounded-md border border-input bg-background px-3 text-sm"
            value={resolvedSystemVersionId}
            onChange={(event) => setSystemVersionId(event.target.value)}
          >
            {systemVersions.map((version) => (
              <option key={version.id} value={version.id}>
                {version.versionTag}{version.isActive ? ' (active)' : ''}
              </option>
            ))}
          </select>
        </label>

        <label className="grid gap-1.5 text-sm font-medium">
          {pair.userPrompt.promptKey}
          <select
            className="h-10 rounded-md border border-input bg-background px-3 text-sm"
            value={resolvedUserVersionId}
            onChange={(event) => setUserVersionId(event.target.value)}
          >
            {userVersions.map((version) => (
              <option key={version.id} value={version.id}>
                {version.versionTag}{version.isActive ? ' (active)' : ''}
              </option>
            ))}
          </select>
        </label>

        <Button onClick={handleActivate} disabled={!canActivate || activateMutation.isPending}>
          {activateMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Activate pair
        </Button>
      </CardContent>
    </Card>
  );
}
