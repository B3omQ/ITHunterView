import api from './api-client';
import type { ApiResponse, PaginatedDataResponse } from '@/types/api.types';

export interface CreateSystemNotificationDto {
  title: string;
  message: string;
  type: string;
  targetType?: 'ALL' | 'ROLE' | 'USER' | 'CUSTOM';
  targetRole?: string;
  targetUserIds?: string[];
  targetEmails?: string[];
}

export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

export interface SystemNotificationDto {
  title: string;
  message: string;
  createdAt: string;
  isHidden: boolean;
}


export const notificationService = {
  createSystemWideNotification: (data: CreateSystemNotificationDto) =>
    api
      .post<ApiResponse<boolean>>('/api/notifications/system-wide', data)
      .then((res) => res.data),

  getUserNotifications: (pageIndex = 1, pageSize = 10) =>
    api
      .get<PaginatedDataResponse<NotificationDto>>(`/api/notifications?pageIndex=${pageIndex}&pageSize=${pageSize}`)
      .then((res) => res.data),

  markAsRead: (id: string) =>
    api
      .put<ApiResponse<boolean>>(`/api/notifications/${id}/read`)
      .then((res) => res.data),

  getSystemNotifications: (pageIndex = 1, pageSize = 10, searchTerm?: string) =>
    api
      .get<PaginatedDataResponse<SystemNotificationDto>>(`/api/notifications/system-wide`, {
        params: { pageIndex, pageSize, searchTerm }
      })
      .then((res) => res.data),

  deleteSystemNotification: (title: string, message: string) =>
    api
      .delete<ApiResponse<boolean>>(`/api/notifications/system-wide`, { params: { title, message } })
      .then((res) => res.data),
};
