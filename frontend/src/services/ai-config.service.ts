import apiClient from './api-client';
import { ApiResponse } from '@/types/api.types';

export interface AiProviderConfig {
    providerName: string;
    model: string;
    isConfigured: boolean;
    apiKeyPreview: string;
}

export interface AiConfigResponse {
    activeProvider: string;
    requestsPerMinute: number;
    availableProviders: AiProviderConfig[];
}

export interface UpdateAiConfigRequest {
    providerName: string;
    requestsPerMinute: number;
    apiKey?: string;
}

export interface TestConnectionRequest {
    providerName: string;
    prompt?: string;
}

export interface TestConnectionResponse {
    success: boolean;
    message: string;
    responseText: string;
    responseTimeMs: number;
}

export interface AiUsageFilter {
    fromDate?: string;
    toDate?: string;
    providerName?: string;
    featureCode?: string;
    page?: number;
    pageSize?: number;
}

export interface ProviderUsageBreakdown {
    providerName: string;
    totalTokens: number;
    estimatedCostUsd: number;
    requestCount: number;
    percentage: number;
}

export interface FeatureUsageBreakdown {
    featureCode: string;
    featureName: string;
    totalTokens: number;
    estimatedCostUsd: number;
    requestCount: number;
}

export interface AiUsageLogItem {
    id: string;
    createdAt: string;
    providerName: string;
    model: string;
    featureCode: string;
    userEmail: string;
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
    estimatedCostUsd: number;
    latencyMs: number;
    status: string;
}

export interface AiUsageSummaryResponse {
    totalTokens: number;
    promptTokens: number;
    completionTokens: number;
    totalEstimatedCostUsd: number;
    totalRequests: number;
    avgLatencyMs: number;
    providerBreakdown: ProviderUsageBreakdown[];
    featureBreakdown: FeatureUsageBreakdown[];
    logs: AiUsageLogItem[];
    page: number;
    pageSize: number;
    totalLogRecords: number;
    totalPages: number;
}

export const aiConfigService = {
    getConfigs: async () => {
        const response = await apiClient.get<ApiResponse<AiConfigResponse>>('/api/ai/configs');
        return response.data;
    },

    updateConfig: async (data: UpdateAiConfigRequest) => {
        const response = await apiClient.post<ApiResponse<string>>('/api/ai/configs/update', data);
        return response.data;
    },

    testConnection: async (data: TestConnectionRequest) => {
        const response = await apiClient.post<ApiResponse<TestConnectionResponse>>('/api/ai/test-connection', data);
        return response.data;
    },

    getUsageAnalytics: async (filter?: AiUsageFilter) => {
        const response = await apiClient.get<ApiResponse<AiUsageSummaryResponse>>('/api/ai/usage', { params: filter });
        return response.data;
    }
};
