"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Loader2, ArrowLeft, Users, Shield, Mail, Globe } from "lucide-react";
import { notificationService, CreateSystemNotificationDto } from "@/services/notification.service";
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';

export default function AdminCreateSystemNotificationPage() {
  const t = useTranslations('AdminNotifications');
  const router = useRouter();
  const queryClient = useQueryClient();

  const [formData, setFormData] = useState<CreateSystemNotificationDto>({
    title: "",
    message: "",
    type: "SYSTEM",
    targetType: "ALL",
    targetRole: "candidate",
    targetEmails: [],
  });

  const [emailInput, setEmailInput] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.title.trim() || !formData.message.trim()) {
      toast.error(t('emptyError'));
      return;
    }

    let parsedEmails: string[] = [];
    if (formData.targetType === "CUSTOM") {
      parsedEmails = emailInput
        .split(/[\n,;]+/)
        .map((e) => e.trim())
        .filter((e) => e.length > 0);

      if (parsedEmails.length === 0) {
        toast.error("Vui lòng nhập ít nhất 1 email hợp lệ.");
        return;
      }
    }

    setIsSubmitting(true);

    try {
      const payload: CreateSystemNotificationDto = {
        ...formData,
        targetEmails: formData.targetType === "CUSTOM" ? parsedEmails : undefined,
      };

      await notificationService.createSystemWideNotification(payload);
      toast.success(t('createSuccess'));
      
      queryClient.invalidateQueries({ queryKey: ['system-notifications'] });
      router.push("/admin/notifications");
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
          <Button variant="outline" size="icon" onClick={() => router.push("/admin/notifications")}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">{t('titleCreate')}</h1>
            <p className="text-muted-foreground">
              {t('descCreate')}
            </p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6 bg-card p-6 rounded-xl border shadow-xs">
          <div className="space-y-4">
            {/* Title */}
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

            {/* Target Selection */}
            <div className="space-y-2">
              <Label>{t('targetTypeLabel')}</Label>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <button
                  type="button"
                  onClick={() => setFormData({ ...formData, targetType: "ALL" })}
                  className={`p-3 rounded-xl border text-left flex items-start gap-2.5 transition-all ${
                    formData.targetType === "ALL"
                      ? "border-primary bg-primary/5 font-semibold text-primary"
                      : "border-border hover:bg-muted text-muted-foreground"
                  }`}
                >
                  <Globe className="h-4 w-4 mt-0.5 shrink-0" />
                  <div className="text-xs">
                    <div className="font-bold text-foreground">Tất cả</div>
                    <div className="text-[11px] text-muted-foreground">Toàn bộ người dùng</div>
                  </div>
                </button>

                <button
                  type="button"
                  onClick={() => setFormData({ ...formData, targetType: "ROLE" })}
                  className={`p-3 rounded-xl border text-left flex items-start gap-2.5 transition-all ${
                    formData.targetType === "ROLE"
                      ? "border-primary bg-primary/5 font-semibold text-primary"
                      : "border-border hover:bg-muted text-muted-foreground"
                  }`}
                >
                  <Shield className="h-4 w-4 mt-0.5 shrink-0" />
                  <div className="text-xs">
                    <div className="font-bold text-foreground">Theo Vai trò</div>
                    <div className="text-[11px] text-muted-foreground">Ứng viên / NTD / Staff</div>
                  </div>
                </button>

                <button
                  type="button"
                  onClick={() => setFormData({ ...formData, targetType: "CUSTOM" })}
                  className={`p-3 rounded-xl border text-left flex items-start gap-2.5 transition-all ${
                    formData.targetType === "CUSTOM"
                      ? "border-primary bg-primary/5 font-semibold text-primary"
                      : "border-border hover:bg-muted text-muted-foreground"
                  }`}
                >
                  <Mail className="h-4 w-4 mt-0.5 shrink-0" />
                  <div className="text-xs">
                    <div className="font-bold text-foreground">Danh sách Email</div>
                    <div className="text-[11px] text-muted-foreground">Chọn cụ thể từng người</div>
                  </div>
                </button>
              </div>
            </div>

            {/* Role selection dropdown if ROLE is selected */}
            {formData.targetType === "ROLE" && (
              <div className="space-y-2 p-3 bg-muted/30 border rounded-xl animate-in fade-in">
                <Label>{t('selectRoleLabel')}</Label>
                <select
                  value={formData.targetRole}
                  onChange={(e) => setFormData({ ...formData, targetRole: e.target.value })}
                  className="w-full py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary"
                >
                  <option value="candidate">{t('roleCandidate')}</option>
                  <option value="recruiter">{t('roleRecruiter')}</option>
                  <option value="staff">{t('roleStaff')}</option>
                </select>
              </div>
            )}

            {/* Custom Emails textarea if CUSTOM is selected */}
            {formData.targetType === "CUSTOM" && (
              <div className="space-y-2 p-3 bg-muted/30 border rounded-xl animate-in fade-in">
                <Label>{t('emailsLabel')}</Label>
                <Textarea
                  placeholder={t('emailsPlaceholder')}
                  className="min-h-[90px] font-mono text-xs"
                  value={emailInput}
                  onChange={(e) => setEmailInput(e.target.value)}
                />
              </div>
            )}

            {/* Message */}
            <div className="space-y-2">
              <Label htmlFor="message">{t('messageLabel')} <span className="text-destructive">*</span></Label>
              <Textarea
                id="message"
                placeholder={t('messagePlaceholder')}
                className="min-h-[140px]"
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
