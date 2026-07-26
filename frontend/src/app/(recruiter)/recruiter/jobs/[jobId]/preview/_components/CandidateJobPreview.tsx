"use client"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import type { JobPosting } from "@/services/recruiter.service"
import type { JobAnalysisPreviewDto } from "@/types/job-analysis.types"

interface CandidateJobPreviewProps {
  job: JobPosting
  preview: JobAnalysisPreviewDto | null
}

export function CandidateJobPreview({ job, preview }: CandidateJobPreviewProps) {
  const acceptedSkills = preview?.suggestions.filter((s) => s.decisionStatus === "ACCEPTED") || []

  const mustHaveSkills = acceptedSkills.filter((s) => s.importance === "must_have")
  const niceToHaveSkills = acceptedSkills.filter((s) => s.importance === "nice_to_have")

  return (
    <Card className="border shadow-sm">
      <CardHeader className="bg-slate-50 border-b pb-4">
        <div className="flex justify-between items-start">
          <div>
            <CardTitle className="text-2xl font-bold text-slate-900">{job.title}</CardTitle>
            <div className="flex flex-wrap gap-2 mt-2 text-sm text-slate-600">
              {job.level && <Badge variant="secondary">{job.level}</Badge>}
              {job.workingModel && <Badge variant="outline">{job.workingModel}</Badge>}
              {job.location && <span className="text-slate-500">📍 {job.location}</span>}
              {job.incomeText && <span className="text-emerald-600 font-medium">💰 {job.incomeText}</span>}
            </div>
          </div>
          <Badge className="bg-emerald-100 text-emerald-800 border-emerald-200">Draft Preview</Badge>
        </div>
      </CardHeader>

      <CardContent className="pt-6 space-y-6">
        <div>
          <h3 className="font-semibold text-base text-slate-900 mb-2">Mô tả công việc (Job Description)</h3>
          <div className="text-slate-700 whitespace-pre-line text-sm leading-relaxed bg-slate-50 p-4 rounded-md border">
            {job.description || "Chưa có nội dung mô tả."}
          </div>
        </div>

        <div>
          <h3 className="font-semibold text-base text-slate-900 mb-2">Kỹ năng yêu cầu (Dựa trên quyết định duyệt AI)</h3>
          <div className="space-y-3">
            <div>
              <span className="text-xs font-semibold text-red-600 uppercase tracking-wider block mb-1">
                Bắt buộc (Must-have):
              </span>
              {mustHaveSkills.length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {mustHaveSkills.map((s) => (
                    <Badge key={s.id} variant="default" className="bg-red-500 hover:bg-red-600 text-white">
                      {s.resolvedSkillName || s.suggestedSkillName || s.normalizedMention}
                    </Badge>
                  ))}
                </div>
              ) : (
                <span className="text-xs text-slate-400 italic">Chưa có kỹ năng bắt buộc nào được chấp nhận.</span>
              )}
            </div>

            <div>
              <span className="text-xs font-semibold text-blue-600 uppercase tracking-wider block mb-1">
                Ưu tiên (Nice-to-have):
              </span>
              {niceToHaveSkills.length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {niceToHaveSkills.map((s) => (
                    <Badge key={s.id} variant="secondary" className="bg-blue-100 text-blue-800 border-blue-200">
                      {s.resolvedSkillName || s.suggestedSkillName || s.normalizedMention}
                    </Badge>
                  ))}
                </div>
              ) : (
                <span className="text-xs text-slate-400 italic">Chưa có kỹ năng ưu tiên nào được chấp nhận.</span>
              )}
            </div>
          </div>
        </div>

        <div>
          <h3 className="font-semibold text-base text-slate-900 mb-2">Yêu cầu chi tiết (Requirements)</h3>
          <div className="text-slate-700 whitespace-pre-line text-sm leading-relaxed bg-slate-50 p-4 rounded-md border">
            {job.requirements || "Chưa có yêu cầu chi tiết."}
          </div>
        </div>

        {job.benefits && (
          <div>
            <h3 className="font-semibold text-base text-slate-900 mb-2">Quyền lợi (Benefits)</h3>
            <div className="text-slate-700 whitespace-pre-line text-sm leading-relaxed bg-slate-50 p-4 rounded-md border">
              {job.benefits}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
