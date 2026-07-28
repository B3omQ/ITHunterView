"use client"

import { AlertCircle, CheckCircle2, Loader2, RefreshCw } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import type {
  JobAnalysisLifecycleState,
  JobAnalysisPreviewDto,
} from "@/types/job-analysis.types"

interface AnalysisStateCardProps {
  preview: JobAnalysisPreviewDto | null
  lifecycle: JobAnalysisLifecycleState
  isLoading: boolean
  isCurrentAnalysis: boolean
  onEditDraft: () => void
  onRetryAnalysis: () => void
  isRetrying: boolean
}

export function AnalysisStateCard({
  preview,
  lifecycle,
  isLoading,
  isCurrentAnalysis,
  onEditDraft,
  onRetryAnalysis,
  isRetrying,
}: AnalysisStateCardProps) {
  if (isLoading) {
    return (
      <Card className="border-blue-200 bg-blue-50/50">
        <CardContent className="flex items-center gap-3 p-4">
          <Loader2 className="size-5 animate-spin text-blue-600" />
          <span className="text-sm font-medium text-blue-800">Đang tải dữ liệu phân tích AI...</span>
        </CardContent>
      </Card>
    )
  }

  if (lifecycle === "NOT_REQUESTED") {
    return (
      <ActionCard
        title="Bản thảo chưa được phân tích bằng AI."
        description="Quay lại bản thảo và bấm Publish để bắt đầu quy trình phân tích trước khi xuất bản."
        actionLabel="Chỉnh sửa bản thảo"
        onAction={onEditDraft}
        isPending={false}
      />
    )
  }

  if (lifecycle === "STALE") {
    return (
      <ActionCard
        title="Nội dung yêu cầu công việc đã thay đổi."
        description="Kết quả cũ không còn áp dụng. Quay lại bản thảo, sau đó bấm Publish để chạy phân tích cho nội dung mới."
        actionLabel="Chỉnh sửa bản thảo"
        onAction={onEditDraft}
        isPending={false}
      />
    )
  }

  if (lifecycle === "PENDING" || lifecycle === "PROCESSING") {
    const revisionText = preview?.inputRevision ? " (lượt #" + preview.inputRevision + ")" : ""
    return (
      <Card className="border-blue-200 bg-blue-50/80">
        <CardContent className="flex items-center gap-3 p-4">
          <Loader2 className="size-5 animate-spin text-blue-600" />
          <div>
            <p className="text-sm font-medium text-blue-900">AI đang phân tích mô tả công việc{revisionText}...</p>
            <p className="text-xs text-blue-700">
              Hệ thống tự động cập nhật khi có kết quả. Bạn có thể tiếp tục chờ tại trang này.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (lifecycle === "FAILED") {
    const failureText = preview?.failureCode ? " (" + preview.failureCode + ")" : ""
    return (
      <Card className="border-destructive/40 bg-destructive/5">
        <CardContent className="flex items-center justify-between gap-4 p-4">
          <div className="flex items-center gap-3">
            <AlertCircle className="size-5 shrink-0 text-destructive" />
            <div>
              <p className="text-sm font-medium text-destructive">Phân tích AI thất bại{failureText}.</p>
              <p className="text-xs text-destructive/80">
                Hãy kiểm tra nội dung hoặc thử lại. Hệ thống không tự động dùng thêm AI credit.
              </p>
            </div>
          </div>
          <Button size="sm" variant="destructive" onClick={onRetryAnalysis} disabled={isRetrying}>
            {isRetrying ? <Loader2 className="mr-2 size-4 animate-spin" /> : <RefreshCw className="mr-2 size-4" />}
            Thử lại
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="border-emerald-200 bg-emerald-50/50">
      <CardContent className="flex items-center gap-3 p-4">
        <CheckCircle2 className="size-5 text-emerald-600" />
        <div>
          <p className="text-sm font-medium text-emerald-900">
            {isCurrentAnalysis ? "Phân tích AI đã sẵn sàng." : "Kết quả phân tích không còn là bản hiện hành."}
          </p>
          <p className="text-xs text-emerald-700">
            {isCurrentAnalysis
              ? "Kỹ năng khớp từ điển đã được tự động gắn tag. Bạn có thể kiểm tra bản xem trước và xuất bản tin tuyển dụng."
              : "Hãy phân tích lại bản thảo trước khi tiếp tục."}
          </p>
        </div>
      </CardContent>
    </Card>
  )
}

function ActionCard({
  title,
  description,
  actionLabel,
  onAction,
  isPending,
}: {
  title: string
  description: string
  actionLabel: string
  onAction: () => void
  isPending: boolean
}) {
  return (
    <Card className="border-amber-200 bg-amber-50">
      <CardContent className="flex items-center justify-between gap-4 p-4">
        <div className="flex items-center gap-3">
          <AlertCircle className="size-5 shrink-0 text-amber-600" />
          <div>
            <p className="text-sm font-medium text-amber-900">{title}</p>
            <p className="text-xs text-amber-700">{description}</p>
          </div>
        </div>
        <Button size="sm" onClick={onAction} disabled={isPending} className="bg-amber-600 text-white hover:bg-amber-700">
          {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <RefreshCw className="mr-2 size-4" />}
          {actionLabel}
        </Button>
      </CardContent>
    </Card>
  )
}
