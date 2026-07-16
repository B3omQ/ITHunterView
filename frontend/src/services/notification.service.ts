import api from './api-client';
import type { ApiResponse } from '@/types/api.types';

export interface CreateSystemNotificationDto {
  title: string;
  message: string;
  type: string;
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
}

export interface PaginatedDataResponse<T> {
  data: T[];
  total: number;
  pageIndex: number;
  pageSize: number;
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

  getSystemNotifications: (pageIndex = 1, pageSize = 10) =>
    api
      .get<PaginatedDataResponse<SystemNotificationDto>>(`/api/notifications/system-wide?pageIndex=${pageIndex}&pageSize=${pageSize}`)
      .then((res) => res.data),

  deleteSystemNotification: (title: string, message: string) =>
    api
      .delete<ApiResponse<boolean>>(`/api/notifications/system-wide`, { params: { title, message } })
      .then((res) => res.data),
};
