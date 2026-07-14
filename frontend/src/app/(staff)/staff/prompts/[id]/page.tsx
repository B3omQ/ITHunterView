'use client';

import { useParams, useRouter } from 'next/navigation';
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

const formSchema = z.object({
  versionTag: z.string().min(1, 'Version Tag is required').max(50),
  content: z.string().min(1, 'Prompt content is required'),
  modelConfig: z.string().optional(),
  makeActive: z.boolean(),
});

export default function PromptDetailPage() {
  const params = useParams();
  const id = params.id as string;
  const router = useRouter();

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

  function onSubmit(values: z.infer<typeof formSchema>) {
    createMutation.mutate(values, {
      onSuccess: () => {
        form.reset();
        setActiveTab('history');
      }
    });
  }

  function handleActivate(versionId: string) {
    if (confirm('Are you sure you want to make this version active? The current active version will be deactivated.')) {
      activateMutation.mutate(versionId);
    }
  }

  function handleCopyFromExisting(content: string, modelConfig?: string) {
    form.setValue('content', content);
    form.setValue('modelConfig', modelConfig || '');
    setActiveTab('create');
    toast.info('Copied content to new version form');
  }

  if (isLoading) return <div className="p-8 text-center text-muted-foreground">Loading prompt details...</div>;
  if (isError || !prompt) return <div className="p-8 text-center text-destructive">Failed to load prompt</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href={APP_ROUTES.STAFF.PROMPTS}>
          <Button variant="outline" size="icon">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </Link>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{prompt.promptKey}</h1>
          <p className="text-muted-foreground mt-1">{prompt.description || 'No description provided'}</p>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-2 lg:w-[400px]">
          <TabsTrigger value="history">Version History</TabsTrigger>
          <TabsTrigger value="create">Create New Version</TabsTrigger>
        </TabsList>

        <TabsContent value="history" className="mt-6 space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left Column: List of versions */}
            <Card className="lg:col-span-1 h-fit max-h-[800px] overflow-y-auto">
              <CardHeader className="sticky top-0 bg-card z-10 border-b">
                <CardTitle className="text-lg">Versions</CardTitle>
              </CardHeader>
              <div className="p-0">
                {prompt.versions?.length === 0 && (
                  <div className="p-6 text-center text-muted-foreground">No versions found.</div>
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
                            Active
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
                        Created at {format(new Date(selectedVersion.createdAt), 'PPpp')}
                      </CardDescription>
                    </div>
                    <div className="flex gap-2">
                      <Button variant="outline" size="sm" onClick={() => handleCopyFromExisting(selectedVersion.content, selectedVersion.modelConfig)}>
                        <Copy className="h-4 w-4 mr-2" />
                        Copy to New
                      </Button>
                      {!selectedVersion.isActive && (
                        <Button 
                          size="sm" 
                          onClick={() => handleActivate(selectedVersion.id)}
                          disabled={activateMutation.isPending}
                        >
                          Activate
                        </Button>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    <div>
                      <h4 className="text-sm font-medium mb-2">Prompt Content</h4>
                      <div className="bg-muted p-4 rounded-md overflow-x-auto">
                        <pre className="text-sm font-mono whitespace-pre-wrap">{selectedVersion.content}</pre>
                      </div>
                    </div>
                    
                    {selectedVersion.modelConfig && (
                      <div>
                        <h4 className="text-sm font-medium mb-2">Model Config (JSON)</h4>
                        <div className="bg-muted p-4 rounded-md overflow-x-auto">
                          <pre className="text-sm font-mono whitespace-pre-wrap">{selectedVersion.modelConfig}</pre>
                        </div>
                      </div>
                    )}
                  </CardContent>
                </Card>
              ) : (
                <Card className="h-full flex items-center justify-center p-8 text-muted-foreground">
                  Select a version from the left to view details.
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
                Remember to keep required placeholders like [CV_TEXT] and [JD_TEXT].
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
                          <FormLabel>Version Tag</FormLabel>
                          <FormControl>
                            <Input placeholder="e.g., v2.0-experimental" {...field} />
                          </FormControl>
                          <FormDescription>A unique identifier for this version.</FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="makeActive"
                      render={({ field }) => (
                        <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                          <div className="space-y-0.5">
                            <FormLabel className="text-base">Set as Active</FormLabel>
                            <FormDescription>
                              Make this version active immediately after creation.
                            </FormDescription>
                          </div>
                          <FormControl>
                            <Switch
                              checked={field.value}
                              onCheckedChange={field.onChange}
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />
                  </div>

                  <FormField
                    control={form.control}
                    name="content"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Prompt Content</FormLabel>
                        <FormControl>
                          <Textarea 
                            placeholder="You are an expert recruiter..." 
                            className="min-h-[400px] font-mono text-sm" 
                            {...field} 
                          />
                        </FormControl>
                        <FormDescription>
                          Use raw text. Make sure to include variables wrapped in brackets, like [CV_TEXT].
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="modelConfig"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Model Configuration (Optional JSON)</FormLabel>
                        <FormControl>
                          <Textarea 
                            placeholder={`{\n  "temperature": 0.2,\n  "topK": 40\n}`} 
                            className="min-h-[150px] font-mono text-sm" 
                            {...field} 
                          />
                        </FormControl>
                        <FormDescription>
                          Optional JSON overriding default LLM settings. Must be valid JSON.
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="flex justify-end gap-4">
                    <Button type="button" variant="outline" onClick={() => setActiveTab('history')}>
                      Cancel
                    </Button>
                    <Button type="submit" disabled={createMutation.isPending}>
                      {createMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                      <Save className="mr-2 h-4 w-4" />
                      Save Version
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
