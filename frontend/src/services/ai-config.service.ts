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
    }
};
