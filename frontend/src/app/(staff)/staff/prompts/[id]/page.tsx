'use client';

import { useParams } from 'next/navigation';
import { useActivatePromptVersion, useCreatePromptVersion, usePromptHistory } from '@/hooks/use-prompts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { ArrowLeft, CheckCircle2, Copy, Loader2, Save } from 'lucide-react';
import Link from 'next/link';
import { APP_ROUTES } from '@/lib/constants';
import { useState } from 'react';
import { format } from 'date-fns';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from 'sonner';
import { CvAnalysisPairActivationCard } from '@/components/prompts/CvAnalysisPairActivationCard';
import { JdAnalysisPairActivationCard } from '@/components/prompts/JdAnalysisPairActivationCard';
import { isJdMatchingPromptKey, normalizePromptModelConfigForSubmission, sanitizeJdMatchingContentForEditing } from '@/lib/prompts/jd-matching-prompt-policy';
import { isJdAnalysisPromptKey, sanitizeJdAnalysisContentForEditing } from '@/lib/prompts/jd-analysis-prompt-policy';
import { useTranslations } from 'next-intl';

const formSchema = z.object({
  versionTag: z.string().min(1, 'Version Tag is required').max(50),
  content: z.string().min(1, 'Prompt content is required'),
  modelConfig: z.string().optional(),
  makeActive: z.boolean(),
});

export default function PromptDetailPage() {
  const t = useTranslations('StaffPrompts');
  const params = useParams();
  const id = params.id as string;

  const { data: response, isLoading, isError } = usePromptHistory(id);
  const prompt = response?.data;

  const createMutation = useCreatePromptVersion(id);
  const activateMutation = useActivatePromptVersion(id);

  const [activeTab, setActiveTab] = useState('history');
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      versionTag: '',
      content: '',
      modelConfig: '',
      makeActive: false,
    },
  });

  // Whenever prompt loads, pre-select the active version for viewing
  if (prompt && !selectedVersionId && prompt.versions && prompt.versions.length > 0) {
    const activeV = prompt.versions.find(v => v.isActive) || prompt.versions[0];
    setSelectedVersionId(activeV.id);
  }

  const selectedVersion = prompt?.versions?.find(v => v.id === selectedVersionId);
  const isCvAnalysisPrompt = prompt?.promptKey === 'CV_ANALYSIS_SYSTEM' || prompt?.promptKey === 'CV_ANALYSIS_USER';
  const isJdAnalysisPrompt = isJdAnalysisPromptKey(prompt?.promptKey);
  const isJdMatchingPrompt = isJdMatchingPromptKey(prompt?.promptKey);
  const isManagedAnalysisPrompt = isCvAnalysisPrompt || isJdAnalysisPrompt;

  function onSubmit(values: z.infer<typeof formSchema>) {
    createMutation.mutate({
      ...values,
      modelConfig: normalizePromptModelConfigForSubmission(prompt?.promptKey, values.modelConfig),
      makeActive: isManagedAnalysisPrompt ? false : values.makeActive,
    }, {
      onSuccess: () => {
        form.reset();
        setActiveTab('history');
      }
    });
  }

  function handleActivate(versionId: string) {
    if (confirm(t('activateConfirm'))) {
      activateMutation.mutate(versionId);
    }
  }

  function handleCopyFromExisting(content: string, modelConfig?: string) {
    const editableContent = isJdMatchingPrompt
      ? sanitizeJdMatchingContentForEditing(content)
      : isJdAnalysisPrompt
        ? sanitizeJdAnalysisContentForEditing(content)
        : content;
    form.setValue('content', editableContent);
    form.setValue('modelConfig', normalizePromptModelConfigForSubmission(prompt?.promptKey, modelConfig) || '');
    setActiveTab('create');
    toast.info(t('copySuccess'));
  }

  if (isLoading) return <div className="p-8 text-center text-muted-foreground">{t('loadingDetails')}</div>;
  if (isError || !prompt) return <div className="p-8 text-center text-destructive">{t('failedLoadDetail')}</div>;

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex items-center gap-4">
        <Link href={APP_ROUTES.STAFF.PROMPTS}>
          <Button variant="outline" size="icon">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </Link>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{prompt.promptKey}</h1>
          <p className="text-muted-foreground mt-1">{prompt.description || t('noDesc')}</p>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-2 lg:w-[400px]">
          <TabsTrigger value="history">{t('tabHistory')}</TabsTrigger>
          <TabsTrigger value="create">{t('tabCreate')}</TabsTrigger>
        </TabsList>

        <TabsContent value="history" className="mt-6 space-y-6">
          {isCvAnalysisPrompt && (
            <Card className="border-blue-200 bg-blue-50/50 dark:border-blue-900 dark:bg-blue-950/20">
              <CardHeader>
                <CardTitle className="text-base">Application-managed CV analysis output</CardTitle>
                <CardDescription>
                  This editor controls CV extraction instructions only. The application appends the fixed cv-analysis/v2 JSON schema at runtime. Known historical embedded schemas are removed when a new version is saved; modified schemas are rejected.
                </CardDescription>
              </CardHeader>
            </Card>
          )}
          {isJdAnalysisPrompt && (
            <Card className="border-blue-200 bg-blue-50/50 dark:border-blue-900 dark:bg-blue-950/20">
              <CardHeader>
                <CardTitle className="text-base">Application-managed JD analysis output</CardTitle>
                <CardDescription>
                  This editor controls semantic JD extraction instructions only. The application appends the fixed jd-analysis/v5 JSON schema at runtime. System and user versions activate only as a compatible pair; ModelConfig.contract pairs those versions and does not select the output schema.
                </CardDescription>
              </CardHeader>
            </Card>
          )}
          {isJdMatchingPrompt && (
            <Card className="border-blue-200 bg-blue-50/50 dark:border-blue-900 dark:bg-blue-950/20">
              <CardHeader>
                <CardTitle className="text-base">Application-managed matching output</CardTitle>
                <CardDescription>
                  This editor controls semantic matching instructions only. The application appends and validates the approved JSON output schema at runtime; it cannot be edited here.
                </CardDescription>
              </CardHeader>
            </Card>
          )}
          {isCvAnalysisPrompt && <CvAnalysisPairActivationCard />}
          {isJdAnalysisPrompt && <JdAnalysisPairActivationCard />}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left Column: List of versions */}
            <Card className="lg:col-span-1 h-fit max-h-[800px] overflow-y-auto">
              <CardHeader className="sticky top-0 bg-card z-10 border-b">
                <CardTitle className="text-lg">{t('versionsTitle')}</CardTitle>
              </CardHeader>
              <div className="p-0">
                {prompt.versions?.length === 0 && (
                  <div className="p-6 text-center text-muted-foreground">{t('noVersions')}</div>
                )}
                <div className="flex flex-col">
                  {prompt.versions?.map((version) => (
                    <div
                      key={version.id}
                      onClick={() => setSelectedVersionId(version.id)}
                      className={`p-4 border-b cursor-pointer transition-colors hover:bg-muted/50 ${selectedVersionId === version.id ? 'bg-muted/80 border-l-4 border-l-primary' : ''}`}
                    >
                      <div className="flex justify-between items-start mb-2">
                        <span className="font-semibold">{version.versionTag}</span>
                        {version.isActive && (
                          <Badge variant="default" className="bg-green-600/10 text-green-700 hover:bg-green-600/20">
                            {t('activeBadge')}
                          </Badge>
                        )}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        {format(new Date(version.createdAt), 'MMM dd, yyyy HH:mm')}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </Card>

            {/* Right Column: View selected version */}
            <div className="lg:col-span-2 space-y-4">
              {selectedVersion ? (
                <Card>
                  <CardHeader className="flex flex-row items-start justify-between space-y-0">
                    <div>
                      <CardTitle className="flex items-center gap-2">
                        {selectedVersion.versionTag}
                        {selectedVersion.isActive && <CheckCircle2 className="h-5 w-5 text-green-600" />}
                      </CardTitle>
                      <CardDescription className="mt-1">
                        {t('createdAt')} {format(new Date(selectedVersion.createdAt), 'PPpp')}
                      </CardDescription>
                    </div>
                    <div className="flex gap-2">
                      <Button variant="outline" size="sm" onClick={() => handleCopyFromExisting(selectedVersion.content, selectedVersion.modelConfig)}>
                        <Copy className="h-4 w-4 mr-2" />
                        {t('copyToNewBtn')}
                      </Button>
                      {!selectedVersion.isActive && !isManagedAnalysisPrompt && (
                        <Button 
                          size="sm" 
                          onClick={() => handleActivate(selectedVersion.id)}
                          disabled={activateMutation.isPending}
                        >
                          {t('activateBtn')}
                        </Button>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    <div>
                      <h4 className="text-sm font-medium mb-2">{t('promptContentTitle')}</h4>
                      <div className="bg-muted p-4 rounded-md overflow-x-auto">
                        <pre className="text-sm font-mono whitespace-pre-wrap">{selectedVersion.content}</pre>
                      </div>
                    </div>
                    
                    {selectedVersion.modelConfig && (
                      <div>
                        <h4 className="text-sm font-medium mb-2">{t('modelConfigTitle')}</h4>
                        <div className="bg-muted p-4 rounded-md overflow-x-auto">
                          <pre className="text-sm font-mono whitespace-pre-wrap">{selectedVersion.modelConfig}</pre>
                        </div>
                      </div>
                    )}
                  </CardContent>
                </Card>
              ) : (
                <Card className="h-full flex items-center justify-center p-8 text-muted-foreground">
                  {t('selectToView')}
                </Card>
              )}
            </div>
          </div>
        </TabsContent>

        <TabsContent value="create" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>Create New Version</CardTitle>
              <CardDescription>
                Create a new immutable version for <span className="font-mono text-primary">{prompt.promptKey}</span>.
                {isJdMatchingPrompt
                  ? ' Edit semantic instructions only; keep the CV and JD input slots intact.'
                  : isCvAnalysisPrompt
                    ? ' Edit semantic extraction instructions only; keep [CV_TEXT] exactly once in the user template. Do not add or modify an output JSON schema.'
                    : isJdAnalysisPrompt
                      ? ' Edit semantic extraction instructions only; keep [JD_TEXT] exactly once in the user template. The application owns and appends the output JSON schema.'
                  : ' Remember to keep required placeholders like [CV_TEXT] and [JD_TEXT].'}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                  
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <FormField
                      control={form.control}
                      name="versionTag"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t('versionTagLabel')}</FormLabel>
                          <FormControl>
                            <Input placeholder={t('versionTagPlaceholder')} {...field} />
                          </FormControl>
                          <FormDescription>{t('versionTagHelp')}</FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    {isManagedAnalysisPrompt ? (
                      <div className="rounded-lg border p-4 text-sm text-muted-foreground">
                        {t('draftHelp')}
                      </div>
                    ) : (
                      <FormField
                        control={form.control}
                        name="makeActive"
                        render={({ field }) => (
                          <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                            <div className="space-y-0.5">
                              <FormLabel className="text-base">{t('setActiveLabel')}</FormLabel>
                              <FormDescription>
                                {t('setActiveHelp')}
                              </FormDescription>
                            </div>
                            <FormControl>
                              <Switch checked={field.value} onCheckedChange={field.onChange} />
                            </FormControl>
                          </FormItem>
                        )}
                      />
                    )}
                  </div>

                  <FormField
                    control={form.control}
                    name="content"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('promptContentTitle')}</FormLabel>
                        <FormControl>
                          <Textarea 
                            placeholder={t('promptContentPlaceholder')} 
                            className="min-h-[400px] font-mono text-sm" 
                            {...field} 
                          />
                        </FormControl>
                        <FormDescription>
                          {isCvAnalysisPrompt
                            ? 'Edit semantic extraction instructions only. Keep [CV_TEXT] exactly once in the user template. Do not add or modify an output JSON schema.'
                            : isJdAnalysisPrompt
                            ? 'Edit semantic extraction instructions only. Keep [JD_TEXT] exactly once in the user template. Do not add or modify an output JSON schema.'
                            : isJdMatchingPrompt
                            ? 'Use raw semantic instructions. Keep exactly one operational CV and JD input slot; the output schema is managed by the application.'
                            : 'Use raw text. Make sure to include variables wrapped in brackets, like [CV_TEXT].'}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  {isJdMatchingPrompt ? (
                    <div className="rounded-md border bg-muted/40 p-4 text-sm text-muted-foreground">
                      Matching provider settings and the JSON output schema are managed by the application. This version stores semantic instructions only, so ModelConfig is not editable.
                    </div>
                  ) : (
                    <FormField
                      control={form.control}
                      name="modelConfig"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t('modelConfigLabel')}</FormLabel>
                          <FormControl>
                            <Textarea
                              placeholder={`{\n  "temperature": 0.2,\n  "topK": 40\n}`}
                              className="min-h-[150px] font-mono text-sm"
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            {isJdAnalysisPrompt
                              ? 'Optional provider settings. ModelConfig.contract only pairs compatible JD system and user prompt versions; it does not select the output schema.'
                              : 'Optional JSON overriding default LLM settings. Must be valid JSON.'}
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  )}

                  <div className="flex justify-end gap-4">
                    <Button type="button" variant="outline" onClick={() => setActiveTab('history')}>
                      {t('cancelBtn')}
                    </Button>
                    <Button type="submit" disabled={createMutation.isPending}>
                      {createMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                      <Save className="mr-2 h-4 w-4" />
                      {t('saveBtn')}
                    </Button>
                  </div>
                </form>
              </Form>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
