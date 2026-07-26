"use client"

import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { AlertCircle, CheckCircle2, Loader2, RefreshCw } from "lucide-react"
import type { JobAnalysisPreviewDto } from "@/types/job-analysis.types"

interface AnalysisStateCardProps {
  preview: JobAnalysisPreviewDto | null
  isLoading: boolean
  jobRevision: number
  onRequestAnalysis: () => void
  isRequesting: boolean
}

export function AnalysisStateCard({
  preview,
  isLoading,
  jobRevision,
  onRequestAnalysis,
  isRequesting,
}: AnalysisStateCardProps) {
  if (isLoading) {
    return (
      <Card className="border-blue-200 bg-blue-50/50">
        <CardContent className="p-4 flex items-center gap-3">
          <Loader2 className="w-5 h-5 text-blue-600 animate-spin" />
          <span className="text-sm font-medium text-blue-800">
            Đang tải dữ liệu phân tích AI...
          </span>
        </CardContent>
      </Card>
    )
  }

  if (!preview) {
    return (
      <Card className="border-amber-200 bg-amber-50">
        <CardContent className="p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-amber-600" />
            <div>
              <p className="text-sm font-medium text-amber-900">
                Chưa có lượt phân tích AI cho bản thảo này.
              </p>
              <p className="text-xs text-amber-700">
                Nhấn nút bên dưới để khởi chạy phân tích AI trích xuất kỹ năng từ mô tả & yêu cầu công việc.
              </p>
            </div>
          </div>
          <Button
            size="sm"
            onClick={onRequestAnalysis}
            disabled={isRequesting}
            className="bg-amber-600 hover:bg-amber-700 text-white"
          >
            {isRequesting ? (
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
            ) : (
              <RefreshCw className="w-4 h-4 mr-2" />
            )}
            Phân tích bằng AI
          </Button>
        </CardContent>
      </Card>
    )
  }

  if (preview.status === "PENDING" || preview.status === "PROCESSING") {
    return (
      <Card className="border-blue-200 bg-blue-50/80">
        <CardContent className="p-4 flex items-center gap-3">
          <Loader2 className="w-5 h-5 text-blue-600 animate-spin" />
          <div>
            <p className="text-sm font-medium text-blue-900">
              AI đang phân tích mô tả công việc (Lượt #{preview.inputRevision})...
            </p>
            <p className="text-xs text-blue-700">
              Hệ thống tự động cập nhật kết quả sau vài giây. Vui lòng chờ trong giây lát.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (preview.status === "FAILED") {
    return (
      <Card className="border-red-200 bg-red-50">
        <CardContent className="p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-red-600" />
            <div>
              <p className="text-sm font-medium text-red-900">
                Phân tích AI thất bại ({preview.failureCode || "UNPROCESSABLE_ENTITY"})
              </p>
              <p className="text-xs text-red-700">
                Không thể trích xuất kỹ năng hoặc định dạng AI trả về không hợp lệ. Vui lòng kiểm tra nội dung và thử lại.
              </p>
            </div>
          </div>
          <Button
            size="sm"
            variant="destructive"
            onClick={onRequestAnalysis}
            disabled={isRequesting}
          >
            {isRequesting ? (
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
            ) : (
              <RefreshCw className="w-4 h-4 mr-2" />
            )}
            Thử lại
          </Button>
        </CardContent>
      </Card>
    )
  }

  if (preview.inputRevision < jobRevision || preview.status === "SUPERSEDED") {
    return (
      <Card className="border-amber-200 bg-amber-50">
        <CardContent className="p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-amber-600" />
            <div>
              <p className="text-sm font-medium text-amber-900">
                Bản thảo công việc đã thay đổi (Phiên bản #{jobRevision})
              </p>
              <p className="text-xs text-amber-700">
                Kết quả phân tích hiện tại thuộc phiên bản cũ (Lượt #{preview.inputRevision}). Vui lòng phân tích lại.
              </p>
            </div>
          </div>
          <Button
            size="sm"
            onClick={onRequestAnalysis}
            disabled={isRequesting}
            className="bg-amber-600 hover:bg-amber-700 text-white"
          >
            {isRequesting ? (
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
            ) : (
              <RefreshCw className="w-4 h-4 mr-2" />
            )}
            Phân tích lại AI
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="border-emerald-200 bg-emerald-50/50">
      <CardContent className="p-4 flex items-center gap-3">
        <CheckCircle2 className="w-5 h-5 text-emerald-600" />
        <div>
          <p className="text-sm font-medium text-emerald-900">
            Phân tích AI sẵn sàng (Phiên bản #{preview.inputRevision}, Lượt duyệt #{preview.decisionVersion})
          </p>
          <p className="text-xs text-emerald-700">
            Hãy xem lại danh sách đề xuất bên dưới, tùy chỉnh quyết định (Đồng ý/Từ chối), sau đó nhấn Xuất bản.
          </p>
        </div>
      </CardContent>
    </Card>
  )
}
