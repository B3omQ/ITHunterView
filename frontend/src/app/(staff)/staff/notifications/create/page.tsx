"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Loader2, ArrowLeft } from "lucide-react";
import { notificationService, CreateSystemNotificationDto } from "@/services/notification.service";
import { toast } from 'sonner';

export default function CreateSystemNotificationPage() {
  const router = useRouter();
  const queryClient = useQueryClient();

  const [formData, setFormData] = useState<CreateSystemNotificationDto>({
    title: "",
    message: "",
    type: "SYSTEM",
  });

  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.title.trim() || !formData.message.trim()) {
      toast.error("Tiêu đề và Nội dung không được để trống.");
      return;
    }

    setIsSubmitting(true);

    try {
      await notificationService.createSystemWideNotification(formData);
      toast.success("Đã gửi thông báo hệ thống thành công.");
      
      // Invalidate the list so it fetches fresh data when redirected
      queryClient.invalidateQueries({ queryKey: ['system-notifications'] });
      
      router.push("/staff/notifications"); // Navigate back to dashboard or notifications list
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Đã xảy ra lỗi khi gửi thông báo.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="w-full pb-8">
      <div className="space-y-6 max-w-2xl mx-auto">
      <div className="flex items-center gap-4">
        <Button variant="outline" size="icon" onClick={() => router.push("/staff/notifications")}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Tạo Thông Báo Hệ Thống</h1>
          <p className="text-muted-foreground">
            Gửi thông báo đến tất cả ứng viên và nhà tuyển dụng.
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-8 bg-card p-6 rounded-lg border shadow-sm">
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="title">Tiêu đề thông báo <span className="text-destructive">*</span></Label>
            <Input
              id="title"
              placeholder="VD: Cập nhật hệ thống v2.0"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              disabled={isSubmitting}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="message">Nội dung <span className="text-destructive">*</span></Label>
            <Textarea
              id="message"
              placeholder="Nhập nội dung chi tiết của thông báo..."
              className="min-h-[150px]"
              value={formData.message}
              onChange={(e) => setFormData({ ...formData, message: e.target.value })}
              disabled={isSubmitting}
            />
          </div>
        </div>

        <div className="flex justify-end">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Gửi Thông Báo
          </Button>
        </div>
      </form>
    </div>
    </div>
  );
}
