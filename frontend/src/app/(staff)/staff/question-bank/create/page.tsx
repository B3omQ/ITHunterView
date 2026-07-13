"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuestionBank } from "@/hooks/useQuestionBank";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Loader2, ArrowLeft } from "lucide-react";
import Link from "next/link";

export default function CreateQuestionPage() {
  const router = useRouter();
  const { importExcel } = useQuestionBank();

  const [formData, setFormData] = useState<{industry: string; level: string; file: File | null}>({
    industry: "BA",
    level: "INTERN_FRESHER",
    file: null,
  });

  const [formError, setFormError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError("");

    if (!formData.industry || !formData.level || !formData.file) {
      setFormError("All fields are required, including the Excel file.");
      return;
    }

    setIsSubmitting(true);
    setFormError("");
    setSuccessMsg("");

    try {
      const res = await importExcel(formData.industry, formData.level, formData.file);
      if (res.success) {
        setSuccessMsg(`Successfully imported ${res.importedCount} questions.`);
        setTimeout(() => {
          router.push("/staff/question-bank");
        }, 1500);
      } else {
        setFormError(res.message || "Failed to import questions");
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

            <div className="space-y-2">
              <Label htmlFor="fileUpload">Question Excel File <span className="text-red-500">*</span></Label>
              <Input
                id="fileUpload"
                type="file"
                accept=".xlsx"
                onChange={(e) => setFormData({ ...formData, file: e.target.files?.[0] || null })}
                className="bg-white dark:bg-zinc-950 cursor-pointer"
              />
              <p className="text-xs text-zinc-500 mt-1">Upload an Excel (.xlsx) file containing questions in the first column (Column A). Row 1 is skipped as header.</p>
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
