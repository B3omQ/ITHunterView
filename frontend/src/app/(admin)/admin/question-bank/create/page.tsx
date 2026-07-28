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

export default function AdminCreateQuestionPage() {
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
      setFormError("Industry and Level are required.");
      return;
    }

    if (!hasFile && !hasText) {
      setFormError("You must provide EITHER an Excel file OR enter a manual question.");
      return;
    }

    if (hasFile && hasText) {
      setFormError("You can only choose ONE method: either upload an Excel file OR enter a manual question, not both.");
      return;
    }

    setIsSubmitting(true);
    setFormError("");
    setSuccessMsg("");

    try {
      if (hasFile) {
        const res = await importExcel(formData.industry, formData.level, formData.file!);
        if (res.success) {
          setSuccessMsg(`Successfully imported ${res.importedCount} questions.`);
          setTimeout(() => {
            router.push("/admin/question-bank");
          }, 1500);
        } else {
          setFormError(res.message || "Failed to import questions");
        }
      } else {
        const res = await createQuestion({
          industry: formData.industry,
          level: formData.level,
          questionText: formData.questionText.trim(),
        });
        if (res.success) {
          setSuccessMsg(`Successfully added the question.`);
          setTimeout(() => {
            router.push("/admin/question-bank");
          }, 1500);
        } else {
          setFormError(res.message || "Failed to add question");
        }
      }
    } catch (err) {
      setFormError("An unexpected error occurred");
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
            <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50 tracking-tight">Add / Import Questions (Admin)</h1>
            <p className="text-zinc-500 dark:text-zinc-400 mt-1 text-sm">Upload an Excel file or add single question manually</p>
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
                <Label htmlFor="industry">Industry <span className="text-red-500">*</span></Label>
                <Select value={formData.industry} onValueChange={(v) => setFormData({ ...formData, industry: v || "" })}>
                  <SelectTrigger id="industry" className="bg-white dark:bg-zinc-950">
                    <SelectValue placeholder="Select Industry" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="BA">BA</SelectItem>
                    <SelectItem value="DEV">Dev</SelectItem>
                    <SelectItem value="TEST">Test</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="level">Level <span className="text-red-500">*</span></Label>
                <Select value={formData.level} onValueChange={(v) => setFormData({ ...formData, level: v || "" })}>
                  <SelectTrigger id="level" className="bg-white dark:bg-zinc-950">
                    <SelectValue placeholder="Select Level" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="INTERN_FRESHER">Intern/Fresher</SelectItem>
                    <SelectItem value="JUNIOR">Junior</SelectItem>
                    <SelectItem value="MIDDLE">Middle</SelectItem>
                    <SelectItem value="SENIOR">Senior</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-4 pt-4 border-t border-zinc-100 dark:border-zinc-800">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">Method 1: Import Excel File</h3>
              <div className="space-y-2">
                <Label htmlFor="excelFile">Excel File (.xlsx)</Label>
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
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">Method 2: Single Manual Question</h3>
              <div className="space-y-2">
                <Label htmlFor="questionText">Question Content</Label>
                <Textarea
                  id="questionText"
                  placeholder="Enter the single question content..."
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
                  Cancel
                </Button>
              </Link>
              <Button type="submit" disabled={isSubmitting} className="bg-blue-600 hover:bg-blue-700 text-white min-w-[120px]">
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                Submit
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
