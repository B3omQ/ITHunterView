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

export default function CreateQuestionPage() {
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
            router.push("/staff/question-bank");
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
            router.push("/staff/question-bank");
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
    <div className="min-h-screen bg-background py-6 px-4 sm:px-6 lg:px-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center gap-4 py-2">
          <Link href="/staff/question-bank">
            <Button variant="ghost" size="icon" className="h-8 w-8 text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-50">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50 tracking-tight">Import Questions</h1>
            <p className="text-zinc-500 dark:text-zinc-400 mt-1 text-sm">Upload an Excel file to add multiple questions</p>
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl shadow-sm p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
              <div className="space-y-2">
                <Label htmlFor="industry">Industry <span className="text-red-500">*</span></Label>
                <Select value={formData.industry} onValueChange={(v) => setFormData({...formData, industry: v || ""})}>
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
                <Select value={formData.level} onValueChange={(v) => setFormData({...formData, level: v || ""})}>
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

            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="questionText" className={hasFile ? "opacity-50" : ""}>Manual Question Entry</Label>
                <Textarea
                  id="questionText"
                  placeholder="Enter the question text here..."
                  value={formData.questionText}
                  onChange={(e) => setFormData({ ...formData, questionText: e.target.value })}
                  className="bg-white dark:bg-zinc-950 min-h-[100px]"
                  disabled={hasFile}
                />
              </div>

              <div className="flex items-center">
                <div className="h-px bg-zinc-200 dark:bg-zinc-800 flex-1"></div>
                <span className="px-4 text-xs font-medium text-zinc-500 uppercase">OR</span>
                <div className="h-px bg-zinc-200 dark:bg-zinc-800 flex-1"></div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="fileUpload" className={hasText ? "opacity-50" : ""}>Question Excel File</Label>
                <div className="flex gap-2 items-center">
                  <Input
                    id="fileUpload"
                    type="file"
                    accept=".xlsx"
                    onChange={(e) => setFormData({ ...formData, file: e.target.files?.[0] || null })}
                    className="bg-white dark:bg-zinc-950 cursor-pointer flex-1"
                    disabled={hasText}
                  />
                  {hasFile && (
                    <Button 
                      type="button" 
                      variant="outline" 
                      onClick={() => {
                        const fileInput = document.getElementById('fileUpload') as HTMLInputElement;
                        if (fileInput) fileInput.value = '';
                        setFormData({ ...formData, file: null });
                      }}
                    >
                      Clear
                    </Button>
                  )}
                </div>
                <p className={`text-xs mt-1 ${hasText ? 'text-zinc-400' : 'text-zinc-500'}`}>Upload an Excel (.xlsx) file containing questions in the first column (Column A). Row 1 is skipped as header.</p>
              </div>
            </div>

            {formError && (
              <div className="p-3 bg-red-50 text-red-700 dark:bg-red-500/10 dark:text-red-400 rounded-md text-sm">
                {formError}
              </div>
            )}
            
            {successMsg && (
              <div className="p-3 bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400 rounded-md text-sm">
                {successMsg}
              </div>
            )}

            <div className="flex items-center justify-end gap-3 pt-6 border-t border-zinc-200 dark:border-zinc-800">
              <Button
                type="button"
                variant="outline"
                onClick={() => router.back()}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={isSubmitting} className="bg-zinc-900 text-white hover:bg-zinc-800 dark:bg-zinc-50 dark:text-zinc-900 dark:hover:bg-zinc-200">
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                {isSubmitting ? "Importing..." : "Import Questions"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
