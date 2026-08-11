"use client";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { MatchRequirementGroupReport } from "@/types/cv.types";
import { useTranslations } from "next-intl";
import { RequirementGroupCard } from "./RequirementGroupCard";

interface RequirementBreakdownProps {
  groups: MatchRequirementGroupReport[];
}

export function RequirementBreakdown({ groups }: RequirementBreakdownProps) {
  const t = useTranslations("CandidateCVMatching");
  if (groups.length === 0) return null;

  return (
    <Card className="border-muted">
      <CardHeader>
        <CardTitle className="text-lg">{t("reqBreakdownTitle")}</CardTitle>
        <CardDescription>{t("reqBreakdownDesc")}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {groups.map((group, index) => (
          <RequirementGroupCard key={group.groupId ?? `requirement-group-${index}`} group={group} groupIndex={index} />
        ))}
      </CardContent>
    </Card>
  );
}
