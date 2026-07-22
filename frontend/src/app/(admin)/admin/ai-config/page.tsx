"use client";

import React, { useState, useEffect } from "react";
import { useForm } from "react-hook-form";
import { Save, Activity, CheckCircle, XCircle, Loader2 } from "lucide-react";
import { aiConfigService, AiConfigResponse, UpdateAiConfigRequest } from "@/services/ai-config.service";
import { toast } from "sonner";

export default function AiConfigPage() {
    const [config, setConfig] = useState<AiConfigResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [isTesting, setIsTesting] = useState(false);
    const [testResult, setTestResult] = useState<{ success: boolean; message: string; ms: number } | null>(null);

    const { register, handleSubmit, setValue, watch } = useForm<UpdateAiConfigRequest>({
        defaultValues: {
            providerName: "Gemini",
            requestsPerMinute: 60,
            apiKey: ""
        }
    });

    const activeProvider = watch("providerName");

    const loadConfig = async () => {
        setIsLoading(true);
        try {
            const res = await aiConfigService.getConfigs();
            if (res.data) {
                setConfig(res.data);
                setValue("providerName", res.data.activeProvider);
                setValue("requestsPerMinute", res.data.requestsPerMinute);
            }
        } catch (error: any) {
            toast.error(error?.response?.data?.message || "Failed to load AI config");
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
            toast.success("AI Configuration saved successfully!");
            // clear api key field after saving
            setValue("apiKey", "");
            await loadConfig();
        } catch (error: any) {
            toast.error(error?.response?.data?.message || "Failed to save AI config");
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
                message: res.data?.message || "Success",
                ms: res.data?.responseTimeMs ?? 0
            });
            if (res.data?.success) {
                toast.success("Test connection successful!");
            } else {
                toast.error("Test connection failed!");
            }
        } catch (error: any) {
            setTestResult({
                success: false,
                message: error?.response?.data?.message || "Test connection failed due to an error.",
                ms: 0
            });
            toast.error("Test connection failed!");
        } finally {
            setIsTesting(false);
        }
    };

    if (isLoading) {
        return (
            <div className="flex h-64 items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin text-indigo-600" />
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
            <div className="mb-8">
                <h1 className="text-2xl font-bold text-gray-900 dark:text-white">AI Configuration</h1>
                <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    Manage the active AI model, API keys, and rate limits for the system.
                </p>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                <div className="bg-white dark:bg-gray-800 shadow rounded-lg overflow-hidden">
                    <div className="px-4 py-5 sm:p-6">
                        <h3 className="text-lg leading-6 font-medium text-gray-900 dark:text-white">
                            Active Provider
                        </h3>
                        <div className="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-4">
                            {config?.availableProviders.map((provider) => (
                                <div key={provider.providerName} className="relative flex items-start">
                                    <div className="flex items-center h-5">
                                        <input
                                            id={provider.providerName}
                                            type="radio"
                                            value={provider.providerName}
                                            {...register("providerName")}
                                            className="focus:ring-indigo-500 h-4 w-4 text-indigo-600 border-gray-300 dark:border-gray-600 dark:bg-gray-700"
                                        />
                                    </div>
                                    <div className="ml-3 text-sm">
                                        <label htmlFor={provider.providerName} className="font-medium text-gray-700 dark:text-gray-300">
                                            {provider.providerName}
                                        </label>
                                        <p className="text-gray-500 dark:text-gray-400">Model: {provider.model || 'Default'}</p>
                                        {provider.isConfigured ? (
                                            <span className="inline-flex items-center mt-1 px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
                                                Configured ({provider.apiKeyPreview})
                                            </span>
                                        ) : (
                                            <span className="inline-flex items-center mt-1 px-2 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">
                                                Not Configured
                                            </span>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>

                <div className="bg-white dark:bg-gray-800 shadow rounded-lg overflow-hidden">
                    <div className="px-4 py-5 sm:p-6 space-y-6">
                        <h3 className="text-lg leading-6 font-medium text-gray-900 dark:text-white">
                            Settings for {activeProvider}
                        </h3>

                        <div>
                            <label htmlFor="apiKey" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                                API Key (Leave blank to keep existing)
                            </label>
                            <div className="mt-1">
                                <input
                                    type="password"
                                    id="apiKey"
                                    {...register("apiKey")}
                                    className="shadow-sm focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-gray-300 rounded-md dark:bg-gray-700 dark:border-gray-600 dark:text-white"
                                    placeholder="sk-..."
                                />
                            </div>
                            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
                                This key will be securely saved in the database and used for {activeProvider}.
                            </p>
                        </div>

                        <div>
                            <label htmlFor="requestsPerMinute" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                                Global Rate Limit (Requests per minute per user)
                            </label>
                            <div className="mt-1">
                                <input
                                    type="number"
                                    id="requestsPerMinute"
                                    {...register("requestsPerMinute", { valueAsNumber: true, min: 1 })}
                                    className="shadow-sm focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-gray-300 rounded-md dark:bg-gray-700 dark:border-gray-600 dark:text-white"
                                />
                            </div>
                            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
                                Limits the number of AI requests a single IP or user can make in one minute to prevent abuse.
                            </p>
                        </div>
                    </div>
                    <div className="px-4 py-3 bg-gray-50 dark:bg-gray-700/50 text-right sm:px-6 flex justify-between items-center">
                        <div className="flex items-center space-x-4">
                            <button
                                type="button"
                                onClick={handleTestConnection}
                                disabled={isTesting}
                                className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 shadow-sm text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
                            >
                                {isTesting ? (
                                    <Loader2 className="animate-spin -ml-1 mr-2 h-4 w-4" />
                                ) : (
                                    <Activity className="-ml-1 mr-2 h-4 w-4 text-gray-500 dark:text-gray-400" />
                                )}
                                Test Connection
                            </button>
                            {testResult && (
                                <div className={`flex items-center text-sm ${testResult.success ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                                    {testResult.success ? (
                                        <CheckCircle className="h-4 w-4 mr-1" />
                                    ) : (
                                        <XCircle className="h-4 w-4 mr-1" />
                                    )}
                                    {testResult.success ? `Success (${testResult.ms}ms)` : 'Failed'}
                                </div>
                            )}
                        </div>
                        
                        <button
                            type="submit"
                            disabled={isSaving}
                            className="inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
                        >
                            {isSaving ? (
                                <Loader2 className="animate-spin -ml-1 mr-2 h-4 w-4" />
                            ) : (
                                <Save className="-ml-1 mr-2 h-4 w-4" />
                            )}
                            Save Configuration
                        </button>
                    </div>
                </div>
            </form>
            
            {testResult && !testResult.success && (
                <div className="mt-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-md p-4">
                    <div className="flex">
                        <div className="flex-shrink-0">
                            <XCircle className="h-5 w-5 text-red-400" aria-hidden="true" />
                        </div>
                        <div className="ml-3">
                            <h3 className="text-sm font-medium text-red-800 dark:text-red-300">Connection Error</h3>
                            <div className="mt-2 text-sm text-red-700 dark:text-red-400">
                                <p>{testResult.message}</p>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
