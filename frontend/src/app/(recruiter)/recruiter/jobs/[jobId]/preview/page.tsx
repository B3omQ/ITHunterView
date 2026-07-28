"use client"

import { use, useEffect, useRef, useState } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import { AlertTriangle, ArrowLeft, Edit, Send } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { useJobDetails } from "@/hooks/useJobs"
import {
  useFinalizeJob,
  useJobAnalysis,
  useRequestJobAnalysis,
  useRetryJobAnalysis,
} from "@/hooks/useJobAnalysis"
import {
  getEffectiveJobAnalysisLifecycle,
  isCurrentJobAnalysis,
} from "@/lib/job-analysis-lifecycle"
import { CandidateJobPreview } from "./_components/CandidateJobPreview"
import { AnalysisStateCard } from "./_components/AnalysisStateCard"
import { SkillAnalysisSummaryCard } from "./_components/SkillAnalysisSummaryCard"

function getErrorMessage(error: unknown, fallback: string) {
  return error instanceof Error && error.message ? error.message : fallback
}

function createIdempotencyKey() {
  return typeof crypto !== "undefined" && typeof crypto.randomUUID === "function"
    ? crypto.randomUUID()
    : undefined
}

export default function JobPreviewPage({ params }: { params: Promise<{ jobId: string }> }) {
  const { jobId } = use(params)
  const router = useRouter()
  const searchParams = useSearchParams()
  const autoStartedRef = useRef(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [confirmNoSkillsOpen, setConfirmNoSkillsOpen] = useState(false)

  const { job, loading: jobLoading, error: jobError, refresh: refreshJob } = useJobDetails(jobId)
  const { data: preview, isLoading: previewLoading, isSuccess: previewLoaded, refetch: refetchAnalysis } = useJobAnalysis(jobId)
  const lifecycle = getEffectiveJobAnalysisLifecycle(preview)
  const currentAnalysis = isCurrentJobAnalysis(preview, job?.analysisRevision)
  const requestAnalysisMutation = useRequestJobAnalysis(jobId)
  const retryAnalysisMutation = useRetryJobAnalysis(jobId, preview?.analysisRunId || "")
  const finalizeJobMutation = useFinalizeJob(jobId)

  const refreshAuthoritativeState = () => {
    void refreshJob()
    void refetchAnalysis()
  }

  useEffect(() => {
    if (searchParams.get("publish") !== "1" || autoStartedRef.current || !job || previewLoading || !previewLoaded) {
      return
    }

    autoStartedRef.current = true
    if (lifecycle === "NOT_REQUESTED" || lifecycle === "STALE") {
      const timer = window.setTimeout(() => {
        setActionError(null)
        requestAnalysisMutation.mutate(
          { expectedRevision: job.analysisRevision, idempotencyKey: createIdempotencyKey() },
          {
            onError: (error) => {
              setActionError(getErrorMessage(error, "Unable to start AI analysis."))
              void refreshJob()
              void refetchAnalysis()
            },
            onSettled: () => router.replace("/recruiter/jobs/" + jobId + "/preview"),
          },
        )
      }, 0)
      return () => window.clearTimeout(timer)
    }

    router.replace("/recruiter/jobs/" + jobId + "/preview")
  }, [job, jobId, lifecycle, previewLoaded, previewLoading, refreshJob, refetchAnalysis, requestAnalysisMutation, router, searchParams])

  const retryAnalysis = () => {
    if (!job || !preview?.analysisRunId || lifecycle !== "FAILED") return
    setActionError(null)
    retryAnalysisMutation.mutate(
      { expectedRevision: job.analysisRevision, idempotencyKey: createIdempotencyKey() },
      {
        onError: (error) => {
          setActionError(getErrorMessage(error, "Unable to retry AI analysis."))
          refreshAuthoritativeState()
        },
      },
    )
  }

  const finalize = (confirmNoStandardSkills: boolean) => {
    if (!job || !preview || lifecycle !== "READY" || !currentAnalysis) return
    setActionError(null)
    finalizeJobMutation.mutate(
      {
        analysisRunId: preview.analysisRunId,
        expectedJobRevision: job.analysisRevision,
        expectedDecisionVersion: preview.decisionVersion,
        confirmNoStandardSkills,
      },
      {
        onSuccess: () => router.push("/recruiter/jobs"),
        onError: (error) => {
          setActionError(getErrorMessage(error, "Unable to publish this job posting."))
          refreshAuthoritativeState()
        },
      },
    )
  }

  const handleFinalize = () => {
    if (!preview || !preview.canFinalize || lifecycle !== "READY" || !currentAnalysis) return
    const hasAcceptedStandardSkill = preview.suggestions.some(
      (suggestion) => suggestion.decisionStatus === "ACCEPTED" && suggestion.resolvedSkillId != null,
    )
    if (!hasAcceptedStandardSkill) {
      setConfirmNoSkillsOpen(true)
      return
    }
    finalize(false)
  }

  if (jobLoading) {
    return <div className="container mx-auto flex min-h-[400px] max-w-6xl items-center justify-center py-8 text-muted-foreground">Đang tải thông tin tin tuyển dụng...</div>
  }

  if (jobError || !job) {
    return (
      <div className="container mx-auto max-w-6xl py-8">
        <Card className="border-destructive/40 bg-destructive/5">
          <CardContent className="p-6">
            <p className="font-medium text-destructive">{jobError || "Không tìm thấy tin tuyển dụng hoặc đã xảy ra lỗi."}</p>
            <Button className="mt-4" onClick={() => router.push("/recruiter/jobs")}>Quay lại danh sách</Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  const canPublish = preview?.canFinalize === true && lifecycle === "READY" && currentAnalysis
  const showAnalysisSummary = lifecycle === "READY" && currentAnalysis && Boolean(preview?.suggestions.length)

  return (
    <div className="container mx-auto max-w-6xl space-y-6 py-8">
      <div className="flex flex-col justify-between gap-4 border-b pb-4 sm:flex-row sm:items-center">
        <div className="flex items-center gap-3">
          <Button variant="outline" size="sm" onClick={() => router.push("/recruiter/jobs")}>
            <ArrowLeft className="mr-1 size-4" />Danh sách tin
          </Button>
          <div>
            <h1 className="text-xl font-bold">Xem trước và xuất bản tin tuyển dụng</h1>
            <p className="text-xs text-muted-foreground">Mã tin: {job.jobCode} · Lượt sửa #{job.analysisRevision}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => router.push("/recruiter/jobs/" + job.id + "/edit")}>
            <Edit className="mr-1 size-4" />Chỉnh sửa bản thảo
          </Button>
          <Button size="sm" onClick={handleFinalize} disabled={!canPublish || finalizeJobMutation.isPending}>
            <Send className="mr-1.5 size-4" />{preview?.finalActionLabel || "Xuất bản tin tuyển dụng"}
          </Button>
        </div>
      </div>

      {preview && !preview.canFinalize && preview.blockingReasons.length > 0 && (
        <Card className="border-amber-300 bg-amber-50">
          <CardContent className="flex items-start gap-3 p-4">
            <AlertTriangle className="mt-0.5 size-5 shrink-0 text-amber-600" />
            <div>
              <p className="text-sm font-semibold text-amber-900">Chưa thể xuất bản tin tuyển dụng:</p>
              <ul className="mt-1 list-inside list-disc space-y-0.5 text-xs text-amber-800">
                {preview.blockingReasons.map((reason, index) => <li key={index}>{reason}</li>)}
              </ul>
            </div>
          </CardContent>
        </Card>
      )}

      <AnalysisStateCard
        preview={preview || null}
        lifecycle={lifecycle}
        isLoading={previewLoading}
        isCurrentAnalysis={currentAnalysis}
        onEditDraft={() => router.push("/recruiter/jobs/" + jobId + "/edit")}
        onRetryAnalysis={retryAnalysis}
        isRetrying={retryAnalysisMutation.isPending}
      />

      {actionError && <Card className="border-destructive/40 bg-destructive/5"><CardContent className="p-4 text-sm text-destructive">{actionError}</CardContent></Card>}

      {showAnalysisSummary && preview && <SkillAnalysisSummaryCard suggestions={preview.suggestions} />}

      <CandidateJobPreview job={job} />

      <AlertDialog open={confirmNoSkillsOpen} onOpenChange={setConfirmNoSkillsOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Xuất bản khi chưa có skill tag chuẩn?</AlertDialogTitle>
            <AlertDialogDescription>
              AI vẫn đã lưu các yêu cầu để matching chi tiết. Tuy nhiên, chưa có kỹ năng nào khớp từ điển chuẩn nên tin này sẽ không có tag hoặc bộ lọc theo kỹ năng.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Quay lại</AlertDialogCancel>
            <AlertDialogAction onClick={() => finalize(true)}>Vẫn xuất bản</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
