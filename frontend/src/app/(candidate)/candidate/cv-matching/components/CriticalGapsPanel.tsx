"use client";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { MatchCriticalGapReport } from "@/types/cv.types";
import { AlertTriangle, XCircle } from "lucide-react";
import { useTranslations } from "next-intl";

interface CriticalGapsPanelProps {
  criticalGaps: MatchCriticalGapReport[];
  warningFlags: string[];
}

const aggregateWarningKeys: Record<string, string> = {
  MULTIPLE_CRITICAL_GAPS: "multipleCriticalGapsWarning",
  CORE_TECH_MISMATCH: "coreTechMismatchWarning",
};

const categoryKeys: Record<string, string> = {
  tech_skill: "categoryTechSkill",
  experience: "categoryExperience",
  domain_knowledge: "categoryDomainKnowledge",
  language: "categoryLanguage",
  education: "categoryEducation",
  soft_skill: "categorySoftSkill",
};

export function criticalGapKey(gap: MatchCriticalGapReport): string {
  const backendId = gap.gapId?.trim();
  if (backendId) return backendId;

  const code = gap.code || "CRITICAL_GAP";
  const scope = gap.scope || "unknown";
  if (scope === "item" && gap.groupId && gap.itemId) {
    return `${code}:item:${gap.groupId}:${gap.itemId}`;
  }
  if (scope === "group" && gap.groupId && (gap.affectedItemIds?.length ?? 0) > 0) {
    return `${code}:group:${gap.groupId}:${gap.affectedItemIds!.join(",")}`;
  }

  return [
    code,
    scope,
    gap.requirement?.trim() || "unknown-requirement",
    gap.operator || "unknown-operator",
    gap.requiredCount ?? "",
    gap.satisfiedCount ?? "",
  ].join(":");
}

export function CriticalGapsPanel({ criticalGaps, warningFlags }: CriticalGapsPanelProps) {
  const t = useTranslations("CandidateCVMatching");
  const aggregateWarnings = warningFlags
    .map((flag) => aggregateWarningKeys[flag])
    .filter((key): key is string => Boolean(key));
  if (criticalGaps.length === 0 && aggregateWarnings.length === 0) return null;

  return (
    <Card className="border-red-200/50 dark:border-red-900/30">
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-lg text-destructive">
          <AlertTriangle className="h-5 w-5" />
          {t("criticalGapsTitle")}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {aggregateWarnings.map((key) => (
          <Alert key={key} variant="destructive" className="border-red-200 bg-red-50/50 dark:border-red-900/50 dark:bg-red-950/20">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription className="text-red-700/90 dark:text-red-400">{t(key)}</AlertDescription>
          </Alert>
        ))}
        {criticalGaps.map((gap, gapIndex) => {
          const categoryKey = gap.category ? categoryKeys[gap.category] ?? "categoryOther" : null;
          const sourceClause = gap.requirementVerbatim?.trim();
          const showSourceClause = Boolean(sourceClause && sourceClause !== gap.requirement.trim());
          const operatorMessage = gap.operator === "one_of" || gap.operator === "at_least_n"
            ? gapMessage(gap, t)
            : null;
          return (
          <Alert
            key={`${criticalGapKey(gap)}:${gapIndex}`}
            variant="destructive"
            className="border-red-200 bg-red-50/50 dark:border-red-900/50 dark:bg-red-950/20"
          >
            <XCircle className="h-4 w-4" />
            <AlertTitle className="flex flex-wrap items-center gap-2 font-semibold text-red-800 dark:text-red-300">
              <span>{gap.requirement || (gap.scope === "item" ? t("requiredItemGap") : t("requiredGroupGap"))}</span>
              {categoryKey ? <Badge variant="outline" className="border-red-300 text-[10px] font-normal text-red-700 dark:text-red-300">{t(categoryKey)}</Badge> : null}
            </AlertTitle>
            <AlertDescription className="mt-2 space-y-2 text-red-700/90 dark:text-red-400">
                {showSourceClause ? (
                  <p className="text-xs text-red-700/80 dark:text-red-400/80">
                    <span className="font-medium">{t("sourceRequirementLabel")}: </span>
                    <span>{sourceClause}</span>
                  </p>
                ) : null}
                {operatorMessage ? <p className="text-xs font-medium">{operatorMessage}</p> : null}
                <p>{gap.reasoning || gapMessage(gap, t)}</p>
                {gap.evidence.map((evidence, evidenceIndex) => (
                  <blockquote key={`${evidence.quotation}-${evidenceIndex}`} className="border-l-2 border-red-300 pl-3 text-xs">
                    “{evidence.quotation}”{evidence.section ? ` — ${evidence.section}` : ""}
                  </blockquote>
                ))}
              </AlertDescription>
          </Alert>
          );
        })}
      </CardContent>
    </Card>
  );
}

function gapMessage(
  gap: MatchCriticalGapReport,
  t: (key: string, values?: Record<string, number>) => string,
) {
  if (gap.operator === "one_of") {
    return t("oneOfGapMessage");
  }
  if (gap.operator === "at_least_n") {
    return t("atLeastGapMessage", {
      required: gap.requiredCount ?? 0,
      satisfied: gap.satisfiedCount ?? 0,
    });
  }
  return gap.scope === "item" ? t("allOfItemGapMessage") : t("requiredGroupGapMessage");
}
