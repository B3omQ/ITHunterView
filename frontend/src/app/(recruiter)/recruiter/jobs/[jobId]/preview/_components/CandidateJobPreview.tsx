"use client"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { JobPostingMarkdownContent } from "@/components/jobs/JobPostingMarkdownContent"
import { WorkLocationScheduleContent } from "@/components/jobs/WorkLocationScheduleContent"
import { useTranslations } from "next-intl"

import type { JobPosting } from "@/services/recruiter.service"

interface CandidateJobPreviewProps {
  job: JobPosting
}

export function CandidateJobPreview({ job }: CandidateJobPreviewProps) {
  const t = useTranslations("RecruiterJobPreviewComp.CandidateJobPreview")

  return (
    <Card>
      <CardHeader className="border-b">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <CardTitle className="text-2xl">{job.title}</CardTitle>
            <div className="mt-2 flex flex-wrap gap-2 text-sm text-muted-foreground">
              {job.level && <Badge variant="secondary">{job.level}</Badge>}
              {job.workingModel && <Badge variant="outline">{job.workingModel}</Badge>}
              {job.location && <span>{job.location}</span>}
            </div>
          </div>
          <Badge variant="secondary">{t("preview")}</Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-8 pt-6">
        <div>
          <h3 className="mb-3 text-base font-semibold">{t("descTitle")}</h3>
          <JobPostingMarkdownContent value={job.description} legacyMode="bullet" emptyFallback={t("noDesc")} />
        </div>

        {job.incomeText && (
          <div>
            <h3 className="mb-3 text-base font-semibold">{t("incomeTitle")}</h3>
            <JobPostingMarkdownContent value={job.incomeText} legacyMode="lines" />
          </div>
        )}

        {job.workLocationText && (
          <div>
            <h3 className="mb-3 text-base font-semibold">{t("workLocTitle")}</h3>
            <WorkLocationScheduleContent workLocationText={job.workLocationText} />
          </div>
        )}

        <div>
          <h3 className="mb-3 text-base font-semibold">{t("reqTitle")}</h3>
          <JobPostingMarkdownContent value={job.requirements} legacyMode="bullet" emptyFallback={t("noReq")} />
        </div>

        {job.benefits && (
          <div>
            <h3 className="mb-3 text-base font-semibold">{t("benTitle")}</h3>
            <JobPostingMarkdownContent value={job.benefits} legacyMode="bullet" />
          </div>
        )}
      </CardContent>
    </Card>
  )
}
