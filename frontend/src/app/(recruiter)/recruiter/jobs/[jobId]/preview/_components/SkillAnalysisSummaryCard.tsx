"use client"

import { CheckCircle2, Sparkles } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import type { JobSkillDecisionDto } from "@/types/job-analysis.types"
import { useTranslations } from "next-intl"

interface SkillAnalysisSummaryCardProps {
  suggestions: JobSkillDecisionDto[]
}

export function SkillAnalysisSummaryCard({ suggestions }: SkillAnalysisSummaryCardProps) {
  const t = useTranslations("RecruiterJobPreviewComp.SkillAnalysisSummaryCard")
  
  const standardizedSkills = suggestions.filter(
    (suggestion) => suggestion.decisionStatus === "ACCEPTED" && suggestion.resolvedSkillId != null,
  )

  return (
    <Card>
      <CardHeader className="border-b pb-4">
        <CardTitle className="flex items-center gap-2 text-lg">
          <Sparkles className="size-5 text-muted-foreground" />
          {t("title")}
        </CardTitle>
        <CardDescription>
          {t("desc")}
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
            {t("empty")}
          </p>
        )}
      </CardContent>
    </Card>
  )
}
