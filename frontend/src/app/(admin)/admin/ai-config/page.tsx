'use client';

import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import {
  Save,
  Activity,
  CheckCircle2,
  XCircle,
  Loader2,
  Cpu,
  Key,
  Gauge,
  Check,
} from 'lucide-react';
import {
  aiConfigService,
  AiConfigResponse,
  UpdateAiConfigRequest,
} from '@/services/ai-config.service';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Label } from '@/components/ui/label';

export default function AiConfigPage() {
  const [config, setConfig] = useState<AiConfigResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<{
    success: boolean;
    message: string;
    ms: number;
  } | null>(null);

  const { register, handleSubmit, setValue, watch } = useForm<UpdateAiConfigRequest>({
    defaultValues: {
      providerName: 'Gemini',
      requestsPerMinute: 60,
      apiKey: '',
    },
  });

  const activeProvider = watch('providerName');

  const loadConfig = async () => {
    setIsLoading(true);
    try {
      const res = await aiConfigService.getConfigs();
      if (res.data) {
        setConfig(res.data);
        setValue('providerName', res.data.activeProvider);
        setValue('requestsPerMinute', res.data.requestsPerMinute);
      }
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Failed to load AI config');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadConfig();
  }, []);

  const onSubmit = async (data: UpdateAiConfigRequest) => {
    setIsSaving(true);
    try {
      await aiConfigService.updateConfig(data);
      toast.success('AI Configuration saved successfully!');
      // clear api key field after saving
      setValue('apiKey', '');
      await loadConfig();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Failed to save AI config');
    } finally {
      setIsSaving(false);
    }
  };

  const handleTestConnection = async () => {
    setIsTesting(true);
    setTestResult(null);
    try {
      const res = await aiConfigService.testConnection({ providerName: activeProvider });
      setTestResult({
        success: res.data?.success ?? false,
        message: res.data?.message || 'Success',
        ms: res.data?.responseTimeMs ?? 0,
      });
      if (res.data?.success) {
        toast.success('Test connection successful!');
      } else {
        toast.error('Test connection failed!');
      }
    } catch (error: any) {
      setTestResult({
        success: false,
        message: error?.response?.data?.message || 'Test connection failed due to an error.',
        ms: 0,
      });
      toast.error('Test connection failed!');
    } finally {
      setIsTesting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-[#1877F2]" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight flex items-center gap-2.5">
              <Cpu className="text-[#1877F2] shrink-0 h-8 w-8" />
              AI Configuration
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Manage the active AI model, API keys, and rate limits for platform intelligence features.
            </p>
          </div>
        </div>

        {/* Main Content Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          {/* Card 1: Active Provider Selection */}
          <Card className="border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 shadow-2xs">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg font-bold text-[#050505] dark:text-zinc-50 flex items-center gap-2">
                <Cpu className="h-5 w-5 text-[#1877F2]" />
                Active Provider Selection
              </CardTitle>
              <CardDescription className="text-xs text-[#65676B] dark:text-zinc-400">
                Choose the primary Large Language Model (LLM) engine powering AI features across the application.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {config?.availableProviders.map((provider) => {
                  const isSelected = activeProvider === provider.providerName;
                  return (
                    <label
                      key={provider.providerName}
                      htmlFor={provider.providerName}
                      className={`relative flex flex-col justify-between p-4 rounded-xl border cursor-pointer transition-all duration-150 ${
                        isSelected
                          ? 'border-[#1877F2] bg-[#E7F3FF]/40 dark:bg-blue-950/30 ring-2 ring-[#1877F2]/20'
                          : 'border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 hover:border-[#1877F2]/50 hover:bg-slate-50 dark:hover:bg-zinc-800/50'
                      }`}
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex items-center gap-2.5">
                          <input
                            id={provider.providerName}
                            type="radio"
                            value={provider.providerName}
                            {...register('providerName')}
                            className="h-4 w-4 text-[#1877F2] border-[#CED0D4] focus:ring-[#1877F2] cursor-pointer"
                          />
                          <span className="font-bold text-sm text-[#050505] dark:text-zinc-100">
                            {provider.providerName}
                          </span>
                        </div>
                        {isSelected && (
                          <div className="h-5 w-5 rounded-full bg-[#1877F2] text-white flex items-center justify-center shrink-0">
                            <Check className="h-3 w-3 stroke-[3]" />
                          </div>
                        )}
                      </div>

                      <div className="mt-3 space-y-1.5">
                        <p className="text-xs text-[#65676B] dark:text-zinc-400">
                          Model: <span className="font-semibold text-[#050505] dark:text-zinc-200">{provider.model || 'Default'}</span>
                        </p>
                        <div>
                          {provider.isConfigured ? (
                            <Badge className="bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold shadow-none">
                              Configured ({provider.apiKeyPreview})
                            </Badge>
                          ) : (
                            <Badge className="bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 rounded-full px-2 py-0.5 text-[10px] font-semibold shadow-none">
                              Not Configured
                            </Badge>
                          )}
                        </div>
                      </div>
                    </label>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          {/* Card 2: Settings for Selected Provider */}
          <Card className="border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 shadow-2xs">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg font-bold text-[#050505] dark:text-zinc-50 flex items-center gap-2">
                <Key className="h-5 w-5 text-[#1877F2]" />
                Provider Settings ({activeProvider})
              </CardTitle>
              <CardDescription className="text-xs text-[#65676B] dark:text-zinc-400">
                Configure authentication keys and rate limits for {activeProvider}.
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-5">
              {/* API Key */}
              <div className="space-y-1.5">
                <Label htmlFor="apiKey" className="text-sm font-semibold text-[#050505] dark:text-zinc-200">
                  API Key
                </Label>
                <Input
                  type="password"
                  id="apiKey"
                  {...register('apiKey')}
                  placeholder="Leave blank to keep existing configured key (sk-...)"
                  className="!h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2]"
                />
                <p className="text-xs text-[#65676B] dark:text-zinc-400">
                  This key will be securely saved in the encrypted database and used for {activeProvider} requests.
                </p>
              </div>

              {/* Rate Limit */}
              <div className="space-y-1.5">
                <Label htmlFor="requestsPerMinute" className="text-sm font-semibold text-[#050505] dark:text-zinc-200 flex items-center gap-1.5">
                  <Gauge className="h-4 w-4 text-[#1877F2]" />
                  Global Rate Limit (Requests per minute per user)
                </Label>
                <Input
                  type="number"
                  id="requestsPerMinute"
                  {...register('requestsPerMinute', { valueAsNumber: true, min: 1 })}
                  className="!h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] max-w-xs"
                />
                <p className="text-xs text-[#65676B] dark:text-zinc-400">
                  Limits the maximum number of AI requests a single IP or user account can issue in 1 minute to protect quota limits.
                </p>
              </div>

              {/* Actions & Connection Test Footer inside Card */}
              <div className="pt-4 border-t border-[#CED0D4] dark:border-zinc-800 flex flex-col sm:flex-row items-center justify-between gap-4">
                <div className="flex items-center space-x-3 w-full sm:w-auto">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleTestConnection}
                    disabled={isTesting}
                    className="border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-200 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 cursor-pointer h-10 gap-2 w-full sm:w-auto"
                  >
                    {isTesting ? (
                      <Loader2 className="animate-spin h-4 w-4" />
                    ) : (
                      <Activity className="h-4 w-4 text-[#1877F2]" />
                    )}
                    Test Connection
                  </Button>

                  {testResult && (
                    <div
                      className={`flex items-center text-xs font-semibold ${
                        testResult.success
                          ? 'text-emerald-600 dark:text-emerald-400'
                          : 'text-rose-600 dark:text-rose-400'
                      }`}
                    >
                      {testResult.success ? (
                        <CheckCircle2 className="h-4 w-4 mr-1 shrink-0" />
                      ) : (
                        <XCircle className="h-4 w-4 mr-1 shrink-0" />
                      )}
                      {testResult.success ? `Connected (${testResult.ms}ms)` : 'Connection Failed'}
                    </div>
                  )}
                </div>

                <Button
                  type="submit"
                  disabled={isSaving}
                  className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-5 rounded-lg shadow-2xs active:scale-[0.98] transition-all gap-2 cursor-pointer w-full sm:w-auto"
                >
                  {isSaving ? (
                    <Loader2 className="animate-spin h-4 w-4" />
                  ) : (
                    <Save className="h-4 w-4" />
                  )}
                  Save Configuration
                </Button>
              </div>
            </CardContent>
          </Card>
        </form>

        {/* Failure Details Card */}
        {testResult && !testResult.success && (
          <div className="bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800/60 rounded-xl p-4 transition-all">
            <div className="flex items-start gap-3">
              <XCircle className="h-5 w-5 text-rose-600 dark:text-rose-400 shrink-0 mt-0.5" />
              <div>
                <h3 className="text-sm font-bold text-rose-800 dark:text-rose-300">
                  Connection Error Details
                </h3>
                <p className="text-xs text-rose-700 dark:text-rose-400 mt-1 font-mono">
                  {testResult.message}
                </p>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
