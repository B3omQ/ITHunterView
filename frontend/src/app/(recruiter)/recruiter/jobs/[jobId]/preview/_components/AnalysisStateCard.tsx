"use client"

import { AlertCircle, CheckCircle2, Loader2, RefreshCw } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import type {
  JobAnalysisLifecycleState,
  JobAnalysisPreviewDto,
} from "@/types/job-analysis.types"
import { useTranslations } from "next-intl"

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
  const t = useTranslations("RecruiterJobPreviewComp.AnalysisStateCard")

  if (isLoading) {
    return (
      <Card className="border-blue-200 bg-blue-50/50">
        <CardContent className="flex items-center gap-3 p-4">
          <Loader2 className="size-5 animate-spin text-blue-600" />
          <span className="text-sm font-medium text-blue-800">{t("loading")}</span>
        </CardContent>
      </Card>
    )
  }

  if (lifecycle === "NOT_REQUESTED") {
    return (
      <ActionCard
        title={t("notRequestedTitle")}
        description={t("notRequestedDesc")}
        actionLabel={t("editDraft")}
        onAction={onEditDraft}
        isPending={false}
      />
    )
  }

  if (lifecycle === "STALE") {
    return (
      <ActionCard
        title={t("staleTitle")}
        description={t("staleDesc")}
        actionLabel={t("editDraft")}
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
            <p className="text-sm font-medium text-blue-900">{t("analyzing", { rev: revisionText })}</p>
            <p className="text-xs text-blue-700">
              {t("analyzingDesc")}
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
              <p className="text-sm font-medium text-destructive">{t("failed", { code: failureText })}</p>
              <p className="text-xs text-destructive/80">
                {t("failedDesc")}
              </p>
            </div>
          </div>
          <Button size="sm" variant="destructive" onClick={onRetryAnalysis} disabled={isRetrying}>
            {isRetrying ? <Loader2 className="mr-2 size-4 animate-spin" /> : <RefreshCw className="mr-2 size-4" />}
            {t("retry")}
          </Button>
        </CardContent>
      </Card>
    )
  }

  if (lifecycle === "READY" && preview?.analysisQuality === "PARTIAL") {
    const coverage = preview.analysisCoverage
    const coverageText = coverage
      ? `${coverage.acceptedGroupCount}/${coverage.inputGroupCount}`
      : "partial"
    return (
      <Card className="border-amber-200 bg-amber-50/80">
        <CardContent className="flex items-center gap-3 p-4">
          <AlertCircle className="size-5 shrink-0 text-amber-600" />
          <div>
            <p className="text-sm font-medium text-amber-900">{"Ph\u00e2n t\u00edch AI ch\u1ec9 \u0111\u1ecdc \u0111\u01b0\u1ee3c "}{coverageText}{" y\u00eau c\u1ea7u."}</p>
            <p className="text-xs text-amber-700">
              {"K\u1ebft qu\u1ea3 v\u1eabn c\u00f3 th\u1ec3 ki\u1ec3m tra v\u00e0 xu\u1ea5t b\u1ea3n, nh\u01b0ng m\u1ed9t s\u1ed1 y\u00eau c\u1ea7u trong JD ch\u01b0a \u0111\u01b0\u1ee3c \u0111\u01b0a v\u00e0o ph\u00e2n t\u00edch."}
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (lifecycle === "READY" && preview?.analysisQuality === "INVALID") {
    return (
      <Card className="border-amber-200 bg-amber-50/80">
        <CardContent className="flex items-center gap-3 p-4">
          <AlertCircle className="size-5 shrink-0 text-amber-600" />
          <div>
            <p className="text-sm font-medium text-amber-900">{"Kh\u00f4ng t\u1ea1o \u0111\u01b0\u1ee3c ph\u00e2n t\u00edch c\u1ea5u tr\u00fac cho JD."}</p>
            <p className="text-xs text-amber-700">
              {"H\u1ec7 th\u1ed1ng v\u1eabn gi\u1eef n\u1ed9i dung JD g\u1ed1c \u0111\u1ec3 x\u1eed l\u00fd. B\u1ea1n c\u00f3 th\u1ec3 ki\u1ec3m tra v\u00e0 xu\u1ea5t b\u1ea3n, nh\u01b0ng tin s\u1ebd kh\u00f4ng c\u00f3 skill tag chu\u1ea9n."}
            </p>
          </div>
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
            {isCurrentAnalysis ? t("readyTitle") : t("readyStaleTitle")}
          </p>
          <p className="text-xs text-emerald-700">
            {isCurrentAnalysis
              ? t("readyDesc")
              : t("readyStaleDesc")}
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
