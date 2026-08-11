"use client";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { MatchRequirementGroupReport } from "@/types/cv.types";
import { buildRequirementSourceViews } from "@/lib/matching-requirement-sources";
import { useTranslations } from "next-intl";
import { RequirementSourceCard } from "./RequirementSourceCard";

interface RequirementBreakdownProps {
  groups: MatchRequirementGroupReport[];
}

export function RequirementBreakdown({ groups }: RequirementBreakdownProps) {
  const t = useTranslations("CandidateCVMatching");
  if (groups.length === 0) return null;
  const sourceViews = buildRequirementSourceViews(groups);

  return (
    <Card className="border-muted">
      <CardHeader>
        <CardTitle className="text-lg">{t("reqBreakdownTitle")}</CardTitle>
        <CardDescription>{t("reqBreakdownDesc")}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {sourceViews.map((source, index) => (
          <RequirementSourceCard key={source.key} source={source} sourceIndex={index} />
        ))}
      </CardContent>
    </Card>
  );
}
