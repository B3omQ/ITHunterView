"use client";

import { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import { useQuestionBank } from "@/hooks/useQuestionBank";
import { questionBankService } from "@/services/question-bank.service";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Loader2, ArrowLeft } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";

export default function AdminEditQuestionPage() {
  const t = useTranslations("AdminQuestionBank");
  const router = useRouter();
  const params = useParams();
  const questionId = params.id as string;
  const { updateQuestion } = useQuestionBank();

  const [formData, setFormData] = useState({
    industry: "BA",
    level: "INTERN_FRESHER",
    questionText: "",
  });

  const [isLoading, setIsLoading] = useState(true);
  const [formError, setFormError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!questionId) return;

    const fetchQuestion = async () => {
      try {
        setIsLoading(true);
        const res = await questionBankService.getQuestionById(questionId);
        if (res.success && res.data) {
          setFormData({
            industry: res.data.industry || "BA",
            level: res.data.level || "INTERN_FRESHER",
            questionText: res.data.questionText || "",
          });
        } else {
          setFormError(res.message || t('loadFail'));
        }
      } catch (err: any) {
        setFormError(err.message || t('errorOccurred'));
      } finally {
        setIsLoading(false);
      }
    };

    fetchQuestion();
  }, [questionId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError("");

    if (!formData.level || !formData.questionText) {
      setFormError(t('allRequired'));
      return;
    }

    setIsSubmitting(true);
    const res = await updateQuestion(questionId, formData);
    setIsSubmitting(false);

    if (res.success) {
      router.push("/admin/question-bank");
    } else {
      setFormError(res.message || t('errorOccurred'));
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
            <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50 tracking-tight">{t('editTitle')}</h1>
            <p className="text-zinc-500 dark:text-zinc-400 mt-1 text-sm">{t('editDesc')}</p>
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl shadow-sm p-6">
          {isLoading ? (
            <div className="flex justify-center items-center py-20">
              <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-6">
              {formError && (
                <div className="p-3 text-sm text-red-600 bg-red-50 rounded-md dark:bg-red-900/30 dark:text-red-400">
                  {formError}
                </div>
              )}

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                <div className="space-y-2">
                  <Label htmlFor="industry">{t('industryLabel')} <span className="text-red-500">*</span></Label>
                  <Select value={formData.industry} onValueChange={(v) => setFormData({...formData, industry: v || ""})}>
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

              <div className="space-y-2">
                <Label htmlFor="questionText">{t('questionContent')} <span className="text-red-500">*</span></Label>
                <Textarea
                  id="questionText"
                  placeholder={t('questionPlaceholder')}
                  rows={4}
                  value={formData.questionText}
                  onChange={(e) => setFormData({ ...formData, questionText: e.target.value })}
                  className="resize-y bg-white dark:bg-zinc-950"
                />
              </div>

              <div className="flex justify-end gap-3 pt-4 border-t border-zinc-100 dark:border-zinc-800">
                <Link href="/admin/question-bank">
                  <Button type="button" variant="outline">
                    {t('cancelBtn')}
                  </Button>
                </Link>
                <Button type="submit" disabled={isSubmitting} className="bg-blue-600 hover:bg-blue-700 text-white min-w-[120px]">
                  {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                  {isSubmitting ? t('savingBtn') : t('saveBtn')}
                </Button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
