"use client"

import { use } from "react"
import { useRouter } from "next/navigation"
import { useJobDetails, useJobMetadata } from "@/hooks/useJobs"
import {
  useJobAnalysis,
  useRequestJobAnalysis,
  useUpdateJobDecisions,
  useFinalizeJob,
} from "@/hooks/useJobAnalysis"

import { CandidateJobPreview } from "./_components/CandidateJobPreview"
import { SkillReviewPanel } from "./_components/SkillReviewPanel"
import { AnalysisStateCard } from "./_components/AnalysisStateCard"
import { Button } from "@/components/ui/button"
import { ArrowLeft, Send, Edit, AlertTriangle } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"

export default function JobPreviewPage({ params }: { params: Promise<{ jobId: string }> }) {
  const { jobId } = use(params)
  const router = useRouter()

  const { job, loading: jobLoading, error: jobError } = useJobDetails(jobId)
  const { availableSkills } = useJobMetadata()

  const { data: preview, isLoading: previewLoading } = useJobAnalysis(jobId)
  const requestAnalysisMutation = useRequestJobAnalysis(jobId)
  const updateDecisionsMutation = useUpdateJobDecisions(jobId, preview?.analysisRunId || "")
  const finalizeJobMutation = useFinalizeJob(jobId)

  const handleRequestAnalysis = () => {
    if (!job) return
    requestAnalysisMutation.mutate({
      expectedRevision: job.analysisRevision || 1,
    })
  }

  const handleUpdateDecisions = (
    decisions: Array<{
      decisionId: string
      decision: "PENDING" | "ACCEPTED" | "REJECTED"
      resolvedSkillId?: number | null
      importance: string
    }>
  ) => {
    if (!job || !preview) return
    updateDecisionsMutation.mutate({
      expectedJobRevision: job.analysisRevision || 1,
      expectedDecisionVersion: preview.decisionVersion || 1,
      decisions,
    })
  }

  const handleFinalize = () => {
    if (!job || !preview) return
    finalizeJobMutation.mutate(
      {
        analysisRunId: preview.analysisRunId,
        expectedJobRevision: job.analysisRevision || 1,
        expectedDecisionVersion: preview.decisionVersion || 1,
      },
      {
        onSuccess: () => {
          router.push("/recruiter/jobs")
        },
      }
    )
  }

  if (jobLoading) {
    return (
      <div className="container py-8 max-w-6xl mx-auto flex items-center justify-center min-h-[400px]">
        <p className="text-slate-500">Đang tải thông tin tin tuyển dụng...</p>
      </div>
    )
  }

  if (jobError || !job) {
    return (
      <div className="container py-8 max-w-6xl mx-auto">
        <Card className="border-red-200 bg-red-50">
          <CardContent className="p-6">
            <p className="text-red-600 font-medium">Không tìm thấy tin tuyển dụng hoặc xảy ra lỗi.</p>
            <Button className="mt-4" onClick={() => router.push("/recruiter/jobs")}>
              Quay lại danh sách
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="container py-8 max-w-6xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b pb-4">
        <div className="flex items-center gap-3">
          <Button variant="outline" size="sm" onClick={() => router.push("/recruiter/jobs")}>
            <ArrowLeft className="w-4 h-4 mr-1" />
            Danh sách tin
          </Button>
          <div>
            <h1 className="text-xl font-bold text-slate-900">Xem trước & Xuất bản tin tuyển dụng</h1>
            <p className="text-xs text-slate-500">Mã tin: {job.jobCode} • Lượt sửa #{job.analysisRevision}</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => router.push(`/recruiter/jobs/${job.id}/edit`)}>
            <Edit className="w-4 h-4 mr-1" />
            Chỉnh sửa bản thảo
          </Button>

          <Button
            size="sm"
            onClick={handleFinalize}
            disabled={!preview?.canFinalize || finalizeJobMutation.isPending}
            className="bg-emerald-600 hover:bg-emerald-700 text-white font-medium px-4"
          >
            <Send className="w-4 h-4 mr-1.5" />
            {preview?.finalActionLabel || "Xuất bản tin tuyển dụng"}
          </Button>
        </div>
      </div>

      {/* Blocking Reasons Alert if canFinalize is false */}
      {preview && !preview.canFinalize && preview.blockingReasons.length > 0 && (
        <Card className="border-amber-300 bg-amber-50">
          <CardContent className="p-4 flex items-start gap-3">
            <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
            <div>
              <p className="text-sm font-semibold text-amber-900">
                Chưa thể xuất bản tin tuyển dụng:
              </p>
              <ul className="list-disc list-inside text-xs text-amber-800 space-y-0.5 mt-1">
                {preview.blockingReasons.map((reason: string, idx: number) => (
                  <li key={idx}>{reason}</li>
                ))}
              </ul>

            </div>
          </CardContent>
        </Card>
      )}

      {/* Analysis Pipeline Status Banner */}
      <AnalysisStateCard
        preview={preview || null}
        isLoading={previewLoading}
        jobRevision={job.analysisRevision || 1}
        onRequestAnalysis={handleRequestAnalysis}
        isRequesting={requestAnalysisMutation.isPending}
      />

      {/* Main Grid: Preview on Left, AI Skill Review on Right */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <div className="lg:col-span-7 space-y-6">
          <CandidateJobPreview job={job} preview={preview || null} />
        </div>

        <div className="lg:col-span-5 space-y-6">
          {preview && preview.suggestions.length > 0 && (
            <SkillReviewPanel
              suggestions={preview.suggestions}
              availableSkills={availableSkills}
              onUpdateDecisions={handleUpdateDecisions}
              isUpdating={updateDecisionsMutation.isPending}
            />
          )}
        </div>
      </div>
    </div>
  )
}
