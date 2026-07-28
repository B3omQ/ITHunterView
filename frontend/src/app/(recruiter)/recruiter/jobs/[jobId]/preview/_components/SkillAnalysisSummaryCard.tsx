"use client"

import { CheckCircle2, Sparkles } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import type { JobSkillDecisionDto } from "@/types/job-analysis.types"

interface SkillAnalysisSummaryCardProps {
  suggestions: JobSkillDecisionDto[]
}

export function SkillAnalysisSummaryCard({ suggestions }: SkillAnalysisSummaryCardProps) {
  const standardizedSkills = suggestions.filter(
    (suggestion) => suggestion.decisionStatus === "ACCEPTED" && suggestion.resolvedSkillId != null,
  )

  return (
    <Card>
      <CardHeader className="border-b pb-4">
        <CardTitle className="flex items-center gap-2 text-lg">
          <Sparkles className="size-5 text-muted-foreground" />
          Kỹ năng đã chuẩn hóa
        </CardTitle>
        <CardDescription>
          Các tag này sẽ hiển thị cho ứng viên và được dùng cho bộ lọc kỹ năng.
        </CardDescription>
      </CardHeader>
      <CardContent className="pt-5">
        {standardizedSkills.length > 0 ? (
          <div className="flex flex-wrap items-center gap-2">
            <CheckCircle2 className="size-4 text-emerald-600" />
            {standardizedSkills.map((skill) => (
              <Badge key={skill.id}>{skill.resolvedSkillName || skill.suggestedSkillName || skill.normalizedMention}</Badge>
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">
            Chưa có kỹ năng nào khớp từ điển chuẩn để hiển thị thành tag.
          </p>
        )}
      </CardContent>
    </Card>
  )
}
