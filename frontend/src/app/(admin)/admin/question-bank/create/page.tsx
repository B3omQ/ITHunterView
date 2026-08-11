"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuestionBank } from "@/hooks/useQuestionBank";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Loader2, ArrowLeft } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";

export default function AdminCreateQuestionPage() {
  const t = useTranslations("AdminQuestionBank");
  const router = useRouter();
  const { importExcel, createQuestion } = useQuestionBank();

  const [formData, setFormData] = useState<{industry: string; level: string; file: File | null; questionText: string}>({
    industry: "BA",
    level: "INTERN_FRESHER",
    file: null,
    questionText: "",
  });

  const [formError, setFormError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const hasFile = !!formData.file;
  const hasText = !!formData.questionText.trim();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError("");

    if (!formData.industry || !formData.level) {
      setFormError(t('industryRequired'));
      return;
    }

    if (!hasFile && !hasText) {
      setFormError(t('eitherFileOrText'));
      return;
    }

    if (hasFile && hasText) {
      setFormError(t('onlyOneMethod'));
      return;
    }

    setIsSubmitting(true);
    setFormError("");
    setSuccessMsg("");

    try {
      if (hasFile) {
        const res = await importExcel(formData.industry, formData.level, formData.file!);
        if (res.success) {
          setSuccessMsg(t('importSuccess').replace('{count}', (res.importedCount ?? 0).toString()));
          setTimeout(() => {
            router.push("/admin/question-bank");
          }, 1500);
        } else {
          setFormError(res.message || t('importFail'));
        }
      } else {
        const res = await createQuestion({
          industry: formData.industry,
          level: formData.level,
          questionText: formData.questionText.trim(),
        });
        if (res.success) {
          setSuccessMsg(t('addSuccess'));
          setTimeout(() => {
            router.push("/admin/question-bank");
          }, 1500);
        } else {
          setFormError(res.message || t('addFail'));
        }
      }
    } catch (err) {
      setFormError(t('unexpectedError'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-8 space-y-6">
        <div className="flex items-center gap-4 py-2">
          <Link href="/admin/question-bank">
            <Button variant="ghost" size="icon" className="h-8 w-8 text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-50">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50 tracking-tight">{t('createTitleAdmin')}</h1>
            <p className="text-zinc-500 dark:text-zinc-400 mt-1 text-sm">{t('createDescAdmin')}</p>
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl shadow-sm p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            {formError && (
              <div className="p-3 text-sm text-red-600 bg-red-50 rounded-md dark:bg-red-900/30 dark:text-red-400">
                {formError}
              </div>
            )}
            {successMsg && (
              <div className="p-3 text-sm text-green-600 bg-green-50 rounded-md dark:bg-green-900/30 dark:text-green-400">
                {successMsg}
              </div>
            )}

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
              <div className="space-y-2">
                <Label htmlFor="industry">{t('industryLabel')} <span className="text-red-500">*</span></Label>
                <Select value={formData.industry} onValueChange={(v) => setFormData({ ...formData, industry: v || "" })}>
                  <SelectTrigger id="industry" className="bg-white dark:bg-zinc-950">
                    <SelectValue placeholder={t('selectIndustry')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="BA">{t('industryBA')}</SelectItem>
                    <SelectItem value="DEV">{t('industryDev')}</SelectItem>
                    <SelectItem value="TEST">{t('industryTest')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="level">{t('levelLabel')} <span className="text-red-500">*</span></Label>
                <Select value={formData.level} onValueChange={(v) => setFormData({ ...formData, level: v || "" })}>
                  <SelectTrigger id="level" className="bg-white dark:bg-zinc-950">
                    <SelectValue placeholder={t('selectLevel')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="INTERN_FRESHER">{t('levelInternFresher')}</SelectItem>
                    <SelectItem value="JUNIOR">{t('levelJunior')}</SelectItem>
                    <SelectItem value="MIDDLE">{t('levelMiddle')}</SelectItem>
                    <SelectItem value="SENIOR">{t('levelSenior')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-4 pt-4 border-t border-zinc-100 dark:border-zinc-800">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{t('method1')}</h3>
              <div className="space-y-2">
                <Label htmlFor="excelFile">{t('excelFileShort')}</Label>
                <Input
                  id="excelFile"
                  type="file"
                  accept=".xlsx, .xls"
                  disabled={hasText}
                  onChange={(e) => {
                    const selectedFile = e.target.files?.[0] || null;
                    setFormData({ ...formData, file: selectedFile });
                  }}
                  className="bg-white dark:bg-zinc-950 cursor-pointer"
                />
              </div>
            </div>

            <div className="space-y-4 pt-4 border-t border-zinc-100 dark:border-zinc-800">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{t('method2')}</h3>
              <div className="space-y-2">
                <Label htmlFor="questionText">{t('questionContent')}</Label>
                <Textarea
                  id="questionText"
                  placeholder={t('manualPlaceholderAdmin')}
                  rows={4}
                  disabled={hasFile}
                  value={formData.questionText}
                  onChange={(e) => setFormData({ ...formData, questionText: e.target.value })}
                  className="resize-y bg-white dark:bg-zinc-950"
                />
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t border-zinc-100 dark:border-zinc-800">
              <Link href="/admin/question-bank">
                <Button type="button" variant="outline">
                  {t('cancelBtn')}
                </Button>
              </Link>
              <Button type="submit" disabled={isSubmitting} className="bg-blue-600 hover:bg-blue-700 text-white min-w-[120px]">
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                {t('submitBtn')}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
