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
import { useTranslations } from 'next-intl';

export default function CreateSystemNotificationPage() {
  const t = useTranslations('StaffNotifications');
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
      toast.error(t('emptyError'));
      return;
    }

    setIsSubmitting(true);

    try {
      await notificationService.createSystemWideNotification(formData);
      toast.success(t('createSuccess'));
      
      // Invalidate the list so it fetches fresh data when redirected
      queryClient.invalidateQueries({ queryKey: ['system-notifications'] });
      
      router.push("/staff/notifications"); // Navigate back to dashboard or notifications list
    } catch (err: any) {
      toast.error(err.response?.data?.message || t('createError'));
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
          <h1 className="text-3xl font-bold tracking-tight">{t('titleCreate')}</h1>
          <p className="text-muted-foreground">
            {t('descCreate')}
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-8 bg-card p-6 rounded-lg border shadow-sm">
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="title">{t('titleLabel')} <span className="text-destructive">*</span></Label>
            <Input
              id="title"
              placeholder={t('titlePlaceholder')}
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              disabled={isSubmitting}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="message">{t('messageLabel')} <span className="text-destructive">*</span></Label>
            <Textarea
              id="message"
              placeholder={t('messagePlaceholder')}
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
            {t('submitBtn')}
          </Button>
        </div>
      </form>
    </div>
    </div>
  );
}
