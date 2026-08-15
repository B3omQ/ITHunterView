import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"
import axios from "axios"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(value);
}

export function getErrorMessage(error: unknown, fallbackMessage = "Đã xảy ra lỗi. Vui lòng thử lại."): string {
  if (axios.isAxiosError(error)) {
    const serverMessage = error.response?.data?.message || error.response?.data?.Message
    if (serverMessage && typeof serverMessage === 'string') return serverMessage
  }

  if (error instanceof Error && error.message && !error.message.startsWith('Request failed with status code')) {
    return error.message
  }

  return fallbackMessage
}
