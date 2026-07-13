"use client";

import { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import { useQuestionBank } from "@/hooks/useQuestionBank";
import { questionBankService } from "@/services/question-bank.service";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Loader2, ArrowLeft } from "lucide-react";
import Link from "next/link";

export default function EditQuestionPage() {
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
          setFormError(res.message || "Failed to load question details");
        }
      } catch (err: any) {
        setFormError(err.message || "An error occurred");
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
      setFormError("All fields are required.");
      return;
    }

    setIsSubmitting(true);
    const res = await updateQuestion(questionId, formData);
    setIsSubmitting(false);

    if (res.success) {
      router.push("/staff/question-bank");
    } else {
      setFormError(res.message || "An error occurred.");
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
            <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50 tracking-tight">Edit Question</h1>
            <p className="text-zinc-500 dark:text-zinc-400 mt-1 text-sm">Update sample interview question details</p>
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

              <div className="space-y-2">
                <Label htmlFor="questionText">Question Content <span className="text-red-500">*</span></Label>
                <Textarea
                  id="questionText"
                  placeholder="Enter the full question..."
                  rows={4}
                  value={formData.questionText}
                  onChange={(e) => setFormData({ ...formData, questionText: e.target.value })}
                  className="resize-y bg-white dark:bg-zinc-950"
                />
              </div>

              <div className="flex justify-end gap-3 pt-4 border-t border-zinc-100 dark:border-zinc-800">
                <Link href="/staff/question-bank">
                  <Button type="button" variant="outline">
                    Cancel
                  </Button>
                </Link>
                <Button type="submit" disabled={isSubmitting} className="bg-blue-600 hover:bg-blue-700 text-white min-w-[120px]">
                  {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                  Save Changes
                </Button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
